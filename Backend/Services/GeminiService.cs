using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AITalentHub.Models;

namespace AITalentHub.Services
{
    public interface IGeminiService
    {
        Task<(string NextPrompt, string ExtractedDetailsJson, bool IsComplete)> OnboardChatAsync(string onboardingStateJson, string userMessage);
        Task<string> AnalyzeResumeAsync(CandidateProfile profile, string fullName, string email);
        Task<string> GenerateInterviewPrepAsync(JobPost job, CandidateProfile profile, string fullName);
    }

    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public GeminiService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            // Read from appsettings or Environment variable
            _apiKey = config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        }

        private bool IsApiConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<(string NextPrompt, string ExtractedDetailsJson, bool IsComplete)> OnboardChatAsync(string onboardingStateJson, string userMessage)
        {
            if (IsApiConfigured)
            {
                try
                {
                    return await CallGeminiOnboardingAsync(onboardingStateJson, userMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gemini API error in Onboarding, running local NLP fallback: {ex.Message}");
                }
            }

            return RunLocalMockOnboarding(onboardingStateJson, userMessage);
        }

        public async Task<string> AnalyzeResumeAsync(CandidateProfile profile, string fullName, string email)
        {
            if (IsApiConfigured)
            {
                try
                {
                    return await CallGeminiAnalyzeResumeAsync(profile, fullName, email);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gemini API error in Analysis, running local NLP fallback: {ex.Message}");
                }
            }

            return RunLocalMockAnalysis(profile);
        }

        public async Task<string> GenerateInterviewPrepAsync(JobPost job, CandidateProfile profile, string fullName)
        {
            if (IsApiConfigured)
            {
                try
                {
                    return await CallGeminiInterviewPrepAsync(job, profile, fullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gemini API error in Interview Prep, running local NLP fallback: {ex.Message}");
                }
            }

            return RunLocalMockInterviewPrep(job, profile);
        }

        #region Gemini API Calls
        private async Task<(string NextPrompt, string ExtractedDetailsJson, bool IsComplete)> CallGeminiOnboardingAsync(string onboardingStateJson, string userMessage)
        {
            string systemInstruction = @"You are a friendly, premium AI Recruiter and Onboarding Assistant.
Your goal is to collect all the following candidate details through a conversational chat:
1. Full Name
2. Email
3. Phone Number
4. Address
5. Date of Birth
6. Career Objective
7. Education History (School, Degree, Graduation Year)
8. Skills (List of technical or soft skills)
9. Certifications (List of certifications and year)
10. Work Experience (Company, Job Title, Years)
11. Projects (Name, Description, Technologies)
12. Languages
13. Achievements
14. References (Name, Designation, Contact)
15. Preferred Job Category
16. Preferred Salary
17. Preferred Location
18. LinkedIn & GitHub Profiles

Read the current onboarding state JSON, which stores previously gathered fields.
Review the new user message. Parse it, extract any values matching the 18 items, merge them into the profile details, and ask for the next missing information in a natural, polite conversation. Do not ask for too many things at once. Ask for 1-2 details at a time to maintain a premium conversational experience.

You MUST return a JSON object with this EXACT structure:
{
  ""nextPrompt"": ""Your next friendly question/response to the candidate."",
  ""extractedDetails"": {
      ""fullName"": ""parsed name or current value"",
      ""email"": ""parsed email or current value"",
      ""phone"": ""parsed phone or current value"",
      ""address"": ""parsed address or current value"",
      ""dateOfBirth"": ""parsed DOB or current value"",
      ""careerObjective"": ""parsed objective or current value"",
      ""skills"": ""semicolon separated skills list or current value"",
      ""languages"": ""semicolon separated list or current value"",
      ""preferredJobCategory"": ""parsed category or current value"",
      ""preferredSalary"": ""parsed salary or current value"",
      ""preferredLocation"": ""parsed location or current value"",
      ""linkedInUrl"": ""parsed link or current value"",
      ""gitHubUrl"": ""parsed link or current value"",
      ""education"": [ { ""degree"": """", ""school"": """", ""year"": """" } ],
      ""experience"": [ { ""title"": """", ""company"": """", ""years"": """" } ],
      ""certifications"": [ { ""name"": """", ""issuer"": """", ""year"": """" } ],
      ""projects"": [ { ""name"": """", ""description"": """", ""technologies"": """" } ],
      ""achievements"": [ ""achievement 1"", ""achievement 2"" ],
      ""references"": [ { ""name"": """", ""designation"": """", ""contact"": """" } ]
  },
  ""isComplete"": false
}
If all key details are gathered (or the user wishes to skip references/achievements and finish), set isComplete to true.
Current Onboarding State:
" + onboardingStateJson + @"
User's message:
" + userMessage;

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = systemInstruction } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var resultText = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

            using var resultDoc = JsonDocument.Parse(resultText);
            var root = resultDoc.RootElement;
            var nextPrompt = root.GetProperty("nextPrompt").GetString() ?? "Could you tell me more about your experience?";
            var isComplete = root.GetProperty("isComplete").GetBoolean();
            
            // Extract the extractedDetails element to return as JSON
            var detailsJson = root.GetProperty("extractedDetails").GetRawText();
            return (nextPrompt, detailsJson, isComplete);
        }

        private async Task<string> CallGeminiAnalyzeResumeAsync(CandidateProfile profile, string fullName, string email)
        {
            string prompt = $@"Analyze this candidate's profile and calculate recruitment scores.
Name: {fullName}
Email: {email}
Bio: {profile.Bio}
Skills: {profile.Skills}
Experience: {profile.ExperienceJson}
Education: {profile.EducationJson}
Certifications: {profile.CertificationsJson}
Projects: {profile.ProjectsJson}
Languages: {profile.Languages}
Objective: {profile.CareerObjective}

You must calculate:
1. skillScore (0-100) - based on complexity/range of listed skills
2. experienceScore (0-100) - based on jobs, timeline
3. educationScore (0-100) - based on degrees/courses
4. atsCompatibility (0-100) - check if information is structured and keywords are clear
5. overallScore (0-100) - average or weighted score representing employability

Also, provide a list of suggestions (3-5 concrete action items) to improve their profile.

Return EXACTLY this JSON structure:
{{
  ""skillScore"": 85,
  ""experienceScore"": 70,
  ""educationScore"": 90,
  ""atsCompatibility"": 80,
  ""overallScore"": 81,
  ""suggestions"": [
    ""Add more details about React and JavaScript to your experience section."",
    ""Consider earning a certificate in Cloud Architecture.""
  ]
}}";

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;
        }

        private async Task<string> CallGeminiInterviewPrepAsync(JobPost job, CandidateProfile profile, string fullName)
        {
            string prompt = $@"You are an AI Interview Prep Specialist. Provide a set of interview prep questions for:
Candidate: {fullName}
Skills: {profile.Skills}
Experience: {profile.ExperienceJson}
Education: {profile.EducationJson}

Applying for: {job.Title} at Recruiter Company
Job Description: {job.Description}
Job Requirements: {job.Requirements}

Generate:
1. Technical Questions (2 questions specific to job requirements and candidate skills)
2. Behavioral Questions (2 questions)
3. HR Questions (2 questions)
Include an 'aiTips' field for each question explaining how the candidate should formulate their answer based on their profile.

Return a JSON array of questions, each with:
{{
  ""category"": ""Technical"" or ""Behavioral"" or ""HR"",
  ""question"": ""Question text?"",
  ""aiTips"": ""Tips for answering.""
}}

Return ONLY the JSON array.";

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;
        }
        #endregion

        #region Local Mock Fallback Logic (NLP State Machine)
        private (string NextPrompt, string ExtractedDetailsJson, bool IsComplete) RunLocalMockOnboarding(string onboardingStateJson, string userMessage)
        {
            // Parse current state
            Dictionary<string, object> state;
            try
            {
                state = JsonSerializer.Deserialize<Dictionary<string, object>>(onboardingStateJson) ?? new Dictionary<string, object>();
            }
            catch
            {
                state = new Dictionary<string, object>();
            }

            // Extract nested 'extractedDetails' if exists, or initialize new
            Dictionary<string, object> details = new Dictionary<string, object>();
            if (state.TryGetValue("extractedDetails", out var detailsObj) && detailsObj != null)
            {
                details = JsonSerializer.Deserialize<Dictionary<string, object>>(detailsObj.ToString()!) ?? new Dictionary<string, object>();
            }

            // Make sure list fields are present
            EnsureListField(details, "education");
            EnsureListField(details, "experience");
            EnsureListField(details, "certifications");
            EnsureListField(details, "projects");
            EnsureListField(details, "achievements");
            EnsureListField(details, "references");

            // Check current question step
            int currentStep = 0;
            if (state.TryGetValue("currentStep", out var stepVal))
            {
                currentStep = int.TryParse(stepVal.ToString(), out int parsedStep) ? parsedStep : 0;
            }

            string nextPrompt = "";
            bool isComplete = false;

            // Step machine to guide candidate
            switch (currentStep)
            {
                case 0:
                    // Introduction & Name
                    nextPrompt = "Welcome to the AI Talent Hub onboarding! I'm here to build your smart profile. Let's start with your contact details. What is your Full Name, Email, and Phone number?";
                    currentStep = 1;
                    break;

                case 1:
                    // Process Name, Email, Phone
                    ParseNameEmailPhone(userMessage, details);
                    nextPrompt = $"Nice to meet you! Next, could you tell me your current Address and Date of Birth (YYYY-MM-DD)?";
                    currentStep = 2;
                    break;

                case 2:
                    // Process Address & DOB
                    ParseAddressDob(userMessage, details);
                    nextPrompt = "Great. What is your Career Objective or professional bio? E.g., 'A software developer eager to build web APIs...'";
                    currentStep = 3;
                    break;

                case 3:
                    // Process objective
                    details["careerObjective"] = userMessage;
                    details["bio"] = userMessage.Length > 200 ? userMessage.Substring(0, 197) + "..." : userMessage;
                    nextPrompt = "Excellent. Let's discuss your Education. What degree did you earn, from which School/University, and which Year?";
                    currentStep = 4;
                    break;

                case 4:
                    // Process Education
                    ParseEducation(userMessage, details);
                    nextPrompt = "Good. Now, tell me about your Work Experience. What was your Job Title, Company, and Years of employment? (e.g. 'Software Engineer at Google for 2023 - Present')";
                    currentStep = 5;
                    break;

                case 5:
                    // Process Experience
                    ParseExperience(userMessage, details);
                    nextPrompt = "Excellent. What technical or soft Skills do you possess? (Please separate them with semicolons, e.g. 'C#;React;Docker;Agile')";
                    currentStep = 6;
                    break;

                case 6:
                    // Process Skills
                    details["skills"] = userMessage.Replace(",", ";").Replace("\n", ";");
                    nextPrompt = "Great. Have you completed any Certifications? E.g. 'AWS Certified Developer (2024)'";
                    currentStep = 7;
                    break;

                case 7:
                    // Process Certifications
                    ParseCertifications(userMessage, details);
                    nextPrompt = "Let's record your Projects. What is the Name, Description, and technologies used in one of your key projects?";
                    currentStep = 8;
                    break;

                case 8:
                    // Process Projects
                    ParseProjects(userMessage, details);
                    nextPrompt = "Splendid. What Languages do you speak? (E.g. English, Spanish)";
                    currentStep = 9;
                    break;

                case 9:
                    // Process Languages
                    details["languages"] = userMessage.Replace(",", ";").Replace("\n", ";");
                    nextPrompt = "Do you have any notable Achievements or awards? If none, type 'none'.";
                    currentStep = 10;
                    break;

                case 10:
                    // Process Achievements
                    if (userMessage.ToLower().Trim() != "none")
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(details["achievements"].ToString()!) ?? new List<string>();
                        list.Add(userMessage);
                        details["achievements"] = list;
                    }
                    nextPrompt = "Lastly, can you provide a professional Reference? (Name, Designation, Email/Phone). Type 'none' if you want to skip.";
                    currentStep = 11;
                    break;

                case 11:
                    // Process References
                    ParseReferences(userMessage, details);
                    nextPrompt = "What is your Preferred Job Category, Preferred Salary, and Preferred Location? (E.g., Software Engineer, Rs. 200k, Colombo / Remote)";
                    currentStep = 12;
                    break;

                case 12:
                    // Preferred jobs info
                    ParsePreferences(userMessage, details);
                    nextPrompt = "Perfect. Please paste your LinkedIn and GitHub profile URLs. (E.g. https://linkedin.com/in/my-id, https://github.com/my-id)";
                    currentStep = 13;
                    break;

                case 13:
                    // Process Social Links
                    ParseSocials(userMessage, details);
                    nextPrompt = "Thank you! I have gathered all your information. Generating your smart ATS-friendly CV and compatibility reports now. Welcome to AI Talent Hub!";
                    currentStep = 18;
                    isComplete = true;
                    break;

                default:
                    nextPrompt = "Your onboarding is complete. Check out your dashboard for personalized recommendations!";
                    isComplete = true;
                    break;
            }

            // Update state
            state["currentStep"] = currentStep;
            state["extractedDetails"] = details;
            state["isComplete"] = isComplete;

            var updatedStateJson = JsonSerializer.Serialize(state);
            var detailsJson = JsonSerializer.Serialize(details);

            return (nextPrompt, detailsJson, isComplete);
        }

        private string RunLocalMockAnalysis(CandidateProfile profile)
        {
            var skillsList = profile.Skills.Split(';', StringSplitOptions.RemoveEmptyEntries);
            int skillScore = Math.Min(60 + (skillsList.Length * 6), 98);

            int experienceCount = 0;
            try
            {
                var exp = JsonSerializer.Deserialize<List<object>>(profile.ExperienceJson);
                experienceCount = exp?.Count ?? 0;
            }
            catch { }
            int experienceScore = Math.Min(50 + (experienceCount * 15), 95);

            int educationCount = 0;
            try
            {
                var edu = JsonSerializer.Deserialize<List<object>>(profile.EducationJson);
                educationCount = edu?.Count ?? 0;
            }
            catch { }
            int educationScore = Math.Min(70 + (educationCount * 10), 96);

            int atsScore = 80;
            if (profile.Skills.Length > 10) atsScore += 5;
            if (profile.Phone.Length > 5) atsScore += 5;
            if (profile.LinkedInUrl.Contains("linkedin.com")) atsScore += 5;
            atsScore = Math.Min(atsScore, 98);

            int overallScore = (skillScore + experienceScore + educationScore + atsScore) / 4;

            var suggestions = new List<string>
            {
                "Complete your social links to improve recruiter reachability.",
                "Detail technologies used under each project entry to optimize keyword matches.",
                "Acquire additional certifications aligned with your career goals."
            };

            var analysisObj = new
            {
                skillScore = skillScore,
                experienceScore = experienceScore,
                educationScore = educationScore,
                atsCompatibility = atsScore,
                overallScore = overallScore,
                suggestions = suggestions
            };

            return JsonSerializer.Serialize(analysisObj);
        }

        private string RunLocalMockInterviewPrep(JobPost job, CandidateProfile profile)
        {
            var prepList = new List<object>
            {
                new {
                    category = "Technical",
                    question = $"Can you explain how you would apply your skills in {profile.Skills.Split(';')[0]} to implement the core requirements of the {job.Title} role?",
                    aiTips = "Mention a specific project where you successfully implemented these technologies, focusing on scale and optimizations."
                },
                new {
                    category = "Technical",
                    question = "How do you ensure code quality, testing, and continuous deployment in an engineering project?",
                    aiTips = "Highlight experience with Git, continuous integration pipelines, and write-ups of automated tests."
                },
                new {
                    category = "Behavioral",
                    question = "Describe a situation where you had a tight deadline and how you managed to deliver high-quality work.",
                    aiTips = "Use the STAR method (Situation, Task, Action, Result). State the deadline pressure, prioritize critical paths, and emphasize the positive outcome."
                },
                new {
                    category = "Behavioral",
                    question = "Tell me about a time you encountered a technical blocker. How did you troubleshoot and resolve it?",
                    aiTips = "Explain your debugging methodology, utilization of documentation or community support, and collaborative team communication."
                },
                new {
                    category = "HR",
                    question = $"Why are you interested in joining Apex Software Solutions as a {job.Title}?",
                    aiTips = "Mention the company description (global leader in smart recruitment solutions) and connect it with your career objective."
                },
                new {
                    category = "HR",
                    question = "What are your salary expectations and preferred work setup (remote vs. physical)?",
                    aiTips = $"Align your response with your preferred settings: {profile.PreferredSalary} in {profile.PreferredLocation}."
                }
            };

            return JsonSerializer.Serialize(prepList);
        }

        private void EnsureListField(Dictionary<string, object> dict, string fieldName)
        {
            if (!dict.ContainsKey(fieldName) || dict[fieldName] == null || string.IsNullOrWhiteSpace(dict[fieldName].ToString()))
            {
                if (fieldName == "achievements")
                {
                    dict[fieldName] = new List<string>();
                }
                else
                {
                    dict[fieldName] = new List<object>();
                }
            }
            else
            {
                try
                {
                    // Check if it's already a deserialized array/list or needs conversion
                    var type = dict[fieldName].GetType();
                    if (type == typeof(string))
                    {
                        if (fieldName == "achievements")
                        {
                            dict[fieldName] = JsonSerializer.Deserialize<List<string>>(dict[fieldName].ToString()!) ?? new List<string>();
                        }
                        else
                        {
                            dict[fieldName] = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(dict[fieldName].ToString()!) ?? new List<Dictionary<string, string>>();
                        }
                    }
                }
                catch
                {
                    if (fieldName == "achievements")
                    {
                        dict[fieldName] = new List<string>();
                    }
                    else
                    {
                        dict[fieldName] = new List<object>();
                    }
                }
            }
        }

        private void ParseNameEmailPhone(string msg, Dictionary<string, object> details)
        {
            // Simple heuristic parsing
            var words = msg.Split(new[] { ' ', ',', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Name detection: Capitalized words
            var nameParts = new List<string>();
            foreach (var w in words)
            {
                if (w.Length > 1 && char.IsUpper(w[0]) && !w.Contains("@") && !w.Contains("http") && !int.TryParse(w, out _))
                {
                    nameParts.Add(w);
                }
            }
            if (nameParts.Count > 0)
            {
                details["fullName"] = string.Join(" ", nameParts);
            }
            else
            {
                details["fullName"] = msg.Length > 30 ? msg.Substring(0, 30) : msg;
            }

            // Email detection
            foreach (var w in words)
            {
                if (w.Contains("@") && w.Contains("."))
                {
                    details["email"] = w.Trim('(', ')', '[', ']', ',');
                    break;
                }
            }

            // Phone detection
            foreach (var w in words)
            {
                var digits = System.Text.RegularExpressions.Regex.Replace(w, @"[^\d\+]", "");
                if (digits.Length >= 9 && digits.Length <= 15)
                {
                    details["phone"] = w;
                    break;
                }
            }
        }

        private void ParseAddressDob(string msg, Dictionary<string, object> details)
        {
            // Try to extract DOB (format YYYY-MM-DD or word dates)
            var dobMatch = System.Text.RegularExpressions.Regex.Match(msg, @"\d{4}-\d{2}-\d{2}");
            if (dobMatch.Success)
            {
                details["dateOfBirth"] = dobMatch.Value;
                details["address"] = msg.Replace(dobMatch.Value, "").Trim(' ', ',', '.');
            }
            else
            {
                details["dateOfBirth"] = "2000-01-01";
                details["address"] = msg;
            }
        }

        private void ParseEducation(string msg, Dictionary<string, object> details)
        {
            var parts = msg.Split(new[] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            string degree = parts.Length > 0 ? parts[0].Trim() : "B.Sc. degree";
            string school = parts.Length > 1 ? parts[1].Trim() : "University";
            string year = parts.Length > 2 ? parts[2].Trim() : "2024";

            var eduList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(JsonSerializer.Serialize(details["education"])) ?? new List<Dictionary<string, string>>();
            eduList.Add(new Dictionary<string, string> {
                { "degree", degree },
                { "school", school },
                { "year", year }
            });
            details["education"] = eduList;
        }

        private void ParseExperience(string msg, Dictionary<string, object> details)
        {
            var parts = msg.Split(new[] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            string title = parts.Length > 0 ? parts[0].Trim() : "Software Engineer";
            string company = parts.Length > 1 ? parts[1].Trim() : "Tech Company";
            string years = parts.Length > 2 ? parts[2].Trim() : "2023 - Present";

            var expList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(JsonSerializer.Serialize(details["experience"])) ?? new List<Dictionary<string, string>>();
            expList.Add(new Dictionary<string, string> {
                { "title", title },
                { "company", company },
                { "years", years }
            });
            details["experience"] = expList;
        }

        private void ParseCertifications(string msg, Dictionary<string, object> details)
        {
            if (msg.ToLower().Trim() == "none") return;
            var parts = msg.Split(new[] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            string name = parts.Length > 0 ? parts[0].Trim() : msg;
            string issuer = parts.Length > 1 ? parts[1].Trim() : "Issuer";
            string year = parts.Length > 2 ? parts[2].Trim() : "2024";

            var certs = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(JsonSerializer.Serialize(details["certifications"])) ?? new List<Dictionary<string, string>>();
            certs.Add(new Dictionary<string, string> {
                { "name", name },
                { "issuer", issuer },
                { "year", year }
            });
            details["certifications"] = certs;
        }

        private void ParseProjects(string msg, Dictionary<string, object> details)
        {
            var parts = msg.Split(new[] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            string name = parts.Length > 0 ? parts[0].Trim() : "Project Portfolio";
            string desc = parts.Length > 1 ? parts[1].Trim() : "Developed smart application services.";
            string tech = parts.Length > 2 ? parts[2].Trim() : "React;C#";

            var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(JsonSerializer.Serialize(details["projects"])) ?? new List<Dictionary<string, string>>();
            list.Add(new Dictionary<string, string> {
                { "name", name },
                { "description", desc },
                { "technologies", tech }
            });
            details["projects"] = list;
        }

        private void ParseReferences(string msg, Dictionary<string, object> details)
        {
            if (msg.ToLower().Trim() == "none") return;
            var parts = msg.Split(new[] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            string name = parts.Length > 0 ? parts[0].Trim() : msg;
            string designation = parts.Length > 1 ? parts[1].Trim() : "Reference Mentor";
            string contact = parts.Length > 2 ? parts[2].Trim() : "Email/Phone";

            var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(JsonSerializer.Serialize(details["references"])) ?? new List<Dictionary<string, string>>();
            list.Add(new Dictionary<string, string> {
                { "name", name },
                { "designation", designation },
                { "contact", contact }
            });
            details["references"] = list;
        }

        private void ParsePreferences(string msg, Dictionary<string, object> details)
        {
            var parts = msg.Split(new[] { ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            details["preferredJobCategory"] = parts.Length > 0 ? parts[0].Trim() : msg;
            details["preferredSalary"] = parts.Length > 1 ? parts[1].Trim() : "Market standard";
            details["preferredLocation"] = parts.Length > 2 ? parts[2].Trim() : "Remote";
        }

        private void ParseSocials(string msg, Dictionary<string, object> details)
        {
            var parts = msg.Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (p.Contains("linkedin.com"))
                {
                    details["linkedInUrl"] = p;
                }
                else if (p.Contains("github.com"))
                {
                    details["gitHubUrl"] = p;
                }
            }
        }
        #endregion
    }
}
