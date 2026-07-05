# YsMoment — Production Deployment Guide

## Architecture

| Layer | Service | Free Tier |
|-------|---------|-----------|
| Frontend | Vercel | Yes |
| Backend API | Render | Yes (sleeps after 15 min idle) |
| Database | Neon PostgreSQL | Yes (0.5 GB storage) |
| Image Storage | Cloudinary | Yes (25 GB storage/bandwidth) |

---

## Step 0 — Prerequisites

- GitHub account (push this repo there first)
- Neon account: https://neon.tech
- Cloudinary account: https://cloudinary.com
- Render account: https://render.com
- Vercel account: https://vercel.com

---

## Step 1 — Database (Neon)

1. Create a new project at https://neon.tech
2. Copy the connection string — it looks like:  
   `postgresql://user:password@ep-xxx.us-east-2.aws.neon.tech/ysmoment?sslmode=require`
3. Convert it to the format: `Host=ep-xxx.us-east-2.aws.neon.tech;Port=5432;Database=ysmoment;Username=user;Password=password;SSL Mode=Require`
4. Save this as `ConnectionStrings__DefaultConnection`

---

## Step 2 — Cloudinary

1. Sign up at https://cloudinary.com (free tier)
2. Go to Dashboard → copy: Cloud Name, API Key, API Secret
3. These become: `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`

---

## Step 3 — Backend on Render

1. Push the repo to GitHub
2. Go to https://render.com → New → Web Service
3. Connect to your GitHub repo
4. Set these fields:
   - **Name:** `ysmoment-api`
   - **Root Directory:** `backend`
   - **Build Command:** *(leave blank — uses Dockerfile)*
   - **Start Command:** *(leave blank — Docker CMD)*
   - **Docker:** select "Use Dockerfile"
   - **Dockerfile path:** `backend/Dockerfile`
5. Set environment variables (Settings → Environment):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<from Step 1>
Jwt__Key=<run: openssl rand -base64 48>
Jwt__Issuer=YsMoment
Jwt__Audience=YsMoment
Admin__Username=admin
Admin__Password=<your strong password>
Cloudinary__CloudName=<from Step 2>
Cloudinary__ApiKey=<from Step 2>
Cloudinary__ApiSecret=<from Step 2>
Cloudinary__Folder=ysmoment
Cors__Origins__0=<will fill after Step 4>
App__GuestBaseUrl=<will fill after Step 4>/e
```

6. **Health Check Path:** `/health`
7. Click **Deploy**

> After first deploy, copy the Render URL (e.g. `https://ysmoment-api.onrender.com`)

---

## Step 4 — Frontend on Vercel

1. Go to https://vercel.com → New Project → Import from GitHub
2. Set **Root Directory** to `frontend`
3. Set these environment variables in Vercel (Settings → Environment Variables):

```
API_URL=https://ysmoment-api.onrender.com
FRONTEND_URL=https://your-project.vercel.app
```

> Get the Vercel URL from the Vercel dashboard after the first automatic deploy.  
> Then go back to Render and update `Cors__Origins__0` and `App__GuestBaseUrl`.

4. **Build Command:** `npm run build:prod`  
   *(Vercel may auto-detect this from package.json)*
5. **Output Directory:** `dist/frontend/browser`
6. Deploy

---

## Step 5 — Update CORS and Guest URL

After both are deployed:

1. In Render environment → set:
   - `Cors__Origins__0` = `https://your-project.vercel.app`
   - `App__GuestBaseUrl` = `https://your-project.vercel.app/e`
2. Trigger a new deploy on Render

---

## Step 6 — Verify

Run these checks in order:

- [ ] `GET https://ysmoment-api.onrender.com/health` → returns `Healthy`
- [ ] `GET https://your-project.vercel.app/` → loads login page
- [ ] Login with the admin credentials you set
- [ ] Create a test event → QR code URL should point to the Vercel frontend
- [ ] Open the guest URL on mobile → order form loads
- [ ] Submit an order with a photo → uploads successfully
- [ ] Admin dashboard shows the new order in real-time
- [ ] Update order status to Ready → dashboard updates
- [ ] Refresh `/admin/events/uuid` directly → page loads (not 404)
- [ ] Refresh `/e/slug` on mobile → page loads (not 404)

---

## Environment Variables Reference

### Backend (Render)

| Variable | Required | Description |
|----------|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Yes | Must be `Production` |
| `ConnectionStrings__DefaultConnection` | Yes | Neon PostgreSQL connection string |
| `Jwt__Key` | Yes | Random string ≥ 32 chars (use `openssl rand -base64 48`) |
| `Jwt__Issuer` | Yes | `YsMoment` |
| `Jwt__Audience` | Yes | `YsMoment` |
| `Admin__Username` | Yes | Admin login username |
| `Admin__Password` | Yes | Admin login password (strong!) |
| `App__GuestBaseUrl` | Yes | `https://your-frontend.vercel.app/e` |
| `Cloudinary__CloudName` | Yes | From Cloudinary dashboard |
| `Cloudinary__ApiKey` | Yes | From Cloudinary dashboard |
| `Cloudinary__ApiSecret` | Yes | From Cloudinary dashboard |
| `Cloudinary__Folder` | No | Default: `ysmoment` |
| `Cors__Origins__0` | Yes | `https://your-frontend.vercel.app` |

### Frontend (Vercel — build time)

| Variable | Required | Description |
|----------|----------|-------------|
| `API_URL` | Yes | Backend URL without trailing slash |
| `FRONTEND_URL` | Yes | Frontend URL without trailing slash |

---

## Free Tier Limitations

| Service | Limitation |
|---------|------------|
| Render (free) | Spins down after 15 min idle; first request takes ~30 sec |
| Neon (free) | 0.5 GB storage, suspends after 5 days inactivity |
| Cloudinary (free) | 25 GB storage, 25 GB bandwidth/month |
| Vercel (free) | 100 GB bandwidth/month, unlimited deployments |

To eliminate the Render cold-start delay, upgrade to Render Starter ($7/month) or use Railway (free tier has 500 hours/month).

---

## Production Checklist

- [ ] `Jwt__Key` is a random secret ≥ 32 characters, not the dev default
- [ ] `Admin__Password` is a strong password, not `admin123`
- [ ] `ConnectionStrings__DefaultConnection` points to Neon, not localhost
- [ ] `Cors__Origins__0` matches the exact Vercel URL (no trailing slash)
- [ ] `App__GuestBaseUrl` matches the exact Vercel URL + `/e` (no trailing slash)
- [ ] `/health` endpoint returns `Healthy`
- [ ] QR codes in event creation link to the correct Vercel domain
- [ ] Refreshing `/e/slug` on mobile does not return 404
- [ ] Image upload works and image appears in admin dashboard
- [ ] Status change in dashboard triggers real-time update
- [ ] SQLite (`ysmoment.db`) is NOT in the repository
- [ ] `appsettings.json` does NOT contain passwords or JWT keys
- [ ] `environment.prod.ts` in the repo contains only `__PLACEHOLDER__` values
