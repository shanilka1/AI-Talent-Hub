# Deployment Guide — AI Talent Hub

## Overview

| Component | Platform | URL |
|-----------|----------|-----|
| **Frontend** | GitHub Pages | `https://shanilka1.github.io/AI-Talent-Hub/` |
| **Backend API** | Render.com | `https://ai-talent-hub-api.onrender.com` |

---

## Part 1: Deploy Backend to Render

### Step 1 — Create Render Account
Go to [https://render.com](https://render.com) and sign up (free). Connect your GitHub account.

### Step 2 — New Web Service
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

### Step 3 — Environment Variables
In the **Environment** tab, add these variables:

| Key | Value |
|-----|-------|
| `Jwt__Key` | `SuperSecretKeyForAITalentHubAuthentication2026!#$` |
| `Jwt__Issuer` | `AITalentHub` |
| `Jwt__Audience` | `AITalentHubUsers` |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/aitalenthub.db` |

> ⚠️ **Tip:** Change `Jwt__Key` to a new random secret before going live!

### Step 4 — Deploy
Click **"Create Web Service"**. Render will:
1. Pull the code from GitHub
2. Build the Docker image (~3-5 minutes)
3. Start the container

Once deployed, your API URL will be:  
`https://ai-talent-hub-api.onrender.com`

Test it: open `https://ai-talent-hub-api.onrender.com/` in browser → should show `AI Talent Hub API is running successfully!`

---

## Part 2: Deploy Frontend to GitHub Pages

### Step 1 — Push to GitHub
```bash
git add .
git commit -m "chore: add hosting config for GitHub Pages + Render"
git push origin main
```

This will automatically trigger the GitHub Actions workflow.

### Step 2 — Enable GitHub Pages
1. Go to your repo: `https://github.com/shanilka1/AI-Talent-Hub`
2. Click **Settings** → **Pages** (left sidebar)
3. Under **Source**, select:
   - **Branch**: `gh-pages`
   - **Folder**: `/ (root)`
4. Click **Save**

### Step 3 — Wait for Deployment
- Go to **Actions** tab in your repo to watch the workflow run
- After ~1-2 minutes, your site will be live at:

`https://shanilka1.github.io/AI-Talent-Hub/`

---

## Part 3: Update Render Service Name (if different)

If your Render service gets a different URL (e.g., `ai-talent-hub-api-xxxx.onrender.com`), update the URL in:

**`Frontend/services/api.js`** — line 4:
```js
: 'https://YOUR-ACTUAL-RENDER-URL.onrender.com/api';
```

Then push again — GitHub Actions will redeploy the frontend automatically.

---

## ⚠️ Known Limitations (Free Tier)

| Issue | Cause | Fix |
|-------|-------|-----|
| Backend sleeps after 15 min idle | Render free tier | Upgrade to paid ($7/mo) or use UptimeRobot to ping every 10 min |
| DB resets on redeploy | No persistent storage | Add Render Disk (paid) or use an external DB like Turso (free) |
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
