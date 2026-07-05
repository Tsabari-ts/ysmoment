# YsMoment — Live Magnet Event System

מערכת ניהול הזמנות מגנטים בזמן אמת לאירועים.

## Tech Stack

| שכבה | טכנולוגיה |
|------|-----------|
| Frontend | Angular 17 |
| Backend | .NET 8 Web API |
| Database | PostgreSQL 16 |
| Real-time | SignalR |
| WhatsApp | Stub (הדפסה לקונסול — מוכן לחיבור עתידי) |

## התחלה מהירה

### 1. PostgreSQL (אופציונלי)

בסביבת `Development` השרת משתמש כברירת מחדל ב-SQLite מקומי (`ysmoment.db`) — אין צורך ב-Docker כדי להריץ מקומית.
הפעילו PostgreSQL רק אם ברצונכם לבדוק התנהגות ספציפית ל-Postgres לפני production:

```bash
docker compose up -d
```

### 2. Backend

```bash
cd backend/YsMoment.Api
dotnet run
```

API: http://localhost:5000/swagger

### 3. Frontend

```bash
cd frontend
npm start
```

App: http://localhost:4200

### התחברות מנהל (ברירת מחדל)

- **משתמש:** `admin`
- **סיסמה:** `admin123`

## מסלולים

| נתיב | תיאור |
|------|--------|
| `/login` | התחברות מנהל |
| `/admin/events/new` | יצירת אירוע + QR |
| `/admin/events/:id` | Dashboard מנהל |
| `/admin/events/:id/summary` | סיכום אירוע |
| `/e/:slug` | עמוד אורח (QR) |

## WhatsApp

כרגע כל ההודעות מודפסות לקונסול של ה-API.
הממשק `IWhatsAppService` מוכן לחיבור ספק אמיתי (Twilio, Green API וכו').

## אבטחה (MVP)

- Rate limiting על הזמנות אורחים
- ולידציית תמונה בשרת (magic bytes)
- JWT לאדמין
- מחיקת תמונה פיזית מ-storage כשסטטוס = מוכן

## Production Deployment

Full step-by-step instructions (Neon, Cloudinary, Render, Vercel) live in [DEPLOYMENT.md](./DEPLOYMENT.md). Summary:

| Layer | Service | Notes |
|-------|---------|-------|
| Frontend | Vercel | Static Angular build, SPA rewrite via `frontend/vercel.json` |
| Backend API | Render (free) | Docker deploy from `backend/Dockerfile`, health check at `/health` |
| Database | Neon PostgreSQL (free) | Connection string via `ConnectionStrings__DefaultConnection` |
| Image storage | Cloudinary (free) | Used automatically in non-Development environments |

**Before deploying, set these on the backend host** (see `backend/.env.example` for the full list):
`ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`, `Jwt__Key` (≥32 random chars), `Jwt__Issuer`, `Jwt__Audience`, `Admin__Username`, `Admin__Password`, `App__GuestBaseUrl`, `Cors__Origins__0`, `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`.

The API now **fails fast at startup** in any non-Development environment if `Jwt:Key`, `App:GuestBaseUrl`, or `Cors:Origins` are missing — this surfaces misconfiguration as a crash-on-boot in the host logs instead of silently generating broken guest/QR links or a locked-down CORS policy.

**Before deploying, set these on the frontend host** (see `frontend/.env.example`):
`API_URL` (backend URL, no trailing slash), `FRONTEND_URL` (frontend URL, no trailing slash). Both are injected into `environment.prod.ts` at build time by `frontend/set-env.js` — never commit real values into that file.

**Deployment checklist** — see the full one in [DEPLOYMENT.md](./DEPLOYMENT.md#production-checklist).
