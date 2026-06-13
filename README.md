# ORUApi

ASP.NET Core 10 Minimal API for university management — admissions, students, courses, announcements, payments, and installment tracking.

## Stack

- **.NET 10** Minimal APIs
- **PostgreSQL** (Neon serverless) via Npgsql
- **Entity Framework Core** (code-first migrations)
- **JWT** authentication (Student + Admin roles)
- **Azure Blob Storage** / Azurite (document uploads)
- **Swagger** (OpenAPI)

## Quick Start

```powershell
# 1. Configure appsettings.json with your DB connection, JWT secret, etc.

# 2. Run Azurite for local file uploads (optional)
npm install -g azurite
azurite --skipApiVersionCheck

# 3. Run the API
dotnet watch
```

Open https://localhost:5099/swagger

Migrations run automatically on startup. A default SuperAdmin and seed data (courses, study levels) are created on first run.

## Response Format

All endpoints return a consistent envelope:

```json
{
  "success": true,
  "message": "Success",
  "data": { ... }
}
```

Error responses return `"success": false` with a message.

## Authentication

| Role | Policy |
|---|---|
| `SuperAdmin` | Full access |
| `AdmissionsOfficer` | Applications |
| `Bursar` | Financials, installment reviews |
| `AcademicAdvisor` | Grades, academics |
| `Student` | Self-service endpoints |

Include the token as `Authorization: Bearer <token>`.

---

## API Endpoints

### Auth

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/student/login` | Public | Student login (email or matric number) |
| POST | `/api/auth/admin/login` | Public | Admin login |
| POST | `/api/auth/student/change-password` | Student | Change own password |

### Admin Management (SuperAdmin only)

| Method | Route | Description |
|---|---|---|
| GET | `/api/admin/admins` | List all active admins |
| POST | `/api/admin/admins` | Create new admin account |
| DELETE | `/api/admin/admins/{id}` | Deactivate an admin |

### Students

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/students/me` | Student | My profile |
| GET | `/api/students/me/academics` | Student | My grades & courses |
| GET | `/api/students/me/financials` | Student | My tuition & payment history |
| POST | `/api/students/me/installments` | Student | Submit installment proof (multipart, 1-3 screenshots) |
| GET | `/api/students/me/installments` | Student | View my installment submissions |

#### Admin Student Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/admin/students` | Admin | List all students (filterable by program, status) |
| GET | `/api/admin/students/{id}` | Admin | Get student by ID |
| PATCH | `/api/admin/students/{id}/academics` | AcademicAdvisor | Update semester & GPA |
| POST | `/api/admin/students/{id}/grades` | AcademicAdvisor | Add/update course grade |
| PATCH | `/api/admin/students/{id}/status` | Admin | Suspend, reactivate, graduate |
| PATCH | `/api/admin/students/{id}/tuition` | Bursar | Set total tuition due |
| GET | `/api/admin/students/{id}/installments` | Bursar | View installment submissions |
| PATCH | `/api/admin/students/{studentId}/installments/{submissionId}` | Bursar | Approve/reject installment |

### Applications

| Method | Route | Description |
|---|---|---|
| POST | `/api/applications` | Submit admission application |
| GET | `/api/applications/study-levels` | List available study levels |
| GET | `/api/applications/status/{email}` | Check application status by email |
| POST | `/api/applications/{id}/documents` | Upload supporting documents |

#### Admin Application Endpoints (Admissions)

| Method | Route | Description |
|---|---|---|
| GET | `/api/admin/applications` | List applications (default: pending + under review) |
| GET | `/api/admin/applications/{id}` | Get single application |
| PATCH | `/api/admin/applications/{id}/status` | Update review status |
| POST | `/api/admin/applications/{id}/admit` | Admit applicant → creates student account |

### Courses

| Method | Route | Description |
|---|---|---|
| GET | `/api/courses` | List active courses (public) |
| GET | `/api/courses/{id}` | Get single course (public) |

#### Admin Course Endpoints (Admin)

| Method | Route | Description |
|---|---|---|
| GET | `/api/admin/courses` | List all courses (including deleted) |
| POST | `/api/admin/courses` | Create course |
| PATCH | `/api/admin/courses/{id}` | Update course |
| DELETE | `/api/admin/courses/{id}` | Soft-delete course |

### Study Levels (Admin)

| Method | Route | Description |
|---|---|---|
| GET | `/api/admin/study-levels` | List all study levels |
| POST | `/api/admin/study-levels` | Create study level |
| PATCH | `/api/admin/study-levels/{id}` | Update study level |
| DELETE | `/api/admin/study-levels/{id}` | Deactivate study level |

### Announcements

| Method | Route | Description |
|---|---|---|
| GET | `/api/announcements` | List published announcements (public) |
| GET | `/api/announcements/{id}` | Get single announcement (public) |

#### Admin Announcement Endpoints (Admin)

| Method | Route | Description |
|---|---|---|
| POST | `/api/admin/announcements` | Create announcement |
| PATCH | `/api/admin/announcements/{id}` | Edit announcement |
| DELETE | `/api/admin/announcements/{id}` | Delete announcement |
| POST | `/api/admin/announcements/{id}/images` | Upload images |
| DELETE | `/api/admin/announcements/{id}/images` | Remove image |

### Webhooks (Public)

| Method | Route | Description |
|---|---|---|
| POST | `/api/webhooks/paystack` | Paystack payment callback |
| POST | `/api/webhooks/monnify` | Monnify payment callback |
| POST | `/api/webhooks/flutterwave` | Flutterwave payment callback |

### Health

| Method | Route | Description |
|---|---|---|
| GET | `/api/health/basic` | Health check |

---

## Seed Data

On first run (empty tables):

| Table | Data |
|---|---|
| **Admins** | 1 SuperAdmin (configured in `appsettings.json` → `DefaultAdmin`) |
| **Courses** | 9 courses (Theology, Leadership, Business Admin, Education, Management, Computer Science, Guidance & Counseling, Conflict Resolution, Professional Certifications) |
| **StudyLevels** | 5 levels (Certificate, Diploma, Undergraduate, Postgraduate Diploma, Masters) |

## Deploy to Azure (Free)

```powershell
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force
az webapp deploy --name oru-api --resource-group oru-api-rg --src-path ./deploy.zip --type zip
```

Create the App Service on **F1 (Free)** Linux plan with runtime `.NET 10`. Configure the connection string and JWT secret in **App settings** via the Azure portal.
