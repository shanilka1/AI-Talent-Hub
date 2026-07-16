# 🚀 AI Talent Hub – AI Powered Resume Builder & Job Matching Platform

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-Latest-purple?style=for-the-badge&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red?style=for-the-badge&logo=microsoftsqlserver)](https://www.microsoft.com/en-us/sql-server)
[![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5-purple?style=for-the-badge&logo=bootstrap)](https://getbootstrap.com/)
[![AI Powered](https://img.shields.io/badge/AI-Powered-success?style=for-the-badge&logo=openai)](https://openai.com/)

AI Talent Hub is an advanced, intelligent recruitment ecosystem engineered to bridge the gap between job seekers and corporate recruiters using AI-driven matching technology. Candidates can dynamically build smart resumes, track applications, and optimize their profiles, while recruiters can efficiently post jobs, manage applicants via an ATS pipeline, and dynamically score candidate capabilities against core requirements.

---

## ✨ System Modules & Features

### 👨‍🎓 Candidate Features
* **AI Resume Builder & Management:** Structurally compile, build, and manage personal resumes.
* **Resume Upload & Parsing:** Auto-extract candidate skills and experience histories from raw files (CV Upload Option for AI Read).
* **Smart Job Recommendations:** Intuitive system recommendation algorithms based on skill profiles.
* **Application Tracking System (ATS):** Interactive dashboard indicating application progress status and applicant counts.
* **Profile Management:** Comprehensive role-based profile analytics for student profiles.

### 🏢 Recruiter Features
* **Job Posting & Details System:** Seamless creation, publishing, and configuration of dynamic job ads with role-based UIs.
* **Candidate Search & Filtering:** Sift through multiple active candidates effortlessly via custom queries on the recruiter dashboard.
* **Applicant Tracking System (ATS):** Comprehensive recruitment pipeline tracking from intake to final selection.
* **Interview Scheduling:** Integrated third-party scheduling component utilizing the **Google Calendar API** for real-time reminders.
* **Company Profile Management:** Control corporate branding, parameters, and details.

### 🤖 AI Core Features
* **Gemini AI Integration:** Deep scanning of text to extract key professional metrics from uploaded resumes.
* **Skill Extraction & Projects Layer:** Structural classification of technology expertise and projects.
* **Job-Candidate Matching:** Compute accuracy rankings based on candidate capabilities vs job prerequisites.

### 🔐 Security & Infrastructure
* **JWT Authentication:** Stateful tokenization patterns protecting backend communication routing.
* **Role-Based Access Control (RBAC):** Tiered operational profiles for Candidates, Recruiters, and Administrators.
* **Data Encryption:** Enterprise-grade security handling sensitive user details.

---

## ⚡ Tech Stack

* **Frontend:** HTML5, CSS3, JavaScript (ES6), Bootstrap 5 Framework
* **Backend:** ASP.NET Core 8.0 / Compiled for .NET 10.0 runtime environment
* **Database:** Microsoft SQL Server (Entity Framework Core migrations pattern)
* **AI Engine Layer:** OpenAI API / Google Gemini AI API (`gemini-pro`)
* **Third-Party Integrations:** Google Calendar API Suite
* **Development Tools:** Visual Studio 2022, Git Version Control, GitHub Ecosystem

---

## 📁 Project Structure

```text
AI-Talent-Hub/
│
├── Frontend/
│   ├── pages/               # Candidate & Recruiter Dashboards, Search Interfaces, Profiles
│   ├── components/          # Shared Layout Components (Navbars, Footers, Modals)
│   ├── assets/              # Core stylesheets, styling architectures, and site imagery
│   └── services/            # Frontend Web Client API handlers & Google Calendar integrations
│
├── Backend/
│   ├── Controllers/         # Endpoint Routing Handlers (Authentication, Job Matching, Postings)
│   ├── Models/              # Data Entities & Database Relational Mapping Classes
│   ├── Services/            # Business Logic Implementation & Core AI Integrations
│   └── Data/                # ApplicationDbContext & Entity Framework Core Migration History
│
├── Database/
│   ├── tables/              # DDL scripts outlining standard system databases
│   ├── procedures/          # Stored procedures maximizing processing speed
│   └── scripts/             # Mock data scripts and seed data execution profiles
│
└── README.md                # System Documentation File
```
## 👥 Project Team & Contributions

### 👑 Group Leader
> **Shanilka Lakshan** (`shanilka1`) – **Feature Lead**  
> Managed end-to-end feature integrations, designed and built the recruiter candidate search section UI, oversaw database profile seeding configurations, and carried out structural project quality control across major milestones.

### 👨‍💻 Development Team & Core Focus Areas

> **Kawya Dissanayaka** (`KawyaDissanayaka`) – **Technical & Project Lead**  
> Orchestrated full project infrastructure configuration, established target compilation parameters for the `.NET 10.0` runtime environment, configured multi-platform deployment routing (GitHub Pages for Frontend, Render/Azure for Backend APIs), purged legacy codebase implementations, and structured enterprise-grade environment `.gitignore` profiles.

> **Darshana Chinthaka** (`DarshanaChinthaka`) – **Backend Architect**  
> Designed core RESTful Web APIs, formulated internal relational database schemas using Entity Framework Core, deployed operational candidate/recruiter dashboard UI flows, built structured project upload layers, and established deep CV AI-reading pipeline hooks.

> **Minidu Mansara** (`minidu1`) – **Frontend Integration Lead**  
> Successfully deployed Google Calendar API communication mechanics, automated live interview reminder dispatches, refactored redundant code segments, resolved navigation layout discrepancies across different roles, and managed system runtime bug squashing.

> **Desanda Chathmal** (`Desanda`) – **Quality Assurance Specialist**  
> Monitored integration testing frameworks, mapped strict system verification endpoints, and audited workflow behavioral stability across all integrated branches.

> **Kavindi Perera** (`Kavindi`) – **UI/UX Wireframing Specialist**  
> Authored detailed technical system documentation and drafted early-stage UI architecture wireframes.

## 📊 Repository Development & Branch Insights

### 📋 Branch Matrix & Production Status
* **Total Active Branches:** 9
* **Report Extraction Reference Date:** 2026-07-16
* **Development Duration:** ~29 Days of Active Implementation Trace

| # | Branch Name | Latest Commit Message | Status | Core Branch Focus Area |
|---|---|---|---|---|
| 1 | **main** | Merge PR #11 - Shanilka branch merge | `✅ Primary` | Stable production-ready deployment trunk |
| 2 | **Shanilka** | Add seed data for candidate profiles | `🔄 Active` | Candidate operational trace and testing |
| 3 | **Darshana** | Implement profile & job-details pages | `🔄 Active` | Core page controllers & database extensions |
| 4 | **kawya** | Merge main into kawya | `🔄 Active` | Infrastructure configuration, cleanup, & deployment |
| 5 | **minidu** | Fix nav bar | `✅ Merged` | Google Calendar integrations & navbar tweaks |
| 6 | **Desanda** | *(Latest: No recent activity)* | `⏸️ Inactive` | Quality assurance and verification staging |
| 7 | **Kavindi** | *(Latest: No recent activity)* | `⏸️ Inactive` | Documentation logging branch |
| 8 | **TestBranch**| *(Latest: No recent activity)* | `⏸️ Inactive` | Sandbox test bedding area |
| 9 | **gh-pages**  | *(GitHub Pages Deploy)* | `📄 Pages` | Hosting destination for static frontend clients |

### 📈 Language Distribution Statistics
* **HTML:** 61.1%
* **C# (.NET Source Code):** 30.6%
* **JavaScript:** 5.2%
* **CSS:** 2.9%
* **Dockerfile:** 0.2%

---

## 🛠️ Complete Commit History & Feature Log

Below is the structured feature trace implemented across active development lines, extracted directly from developer commit logs up to **July 16, 2026**:

### 1. Main Development Branch (`main`)
> 🔍 **Core Focus:** Stable production-ready deployment trunk
* **Candidate Search System:** Implemented full backend query filters paired with frontend UI integration to allow recruiters to look up candidates dynamically (**PR #11** by `KawyaDissanayaka` & `shanilka1`).
* **Database Initializations:** Added initial Entity Framework Core migration files and configured database schemas inside `appsettings.json` (`KawyaDissanayaka`).
* **Google Calendar Integration:** Merged **PR #8** to connect interview scheduling routines with the Google Calendar API (`DarshanaChinthaka`).
* **UI Navigation Optimization:** Fixed top navigation bar layouts to dynamically strip the "Find Job" reference for administrative/manager views (`minidu1`).
* **Code Optimization:** Cleaned out redundant codes, adjusted global API Key variable targets, and resolved `googleCalendar` script loading synchronization typos (`minidu1`).

### 2. Feature Lead Branch (`Shanilka`)
> 🔍 **Core Focus:** Candidate operational trace and testing
* **Mock Data Ingestion:** Written and executed database injection scripts to seed complete, real-world profile variations for initial candidate lookups.
* **Recruiter UX Sourcing:** Standardized candidate search layout boxes inside the recruiter module dashboard interface.

### 3. Backend Architecture Branch (`Darshana`)
> 🔍 **Core Focus:** Core page controllers & database extensions
* **Role-Based UI Views:** Configured profile pages and job detail sub-views to adapt visually depending on JWT authorization roles.
* **CV Parsing Hook:** Implemented file upload buffers to capture raw candidate document strings for AI reading capability.
* **Applicant Metrical Trace:** Created tracking mechanisms to trace total job-specific applicant counts natively on the dashboard grids.
* **Projects Infrastructure:** Added structural database tables mapping specific candidate independent/academic projects.

### 4. Infrastructure & Configuration Branch (`kawya`)
> 🔍 **Core Focus:** Infrastructure configuration, cleanup, & deployment
* **Modern Runtime Target:** Configured build scripts and compiled backend pipelines to execute safely on the latest `.NET 10.0` runtime.
* **Security & Environment Cleansing:** Implemented a robust corporate `.gitignore` pattern masking sensitive operational tokens and IDE settings.
* **Legacy Codebase Purge:** Completely removed old app framework structures to ensure high-speed processing capabilities on clean code.

### 5. Third-Party Integrations Branch (`minidu`)
> 🔍 **Core Focus:** Google Calendar integrations & navbar tweaks
* **Reminder Dispatch Logic:** Built automated functions tracking upcoming interviews and injecting corresponding notification schedules into the Google Calendar API interface.

* ---

## 🚀 Getting Started & Installation Guide

Follow these steps to set up and run the dynamic talent acquisition platform environment locally:

### 📋 Prerequisites
Before installation, ensure you have the following frameworks and tools installed on your machine:
* **SDK Framework:** [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or higher installed.
* **Database Engine:** SQL Server / PostgreSQL (or LocalDB for local debugging lines).
* **Package Management:** NuGet Package Manager (bundled directly with your .NET CLI/IDE).
* **Code Editor:** Visual Studio 2022, Visual Studio Code, or JetBrains Rider.

### 💻 Local Setup Procedures

1. **Clone the Repository:**
   ```bash
   git clone [https://github.com/your-username/AI-Talent-Hub.git](https://github.com/your-username/AI-Talent-Hub.git)
   cd AI-Talent-Hub
2. **Set Up the Backend (.NET Web API):**
   Navigate to the backend project directory:
     ```bash
     cd backend
     ```
   Open the `appsettings.json` file and configure your database connection string under `ConnectionStrings:DefaultConnection` to match your local SQL Server / Database settings.
   Restore the NuGet packages and apply database migrations:
     ```bash
     dotnet restore
     dotnet ef database update
     ```
   Run the backend server:
     ```bash
     dotnet run
     ```
   The backend API server will start running locally (usually on `http://localhost:5000` or `https://localhost:5001`).

3. **Set Up the Frontend / Client Server (Node.js):**
   Open a new terminal window and navigate to the frontend environment directory:
     ```bash
     cd frontend
     ```
     ```
   Launch the client server using Node:
     ```bash
     node server.js
     ```
   The client portal will now be running on your designated local hosting port (usually `http://localhost:3000`).

---

## 🔒 Security & Contribution Guidelines
* **Environment Governance:** Do not push local configurations, system secrets, or sensitive credentials to public tracking branches. Ensure all unnecessary tracking profiles strictly adhere to the project's `.gitignore` rules.
* **Feature Branches:** Always branch off (`git checkout -b feature/your-feature-name`) to isolate new components or updates instead of committing directly onto active production trunks.   

