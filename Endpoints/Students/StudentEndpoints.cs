using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ORUApi.Data;
using ORUApi.Models;
using ORUApi.Services;

namespace ORUApi.Endpoints.Students;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        group.MapGet("/me", GetMyProfile)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Get logged-in student's profile");

        group.MapGet("/me/academics", GetMyAcademics)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Get logged-in student's academic records");

        group.MapGet("/me/financials", GetMyFinancials)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Get logged-in student's financial records");

        group.MapPost("/me/installments", SubmitInstallment)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Submit installment payment proof (max 3 screenshots)")
            .DisableAntiforgery();

        group.MapGet("/me/installments", GetMyInstallments)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Get my installment submissions");

        var admin = app.MapGroup("/api/admin/students").WithTags("Admin: Students");

        admin.MapGet("/", GetAllStudents)
            .RequireAuthorization("AdminOnly")
            .WithSummary("List all students");

        admin.MapGet("/{id}", GetStudent)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Get a student by ID");

        admin.MapPatch("/{id}/academics", UpdateAcademics)
            .RequireAuthorization("AcademicAdvisor")
            .WithSummary("Update semester and GPA");

        admin.MapPost("/{id}/grades", AddOrUpdateGrade)
            .RequireAuthorization("AcademicAdvisor")
            .WithSummary("Add or update a course grade");

        admin.MapPatch("/{id}/status", UpdateStatus)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Suspend, reactivate, or graduate a student");

        admin.MapPatch("/{id}/tuition", UpdateTuition)
            .RequireAuthorization("Bursar")
            .WithSummary("Set total tuition amount due");

        admin.MapGet("/{id}/installments", GetStudentInstallments)
            .RequireAuthorization("Bursar")
            .WithSummary("View a student's installment submissions");

        admin.MapPatch("/{studentId}/installments/{submissionId}", ReviewInstallment)
            .RequireAuthorization("Bursar")
            .WithSummary("Approve or reject an installment submission");
    }

    static async Task<IResult> GetMyProfile(ClaimsPrincipal user, ORUDbContext db)
    {
        var id = GetStudentId(user);
        if (id is null) return Results.Unauthorized();
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToProfile(student)));
    }

    static async Task<IResult> GetMyAcademics(ClaimsPrincipal user, ORUDbContext db)
    {
        var id = GetStudentId(user);
        if (id is null) return Results.Unauthorized();
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToAcademics(student)));
    }

    static async Task<IResult> GetMyFinancials(ClaimsPrincipal user, ORUDbContext db)
    {
        var id = GetStudentId(user);
        if (id is null) return Results.Unauthorized();
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToFinancials(student)));
    }

    static async Task<IResult> GetAllStudents(
        ORUDbContext db,
        string? program = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.Students.AsQueryable();

        if (!string.IsNullOrEmpty(program))
            query = query.Where(s => s.Program == program);

        if (Enum.TryParse<AccountStatus>(status, out var parsedStatus))
            query = query.Where(s => s.Status == parsedStatus);

        var total = await query.CountAsync();
        var students = await query
            .OrderBy(s => s.MatricNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => StudentMapper.ToProfile(s))
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(new { total, page, pageSize, students }));
    }

    static async Task<IResult> GetStudent(Guid id, ORUDbContext db)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToProfile(student)));
    }

    static async Task<IResult> UpdateAcademics(
        Guid id, UpdateAcademicsRequest req, ORUDbContext db)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        if (req.CurrentSemester.HasValue) student.CurrentSemester = req.CurrentSemester.Value;
        if (req.GPA.HasValue) student.GPA = req.GPA.Value;
        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToAcademics(student), "Academics updated successfully."));
    }

    static async Task<IResult> AddOrUpdateGrade(
        Guid id, UpdateCourseGradeRequest req, ORUDbContext db)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));

        var existing = student.EnrolledCourses
            .FirstOrDefault(c => c.CourseCode == req.CourseCode && c.Semester == req.Semester);

        if (existing is not null)
        {
            existing.Grade = req.Grade;
            existing.CourseTitle = req.CourseTitle;
            existing.CreditUnits = req.CreditUnits;
        }
        else
        {
            student.EnrolledCourses.Add(new EnrolledCourse
            {
                CourseCode = req.CourseCode,
                CourseTitle = req.CourseTitle,
                Grade = req.Grade,
                Semester = req.Semester,
                CreditUnits = req.CreditUnits
            });
        }

        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToAcademics(student), "Grade updated successfully."));
    }

    static async Task<IResult> UpdateStatus(
        Guid id, UpdateAccountStatusRequest req, ORUDbContext db, EmailService email)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        student.Status = req.Status;
        await db.SaveChangesAsync();

        if (req.Status == AccountStatus.Suspended)
            email.SendFireAndForget(student.Email, student.FullName,
                "Account Suspended — ORU",
                EmailService.StudentSuspended(student.FullName, student.MatricNumber));
        else if (req.Status == AccountStatus.Active)
            email.SendFireAndForget(student.Email, student.FullName,
                "Account Reactivated — ORU",
                EmailService.StudentReactivated(student.FullName, student.MatricNumber));

        return Results.Ok(ApiResponse.Ok(new { student.MatricNumber, student.Status }, "Student status updated successfully."));
    }

    static async Task<IResult> UpdateTuition(
        Guid id, UpdateTuitionRequest req, ORUDbContext db)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));
        student.TotalTuitionDue = req.TotalTuitionDue;
        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok(StudentMapper.ToFinancials(student), "Tuition updated successfully."));
    }

    static async Task<IResult> SubmitInstallment(
        ClaimsPrincipal user,
        ORUDbContext db,
        BlobStorageService blob,
        IFormFileCollection files,
        [Microsoft.AspNetCore.Mvc.FromForm] int installmentNumber,
        [Microsoft.AspNetCore.Mvc.FromForm] decimal amount,
        [Microsoft.AspNetCore.Mvc.FromForm] string? notes)
    {
        var id = GetStudentId(user);
        if (id is null) return Results.Unauthorized();

        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));

        if (files.Count == 0)
            return Results.BadRequest(ApiResponse.Error("At least one screenshot is required."));

        if (files.Count > 3)
            return Results.BadRequest(ApiResponse.Error("Maximum 3 screenshots per installment."));

        if (amount <= 0)
            return Results.BadRequest(ApiResponse.Error("Amount must be greater than zero."));

        if (installmentNumber < 1 || installmentNumber > 4)
            return Results.BadRequest(ApiResponse.Error("Installment number must be between 1 and 4."));

        var submission = new InstallmentSubmission
        {
            InstallmentNumber = installmentNumber,
            Amount = amount,
            Notes = notes,
        };

        try
        {
            var uploadedUrls = new List<string>();
            foreach (var file in files)
            {
                var url = await blob.UploadAsync(file, $"installments/{student.Id}/{submission.Id}");
                submission.ScreenshotUrls.Add(url);
                uploadedUrls.Add(url);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }

        student.InstallmentSubmissions.Add(submission);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/students/me/installments/{submission.Id}",
            ApiResponse.Ok(new InstallmentSubmissionResponse(
                submission.Id, submission.InstallmentNumber, submission.Amount,
                submission.ScreenshotUrls, submission.Notes,
                submission.Status, submission.SubmittedAt,
                submission.ReviewedAt, submission.ReviewedByAdminId
            ), "Installment proof submitted successfully."));
    }

    static async Task<IResult> GetMyInstallments(ClaimsPrincipal user, ORUDbContext db)
    {
        var id = GetStudentId(user);
        if (id is null) return Results.Unauthorized();

        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));

        var submissions = student.InstallmentSubmissions
            .OrderByDescending(i => i.SubmittedAt)
            .Select(i => new InstallmentSubmissionResponse(
                i.Id, i.InstallmentNumber, i.Amount,
                i.ScreenshotUrls, i.Notes,
                i.Status, i.SubmittedAt,
                i.ReviewedAt, i.ReviewedByAdminId
            ))
            .ToList();

        return Results.Ok(ApiResponse.Ok(submissions));
    }

    static async Task<IResult> GetStudentInstallments(Guid id, ORUDbContext db)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));

        var submissions = student.InstallmentSubmissions
            .OrderByDescending(i => i.SubmittedAt)
            .Select(i => new InstallmentSubmissionResponse(
                i.Id, i.InstallmentNumber, i.Amount,
                i.ScreenshotUrls, i.Notes,
                i.Status, i.SubmittedAt,
                i.ReviewedAt, i.ReviewedByAdminId
            ))
            .ToList();

        return Results.Ok(ApiResponse.Ok(submissions));
    }

    static async Task<IResult> ReviewInstallment(
        Guid studentId,
        Guid submissionId,
        ReviewInstallmentRequest req,
        ClaimsPrincipal user,
        ORUDbContext db,
        EmailService email)
    {
        var student = await db.Students.FindAsync(studentId);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));

        var submission = student.InstallmentSubmissions
            .FirstOrDefault(i => i.Id == submissionId);

        if (submission is null)
            return Results.NotFound(ApiResponse.Error("Installment submission not found."));

        if (submission.Status != InstallmentStatus.Pending)
            return Results.BadRequest(ApiResponse.Error("This installment has already been reviewed."));

        var adminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        submission.Status = req.Status;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewedByAdminId = adminId;

        if (req.Status == InstallmentStatus.Approved)
        {
            student.TotalAmountPaid += submission.Amount;
            student.PaymentHistory.Add(new PaymentInstallment
            {
                Amount = submission.Amount,
                PaymentReference = $"INST-{submission.Id.ToString()[..8].ToUpper()}",
                Gateway = "installment",
                PaidAt = DateTime.UtcNow
            });
            email.SendFireAndForget(student.Email, student.FullName,
                "Installment Approved — ORU",
                EmailService.InstallmentApproved(student.FullName, submission.InstallmentNumber, submission.Amount));
        }
        else
        {
            email.SendFireAndForget(student.Email, student.FullName,
                "Installment Rejected — ORU",
                EmailService.InstallmentRejected(student.FullName, submission.InstallmentNumber, submission.Amount));
        }

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Ok(new InstallmentSubmissionResponse(
            submission.Id, submission.InstallmentNumber, submission.Amount,
            submission.ScreenshotUrls, submission.Notes,
            submission.Status, submission.SubmittedAt,
            submission.ReviewedAt, submission.ReviewedByAdminId
        ), $"Installment {req.Status.ToString().ToLower()} successfully."));
    }

    private static Guid? GetStudentId(ClaimsPrincipal user)
    {
        var val = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(val, out var id) ? id : null;
    }
}
