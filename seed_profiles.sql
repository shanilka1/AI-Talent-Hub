SET @amaya_id = (SELECT Id FROM Users WHERE Email = 'amaya.perera@gmail.com');
SET @tharindu_id = (SELECT Id FROM Users WHERE Email = 'tharindu.silva@gmail.com');
SET @nimesha_id = (SELECT Id FROM Users WHERE Email = 'nimesha.fernando@gmail.com');
SET @kasun_id = (SELECT Id FROM Users WHERE Email = 'kasun.rajapaksha@gmail.com');
SET @dilani_id = (SELECT Id FROM Users WHERE Email = 'dilani.wickrama@gmail.com');

INSERT INTO CandidateProfiles (UserId, Bio, Skills, ExperienceJson, EducationJson, ProjectsJson, ResumePath, UpdatedAt) VALUES
(
  @amaya_id,
  'Passionate full-stack developer with 3 years of experience building scalable web applications. Strong background in React and Node.js.',
  'React;Node.js;JavaScript;TypeScript;MongoDB;CSS;HTML;Git;REST APIs',
  '[{"title":"Frontend Developer","company":"CodeCraft Solutions","years":"2022-Present"},{"title":"Junior Developer","company":"Tech Startup LK","years":"2021-2022"}]',
  '[{"degree":"BSc Computer Science","school":"University of Colombo","year":"2021"}]',
  '[{"title":"E-Commerce Platform","description":"Built a full-stack online store using React and Node.js"},{"title":"Task Manager App","description":"Real-time task management tool with WebSocket support"}]',
  '', NOW()
),
(
  @tharindu_id,
  'Machine Learning Engineer specialising in NLP and computer vision. Experienced in building AI-powered solutions for enterprise clients.',
  'Python;TensorFlow;PyTorch;Scikit-learn;NLP;Computer Vision;SQL;Docker;AWS;Keras',
  '[{"title":"ML Engineer","company":"AI Solutions Lanka","years":"2023-Present"},{"title":"Data Analyst","company":"Virtusa","years":"2021-2023"}]',
  '[{"degree":"BSc Data Science","school":"University of Moratuwa","year":"2021"}]',
  '[{"title":"Sentiment Analysis Tool","description":"NLP model for customer feedback classification with 94% accuracy"}]',
  '', NOW()
),
(
  @nimesha_id,
  'UI/UX Designer and Frontend Developer with a strong eye for design. I bring wireframes to life with clean, accessible code.',
  'Figma;Adobe XD;HTML;CSS;JavaScript;React;Vue.js;SASS;Tailwind CSS;Accessibility',
  '[{"title":"UI/UX Developer","company":"Creative Agency Colombo","years":"2022-Present"}]',
  '[{"degree":"BSc Information Technology","school":"SLIIT","year":"2022"}]',
  '[{"title":"Travel App Redesign","description":"Redesigned a travel booking app resulting in 40% increase in conversions"}]',
  '', NOW()
),
(
  @kasun_id,
  'DevOps Engineer with expertise in cloud infrastructure, CI/CD pipelines and container orchestration. AWS Certified Solutions Architect.',
  'AWS;Azure;Docker;Kubernetes;Terraform;Jenkins;Linux;Python;Bash;CI/CD;Ansible',
  '[{"title":"Senior DevOps Engineer","company":"WSO2","years":"2021-Present"},{"title":"Cloud Engineer","company":"99X Technology","years":"2019-2021"}]',
  '[{"degree":"BSc Computer Engineering","school":"University of Peradeniya","year":"2019"}]',
  '[{"title":"Zero-Downtime Deployment Pipeline","description":"Implemented blue-green deployments saving 8 hours of downtime per month"}]',
  '', NOW()
),
(
  @dilani_id,
  'Mobile developer focused on Flutter and React Native. Delivered 10+ apps to the App Store and Google Play with excellent user ratings.',
  'Flutter;React Native;Dart;JavaScript;Firebase;SQLite;REST APIs;Git;Android;iOS',
  '[{"title":"Mobile Developer","company":"Inivos","years":"2022-Present"},{"title":"Junior Mobile Dev","company":"Pearson Lanka","years":"2021-2022"}]',
  '[{"degree":"Higher National Diploma in IT","school":"NIBM","year":"2021"}]',
  '[{"title":"Health Tracker App","description":"Cross-platform health monitoring app with 50k+ downloads"},{"title":"Food Delivery App","description":"Flutter app with real-time order tracking and payment integration"}]',
  '', NOW()
);
