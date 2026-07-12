# Deployment Guide — AI Talent Hub

## Overview

| Component | Platform | URL |
|-----------|----------|-----|
| **Frontend** | GitHub Pages | `https://shanilka1.github.io/AI-Talent-Hub/` |
| **Backend API (Option A)** | Azure App Service (Free) | `https://ai-talent-hub-api.azurewebsites.net/` |
| **Backend API (Option B)** | Render.com (Free) | `https://ai-talent-hub-api.onrender.com` |

---

## Option A: Deploy Backend to Azure App Service (Recommended for SQLite persistence)

Azure App Service (Free F1 SKU / Education) is a great free hosting option. Since it has a persistent `/home` directory, your SQLite database file will not be deleted when the application restarts.

### Step 1 — Navigate to your App Service
1. Open the [Azure Portal](https://portal.azure.com).
2. In the top search bar, search for your App Service: **`ai-talent-hub-api`** and click it.

### Step 2 — Configure Deployment Center
1. In the left sidebar of your App Service page, click **Deployment Center** (under the **Deployment** section).
2. Set the following settings:
   - **Source**: Select **GitHub**. (If not authorized, click Authorize and log in to your GitHub account).
   - **Organization**: Select your organization/username (e.g., `shanilka1`).
   - **Repository**: Select **`AI-Talent-Hub`**.
   - **Branch**: Select **`main`**.
   - **Build provider**: Select **GitHub Actions**.
3. Click the **Save** button at the top menu.

### Step 3 — Pull and Update GitHub Actions Workflow (if it fails)
Because the backend code is located inside the `Backend/` folder (and not the root), the auto-generated GitHub Actions workflow might fail. If it fails:
1. I will help you pull the new workflow file that Azure commits to your repository.
2. We will modify the workflow file to point to the `Backend` directory.
3. We will push the fix to GitHub to make it build and deploy successfully.

---

## Option B: Deploy Backend to Render

If you prefer Render, you can host the Docker container there.

### Step 1 — Create Render Account
Go to [https://render.com](https://render.com) and sign up (free). Connect your GitHub account.

### Step 2 — Fix "Repository not showing" on Render
If you cannot see the `AI-Talent-Hub` repository when creating a new Web Service:
1. On the Render repository connection page, click **"Configure GitHub App"** or **"Adjust permissions on GitHub"**.
2. This will open GitHub. Scroll down to **Repository access**.
3. Select **"Only select repositories"** and choose **`AI-Talent-Hub`** (or select **"All repositories"**).
4. Click **Save**.
5. Go back to Render, refresh, and the repository will now show up!

### Step 3 — Create Web Service
1. Click **"New +"** → **"Web Service"**
2. Connect the repo: **`shanilka1/AI-Talent-Hub`**
3. Fill in the settings:

| Setting | Value |
|---------|-------|
| **Name** | `ai-talent-hub-api` |
| **Root Directory** | `Backend` |
| **Environment** | `Docker` |
| **Instance Type** | `Free` |
| **Branch** | `main` |

### Step 4 — Environment Variables
In the **Environment** tab, add these variables:

| Key | Value |
|-----|-------|
| `Jwt__Key` | `SuperSecretKeyForAITalentHubAuthentication2026!#$` |
| `Jwt__Issuer` | `AITalentHub` |
| `Jwt__Audience` | `AITalentHubUsers` |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/aitalenthub.db` |

### Step 5 — Deploy
Click **"Create Web Service"**. Render will build and deploy the container (~3-5 minutes).

---

## Part 3: Update Frontend API URL

Depending on which platform you use, open **`Frontend/services/api.js`** and make sure the production URL matches your deployed API:

```js
const API_BASE_URL = IS_LOCAL
    ? 'http://localhost:5260/api'
    : 'https://YOUR-DEPLOYED-URL.onrender.com/api'; // Or .azurewebsites.net/api
```

---

## ⚠️ Known Limitations (Free Tier)

| Issue | Cause | Fix |
|-------|-------|-----|
| Backend sleeps after 15 min idle (Render) | Render free tier | Upgrade to paid or use UptimeRobot to ping every 10 min |
| DB resets on redeploy (Render Docker) | No persistent storage | Use Azure App Service or connect to an external DB like Turso (free) |
| First request slow (~30s) | Cold start after sleep | Expected behavior on free tier |

---

## Local Development (unchanged)

Backend:
```bash
cd Backend
dotnet run
# Runs on http://localhost:5260
```

Frontend:
```bash
# Open Frontend/index.html with Live Server in VS Code
# OR use any static file server
```

