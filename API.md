# ORU API — Frontend Integration Guide

**Base URL**: `https://localhost:5099` (dev) / `https://oru-api.azurewebsites.net` (prod)

---

## 1. Response Format

Every endpoint returns:

```json
{
  "success": true,
  "message": "Success",
  "data": { ... }
}
```

**Errors** return the same shape with `"success": false`:

```json
{
  "success": false,
  "message": "Course not found.",
  "data": null
}
```

| HTTP Status | Meaning |
|---|---|
| `200` | Success |
| `201` | Created |
| `400` | Validation error (check `message`) |
| `401` | Missing or invalid token |
| `403` | Insufficient role |
| `404` | Resource not found |
| `409` | Duplicate/conflict |

---

## 2. Authentication

### 2.1 Student Login

```
POST /api/auth/student/login
Content-Type: application/json
```

**Request:**
```json
{
  "matricNumberOrEmail": "CSC/2024/001",
  "password": "CSC/2024/001"
}
```

**Response `200`:**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOi...",
    "role": "Student",
    "name": "John Doe",
    "id": "a1b2c3d4-..."
  }
}
```

### 2.2 Admin Login

```
POST /api/auth/admin/login
Content-Type: application/json
```

**Request:**
```json
{
  "email": "admin@example.com",
  "password": "Admin@123"
}
```

**Response `200`:**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOi...",
    "role": "SuperAdmin",
    "name": "Default SuperAdmin",
    "id": "6a009981-..."
  }
}
```

### 2.3 Using the token

Include in all authenticated requests:

```
Authorization: Bearer <token>
```

**Token lifespan**: 7 days.

**Student token claims**: `nameid` (GUID), `email`, `matricNumber`, `role` = `"Student"`  
**Admin token claims**: `nameid` (GUID), `email`, `staffId`, `role` = `"SuperAdmin"`, `"AdmissionsOfficer"`, `"Bursar"`, or `"AcademicAdvisor"`

### 2.4 Admin roles & permissions

| Role | Can access |
|---|---|
| `SuperAdmin` | Everything |
| `AdmissionsOfficer` | Applications (list, review, admit) |
| `Bursar` | Tuition, payment history, installment review |
| `AcademicAdvisor` | Grades, semester, GPA |

---

## 3. Enums & Lookups

### 3.1 Study Levels

```
GET /api/applications/study-levels
```
*Public — no auth required*

**Response `200`:**
```json
{
  "success": true,
  "data": [
    { "value": 1, "label": "Professional Certifications & Short-Term Courses (3-6 months or 1 year)" },
    { "value": 2, "label": "Diploma Programs (2 years)" },
    { "value": 3, "label": "Undergraduate / Bachelor's Degree (4 years)" },
    { "value": 4, "label": "Postgraduate Diploma (PGDE, PDE)" },
    { "value": 5, "label": "Master's Degree (M.Ed., etc.)" }
  ]
}
```

### 3.2 Application Status

| Value | Label |
|---|---|
| `0` | Pending |
| `1` | UnderReview |
| `2` | Approved |
| `3` | Rejected |

### 3.3 Account Status

| Value | Label |
|---|---|
| `0` | Active |
| `1` | Suspended |
| `2` | Graduated |

### 3.4 Announcement Category

| Value | Label |
|---|---|
| `0` | News |
| `1` | Event |
| `2` | Seminar |
| `3` | Academic |
| `4` | Gallery |

### 3.5 Installment Status

| Value | Label |
|---|---|
| `0` | Pending |
| `1` | Approved |
| `2` | Rejected |

---

## 4. Students (Self-Service)

*All require `Authorization: Bearer <student-token>`*

### 4.1 My Profile

```
GET /api/students/me
```

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "a1b2c3d4-...",
    "matricNumber": "CSC/2024/001",
    "fullName": "John Doe",
    "email": "john@example.com",
    "program": "Computer Science",
    "currentSemester": 1,
    "gpa": 3.5,
    "status": 0
  }
}
```

### 4.2 My Academic Records

```
GET /api/students/me/academics
```

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "matricNumber": "CSC/2024/001",
    "fullName": "John Doe",
    "program": "Computer Science",
    "currentSemester": 1,
    "gpa": 3.5,
    "enrolledCourses": [
      {
        "courseCode": "CSC101",
        "courseTitle": "Computer Science",
        "grade": "A",
        "creditUnits": 3,
        "semester": 1
      }
    ]
  }
}
```

### 4.3 My Financial Records

```
GET /api/students/me/financials
```

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "matricNumber": "CSC/2024/001",
    "fullName": "John Doe",
    "totalTuitionDue": 500000.00,
    "totalAmountPaid": 150000.00,
    "outstandingBalance": 350000.00,
    "paymentHistory": [
      {
        "id": "e5f6g7h8-...",
        "amount": 50000.00,
        "paymentReference": "pay_abc123",
        "gateway": "paystack",
        "paidAt": "2026-06-13T10:30:00Z"
      }
    ],
    "installmentSubmissions": [
      {
        "id": "i9j0k1l2-...",
        "installmentNumber": 1,
        "amount": 100000.00,
        "screenshotUrls": ["https://..."],
        "notes": "First installment payment",
        "status": 1,
        "submittedAt": "2026-06-12T08:00:00Z",
        "reviewedAt": "2026-06-12T10:00:00Z",
        "reviewedByAdminId": "6a009981-..."
      }
    ]
  }
}
```

### 4.4 Submit Installment Proof

```
POST /api/students/me/installments
Content-Type: multipart/form-data
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `installmentNumber` | int | Yes | `1`, `2`, or `3` |
| `amount` | decimal | Yes | Must be > 0 |
| `notes` | string | No | Optional note |
| `files` | file[] | Yes | 1 to 3 screenshot images |

**Response `201`:**
```json
{
  "success": true,
  "message": "Installment proof submitted successfully.",
  "data": {
    "id": "m3n4o5p6-...",
    "installmentNumber": 1,
    "amount": 100000.00,
    "screenshotUrls": ["https://..."],
    "notes": "First installment",
    "status": 0,
    "submittedAt": "2026-06-13T09:00:00Z",
    "reviewedAt": null,
    "reviewedByAdminId": null
  }
}
```

**Validation errors `400`:**
- `"Amount must be greater than zero."`
- `"Installment number must be 1, 2, or 3."`
- `"At least one screenshot is required."`
- `"Maximum 3 screenshots per installment."`
- `"File upload is currently unavailable. Azure Storage is not configured or reachable."`

### 4.5 My Installment Submissions

```
GET /api/students/me/installments
```

**Response `200`:**
```json
{
  "success": true,
  "data": [
    {
      "id": "m3n4o5p6-...",
      "installmentNumber": 1,
      "amount": 100000.00,
      "screenshotUrls": ["https://...", "https://..."],
      "notes": "First installment",
      "status": 0,
      "submittedAt": "2026-06-13T09:00:00Z",
      "reviewedAt": null,
      "reviewedByAdminId": null
    }
  ]
}
```

### 4.6 Change Password

```
POST /api/auth/student/change-password
Content-Type: application/json
```

**Request:**
```json
{
  "currentPassword": "oldpass",
  "newPassword": "newpass"
}
```

**Response `200`:**
```json
{
  "success": true,
  "message": "Password updated successfully.",
  "data": null
}
```

---

## 5. Applications (Public + Admissions)

### 5.1 Submit Application

```
POST /api/applications
Content-Type: application/json
```

**Request:**
```json
{
  "fullName": "Jane Smith",
  "email": "jane@example.com",
  "phone": "08012345678",
  "dateOfBirth": "2000-05-15",
  "address": "123 Main St, Lagos",
  "selectedProgram": "Computer Science",
  "studyLevelId": 3
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `fullName` | string | **Yes** | |
| `email` | string | **Yes** | |
| `phone` | string | No | |
| `dateOfBirth` | string | No | Format: `YYYY-MM-DD` |
| `address` | string | **Yes** | |
| `selectedProgram` | string | **Yes** | Any program name |
| `studyLevelId` | int | **Yes** | From `GET /api/applications/study-levels` |

**Response `201`:**
```json
{
  "success": true,
  "message": "Application submitted successfully.",
  "data": {
    "id": "b53b3b5b-...",
    "fullName": "Jane Smith",
    "email": "jane@example.com",
    "selectedProgram": "Computer Science",
    "studyLevelId": 3,
    "studyLevelName": "Undergraduate",
    "status": 0,
    "applicationFeePaid": false,
    "documents": [],
    "submittedAt": "2026-06-13T09:15:00Z"
  }
}
```

### 5.2 Check Application Status

```
GET /api/applications/status/{email}
```
*Public — no auth*

Returns the full application snapshot including `id` for document uploads and the list of already-uploaded documents.

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "b53b3b5b-38c6-4f3a-b9a0-123456789abc",
    "fullName": "Jane Smith",
    "selectedProgram": "Computer Science",
    "status": 1,
    "applicationFeePaid": false,
    "submittedAt": "2026-06-13T09:15:00Z",
    "documents": [
      {
        "id": "d7e8f9a0-1111-2222-3333-444455556666",
        "name": "Transcript",
        "fileUrl": "https://...",
        "fileName": "transcript.pdf",
        "contentType": "application/pdf",
        "fileSize": 245760,
        "uploadedAt": "2026-06-15T10:00:00Z"
      },
      {
        "id": "e8f9a0b1-2222-3333-4444-555566667777",
        "name": "Passport",
        "fileUrl": "https://...",
        "fileName": "passport.jpg",
        "contentType": "image/jpeg",
        "fileSize": 98304,
        "uploadedAt": "2026-06-15T10:05:00Z"
      }
    ]
  }
}
```

### 5.2a Frontend Flow

After submitting an application, the frontend should:

1. **Save the `id`** from `POST /api/applications` response — use it for all document endpoints.

2. **If the user returns later (only has their email)**, call `GET /api/applications/status/{email}` to retrieve the `id` and see which documents already exist.

3. **Document upload pattern:**
   ```
   // Upload new documents
   POST /api/applications/{id}/documents?name=Transcript
   Content-Type: multipart/form-data
   Body: the file
   
   // Replace a wrong upload
   PUT /api/applications/{id}/documents/{documentId}
   Content-Type: multipart/form-data
   Body: the corrected file
   
   // Remove a document
   DELETE /api/applications/{id}/documents/{documentId}
   ```

4. **Track uploaded vs required:** Maintain a list of expected document names (e.g. `["Transcript", "Passport", "Recommendation Letter"]`). The `documents` array in the status response tells you which have been uploaded. Show a checklist:
   - Transcript ✓ (tap to view/replace/delete)
   - Passport ✓ (tap to view/replace/delete)
   - Recommendation Letter ✗ (tap to upload)

### 5.3 Upload Documents

```
POST /api/applications/{id}/documents?name=Transcript
Content-Type: multipart/form-data
```
*Public — no auth*

| Field | Type | Required |
|---|---|---|
| `name` | string (query) | **Yes** — e.g. `Transcript`, `Passport`, `Recommendation Letter` |
| `files` | file[] | Yes |

**Response `200`:**
```json
{
  "success": true,
  "message": "2 document(s) uploaded successfully.",
  "data": [
    {
      "id": "d7e8f9a0-...",
      "name": "Transcript",
      "fileUrl": "https://...",
      "fileName": "transcript.pdf",
      "contentType": "application/pdf",
      "fileSize": 245760,
      "uploadedAt": "2026-06-15T10:00:00Z"
    }
  ]
}
```

### 5.4 Replace Document

```
PUT /api/applications/{id}/documents/{documentId}
Content-Type: multipart/form-data
```
*Public — no auth*

Replaces a previously uploaded document (e.g. wrong file was submitted).

| Field | Type | Required |
|---|---|---|
| `file` | file | Yes — the corrected file |

**Response `200`:**
```json
{
  "success": true,
  "message": "Document replaced successfully.",
  "data": {
    "id": "d7e8f9a0-...",
    "name": "Transcript",
    "fileUrl": "https://...",
    "fileName": "transcript-corrected.pdf",
    "contentType": "application/pdf",
    "fileSize": 251904,
    "uploadedAt": "2026-06-15T11:30:00Z"
  }
}
```

### 5.5 Delete Document

```
DELETE /api/applications/{id}/documents/{documentId}
```
*Public — no auth*

Removes a document and its file from blob storage.


### 5.6 Admin: List Applications

```
GET /api/admin/applications?status=0&page=1&pageSize=20
```
*Requires: AdmissionsOfficer or SuperAdmin*

**Default filter**: Only returns `Pending` (0) and `UnderReview` (1). Pass `?status=2` for Approved, `?status=3` for Rejected.

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "total": 15,
    "page": 1,
    "pageSize": 20,
    "items": [
      {
        "id": "b53b3b5b-...",
        "fullName": "Jane Smith",
        "email": "jane@example.com",
        "selectedProgram": "Computer Science",
        "studyLevelId": 3,
        "studyLevelName": "Undergraduate",
        "status": 0,
        "applicationFeePaid": false,
        "documents": [],
        "submittedAt": "2026-06-13T09:15:00Z"
      }
    ]
  }
}
```

### 5.7 Admin: Get Single Application

```
GET /api/admin/applications/{id}
```
*Requires: AdmissionsOfficer or SuperAdmin*

### 5.8 Admin: Update Application Status

```
PATCH /api/admin/applications/{id}/status
Content-Type: application/json
```
*Requires: AdmissionsOfficer or SuperAdmin*

**Request:**
```json
{
  "status": 2
}
```
`status` values: `0`=Pending, `1`=UnderReview, `2`=Approved, `3`=Rejected

### 5.9 Admin: Admit Student

```
POST /api/admin/applications/{id}/admit
```
*Requires: AdmissionsOfficer or SuperAdmin*

Creates a student account from an approved application. Default password = matric number.

**Response `200`:**
```json
{
  "success": true,
  "message": "Student admitted successfully. Default password is their matric number.",
  "data": {
    "matricNumber": "CSC/2026/001",
    "fullName": "Jane Smith",
    "defaultPassword": "CSC/2026/001"
  }
}
```

---

## 6. Admin: Student Management

### 6.1 List All Students

```
GET /api/admin/students?program=Computer Science&status=0&page=1&pageSize=20
```
*Requires: AdminOnly*

Filters: `program` (string), `status` (`0`=Active, `1`=Suspended, `2`=Graduated)

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "total": 45,
    "page": 1,
    "pageSize": 20,
    "students": [
      {
        "id": "a1b2c3d4-...",
        "matricNumber": "CSC/2026/001",
        "fullName": "Jane Smith",
        "email": "jane@example.com",
        "program": "Computer Science",
        "currentSemester": 1,
        "gpa": 0.0,
        "status": 0
      }
    ]
  }
}
```

### 6.2 Get Student by ID

```
GET /api/admin/students/{id}
```
*Requires: AdminOnly*

### 6.3 Update Academics

```
PATCH /api/admin/students/{id}/academics
Content-Type: application/json
```
*Requires: AcademicAdvisor or SuperAdmin*

**Request:**
```json
{
  "currentSemester": 2,
  "gpa": 3.8
}
```

### 6.4 Add/Update Grade

```
POST /api/admin/students/{id}/grades
Content-Type: application/json
```
*Requires: AcademicAdvisor or SuperAdmin*

**Request:**
```json
{
  "courseCode": "CSC101",
  "courseTitle": "Intro to Computer Science",
  "grade": "A",
  "semester": 1,
  "creditUnits": 3
}
```

Updates existing grade if same `courseCode` + `semester` combo exists, otherwise adds new.

### 6.5 Update Student Status

```
PATCH /api/admin/students/{id}/status
Content-Type: application/json
```
*Requires: AdminOnly*

**Request:**
```json
{
  "status": 1
}
```

`status`: `0`=Active, `1`=Suspended, `2`=Graduated

### 6.6 Set Tuition

```
PATCH /api/admin/students/{id}/tuition
Content-Type: application/json
```
*Requires: Bursar or SuperAdmin*

**Request:**
```json
{
  "totalTuitionDue": 500000.00
}
```

### 6.7 View Student Installments

```
GET /api/admin/students/{id}/installments
```
*Requires: Bursar or SuperAdmin*

**Response `200`:**
```json
{
  "success": true,
  "data": [
    {
      "id": "m3n4o5p6-...",
      "installmentNumber": 1,
      "amount": 100000.00,
      "screenshotUrls": ["https://..."],
      "notes": "First installment",
      "status": 0,
      "submittedAt": "2026-06-13T09:00:00Z",
      "reviewedAt": null,
      "reviewedByAdminId": null
    }
  ]
}
```

### 6.8 Review Installment (Approve/Reject)

```
PATCH /api/admin/students/{studentId}/installments/{submissionId}
Content-Type: application/json
```
*Requires: Bursar or SuperAdmin*

**Request:**
```json
{
  "status": 1
}
```

`status`: `1`=Approved, `2`=Rejected

On approval, the amount is automatically added to `totalAmountPaid` and a `PaymentInstallment` record is created in `paymentHistory`.

**Rules:**
- Cannot re-review an already approved/rejected submission
- Returns `400` if already reviewed

---

## 7. Courses

### 7.1 List Courses (Public)

```
GET /api/courses?page=1&pageSize=20
```

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "total": 9,
    "page": 1,
    "pageSize": 20,
    "items": [
      {
        "id": "c1d2e3f4-...",
        "code": "CSC101",
        "title": "Computer Science",
        "description": "Introduction to computer science fundamentals",
        "creditUnits": 3,
        "isActive": true,
        "createdByAdminId": "6a009981-...",
        "updatedByAdminId": null,
        "createdAt": "2026-06-13T00:00:00Z",
        "updatedAt": null
      }
    ]
  }
}
```

### 7.2 Get Course (Public)

```
GET /api/courses/{id}
```

### 7.3 Admin: List All Courses

```
GET /api/admin/courses?includeDeleted=true&page=1&pageSize=20
```
*Requires: AdminOnly*

Set `includeDeleted=true` to include soft-deleted courses.

### 7.4 Admin: Create Course

```
POST /api/admin/courses
Content-Type: application/json
```
*Requires: AdminOnly*

**Request:**
```json
{
  "code": "MAT201",
  "title": "Advanced Mathematics",
  "description": "Calculus and linear algebra",
  "creditUnits": 4
}
```

**Response `201`**

### 7.5 Admin: Update Course

```
PATCH /api/admin/courses/{id}
Content-Type: application/json
```
*Requires: AdminOnly*

```json
{
  "title": "Advanced Mathematics II",
  "creditUnits": 5,
  "isActive": true
}
```
All fields optional — only send what changed.

### 7.6 Admin: Delete Course

```
DELETE /api/admin/courses/{id}
```
*Requires: AdminOnly*

Soft-deletes (sets `isDeleted = true`, tracks which admin deleted it).

---

## 8. Study Levels (Admin)

### 8.1 List All

```
GET /api/admin/study-levels
```
*Requires: AdminOnly*

**Response `200`:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Certificate",
      "description": "Professional Certifications & Short-Term Courses (3-6 months or 1 year)",
      "duration": "3-12 months",
      "sortOrder": 1,
      "isActive": true,
      "createdAt": "2026-06-13T00:00:00Z",
      "updatedAt": null
    }
  ]
}
```

### 8.2 Create

```
POST /api/admin/study-levels
Content-Type: application/json
```
*Requires: AdminOnly*

```json
{
  "name": "PhD",
  "description": "Doctoral Degree",
  "duration": "3-5 years",
  "sortOrder": 6
}
```

### 8.3 Update

```
PATCH /api/admin/study-levels/{id}
Content-Type: application/json
```
*Requires: AdminOnly*

All fields optional.

### 8.4 Deactivate

```
DELETE /api/admin/study-levels/{id}
```
*Requires: AdminOnly*

Soft-deactivates (sets `isActive = false`). Won't appear in public dropdown.

---

## 9. Announcements

### 9.1 List Published (Public)

```
GET /api/announcements?category=Event&page=1&pageSize=10
```

`category` filter: `News`, `Event`, `Seminar`, `Academic`, `Gallery`

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "total": 5,
    "page": 1,
    "pageSize": 10,
    "items": [
      {
        "id": "d4e5f6g7-...",
        "title": "Orientation Week",
        "content": "Welcome all new students...",
        "category": 1,
        "imageUrls": ["https://..."],
        "postedByAdminId": "6a009981-...",
        "publishedAt": "2026-06-10T08:00:00Z",
        "isPublished": true
      }
    ]
  }
}
```

### 9.2 Get Single (Public)

```
GET /api/announcements/{id}
```
Only returns published announcements.

### 9.3 Admin: Create

```
POST /api/admin/announcements
Content-Type: application/json
```
*Requires: AdminOnly*

```json
{
  "title": "Exam Timetable",
  "content": "Final exams start July 1st...",
  "category": 3
}
```

`category`: `0`=News, `1`=Event, `2`=Seminar, `3`=Academic, `4`=Gallery

### 9.4 Admin: Update

```
PATCH /api/admin/announcements/{id}
Content-Type: application/json
```
*Requires: AdminOnly*

```json
{
  "title": "Updated Title",
  "isPublished": false
}
```
All fields optional.

### 9.5 Admin: Delete

```
DELETE /api/admin/announcements/{id}
```
*Requires: AdminOnly*

Hard delete.

### 9.6 Admin: Upload Images

```
POST /api/admin/announcements/{id}/images
Content-Type: multipart/form-data
```
*Requires: AdminOnly*

| Field | Type |
|---|---|
| `files` | file[] |

### 9.7 Admin: Remove Image

```
DELETE /api/admin/announcements/{id}/images?imageUrl=https://...
```
*Requires: AdminOnly*

---

## 10. Admin Management (SuperAdmin Only)

### 10.1 List Admins

```
GET /api/admin/admins
```
*Requires: SuperAdmin*

**Response `200`:**
```json
{
  "success": true,
  "data": [
    {
      "id": "6a009981-...",
      "staffId": "SA-001",
      "fullName": "Default SuperAdmin",
      "email": "admin@example.com",
      "role": 0,
      "permissions": {
        "canApproveApplications": true,
        "canPostAnnouncements": true,
        "canUpdateGrades": true,
        "canManageFinance": true,
        "canManageAdmins": true
      },
      "createdAt": "2026-06-13T00:00:00Z"
    }
  ]
}
```

### 10.2 Create Admin

```
POST /api/admin/admins
Content-Type: application/json
```
*Requires: SuperAdmin*

```json
{
  "staffId": "SA-002",
  "fullName": "Jane Bursar",
  "email": "jane@example.com",
  "password": "SecurePass123",
  "role": 2,
  "permissions": {
    "canApproveApplications": false,
    "canPostAnnouncements": false,
    "canUpdateGrades": false,
    "canManageFinance": true,
    "canManageAdmins": false
  }
}
```

`role`: `0`=SuperAdmin, `1`=AdmissionsOfficer, `2`=Bursar, `3`=AcademicAdvisor

### 10.3 Deactivate Admin

```
DELETE /api/admin/admins/{id}
```
*Requires: SuperAdmin*

Sets `isActive = false`. Cannot be undone via API.

---

## 11. Health

```
GET /api/health/basic
```

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "status": "Ok",
    "message": "Basic System Ok"
  }
}
```

---

## 12. Paginated Responses

Endpoints that return lists use this shape:

```json
{
  "success": true,
  "data": {
    "total": 45,
    "page": 1,
    "pageSize": 20,
    "items": [ ... ]
  }
}
```

| Endpoint | Property name |
|---|---|
| Applications | `items` |
| Courses | `items` |
| Announcements | `items` |
| Students | `students` |

---

## 13. Common Error Responses

| Code | Body |
|---|---|
| `400` | `{ "success": false, "message": "Please select a program.", "data": null }` |
| `401` | *(no body — invalid/missing token)* |
| `403` | *(no body — insufficient role)* |
| `404` | `{ "success": false, "message": "Student not found.", "data": null }` |
| `409` | `{ "success": false, "message": "A course with this code already exists.", "data": null }` |
