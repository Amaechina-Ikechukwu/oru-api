using Microsoft.EntityFrameworkCore;
using ORUApi.Data;
using ORUApi.Models;
using ORUApi.Services;

namespace ORUApi.Endpoints.Applications;

public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/applications").WithTags("Applications");

        group.MapPost("/", SubmitApplication)
            .WithSummary("Submit a new admission application");

        group.MapGet("/study-levels", GetStudyLevels)
            .WithSummary("Get available study levels");

        group.MapGet("/status/{email}", GetStatusByEmail)
            .WithSummary("Check application status by email (public)");

        group.MapPost("/{id}/documents", UploadDocuments)
            .WithSummary("Upload supporting documents")
            .DisableAntiforgery();

        var admin = app.MapGroup("/api/admin/applications").WithTags("Admin: Applications");

        admin.MapGet("/", GetAllApplications)
            .RequireAuthorization("Admissions")
            .WithSummary("List all applications");

        admin.MapGet("/{id}", GetApplication)
            .RequireAuthorization("Admissions")
            .WithSummary("Get a single application");

        admin.MapPatch("/{id}/status", UpdateStatus)
            .RequireAuthorization("Admissions")
            .WithSummary("Update review status");

        admin.MapPost("/{id}/admit", AdmitStudent)
            .RequireAuthorization("Admissions")
            .WithSummary("Admit applicant and create student account");
    }

    static async Task<IResult> SubmitApplication(
        SubmitApplicationRequest req, ORUDbContext db, EmailService email)
    {
        if (string.IsNullOrWhiteSpace(req.FullName))
            return Results.BadRequest(ApiResponse.Error("Full name is required."));

        if (string.IsNullOrWhiteSpace(req.Email))
            return Results.BadRequest(ApiResponse.Error("Email is required."));

        if (string.IsNullOrWhiteSpace(req.SelectedProgram))
            return Results.BadRequest(ApiResponse.Error("Please select a program."));

        if (req.StudyLevelId <= 0)
            return Results.BadRequest(ApiResponse.Error("Please select a study level."));

        var level = await db.StudyLevels.FindAsync(req.StudyLevelId);
        if (level is null || !level.IsActive)
            return Results.BadRequest(ApiResponse.Error("Invalid study level selected."));

        var application = new Application
        {
            FullName = req.FullName,
            Email = req.Email,
            Phone = req.Phone,
            DateOfBirth = req.DateOfBirth,
            Address = req.Address,
            SelectedProgram = req.SelectedProgram,
            StudyLevelId = req.StudyLevelId,
        };

        db.Applications.Add(application);
        await db.SaveChangesAsync();

        application.StudyLevelRef = level;

        email.SendFireAndForget(req.Email, req.FullName,
            "Application Received — ORU",
            EmailService.ApplicationSubmitted(req.FullName, req.SelectedProgram));

        return Results.Created(
            $"/api/applications/{application.Id}",
            ApiResponse.Ok(ApplicationMapper.ToResponse(application), "Application submitted successfully."));
    }

    static async Task<IResult> GetStudyLevels(ORUDbContext db)
    {
        var levels = await db.StudyLevels
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .Select(l => new
            {
                value = l.Id,
                label = l.Description ?? l.Name
            })
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(levels));
    }

    static async Task<IResult> GetStatusByEmail(string email, ORUDbContext db)
    {
        var application = await db.Applications
            .Where(a => a.Email == email)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync();

        if (application is null)
            return Results.NotFound(ApiResponse.Error("No application found for this email."));

        return Results.Ok(ApiResponse.Ok(new
        {
            application.Status,
            application.ApplicationFeePaid,
            application.SubmittedAt
        }));
    }

    static async Task<IResult> GetAllApplications(
        ORUDbContext db,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.Applications.AsQueryable();

        if (Enum.TryParse<ApplicationStatus>(status, out var parsed))
            query = query.Where(a => a.Status == parsed);
        else
            query = query.Where(a => a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.UnderReview);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(a => a.StudyLevelRef)
            .Select(a => ApplicationMapper.ToResponse(a))
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(new { total, page, pageSize, items }));
    }

    static async Task<IResult> GetApplication(Guid id, ORUDbContext db)
    {
        var application = await db.Applications
            .Include(a => a.StudyLevelRef)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null) return Results.NotFound(ApiResponse.Error("Application not found."));
        return Results.Ok(ApiResponse.Ok(ApplicationMapper.ToResponse(application)));
    }

    static async Task<IResult> UpdateStatus(
        Guid id, UpdateStatusRequest req, ORUDbContext db, EmailService email)
    {
        var application = await db.Applications
            .Include(a => a.StudyLevelRef)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null) return Results.NotFound(ApiResponse.Error("Application not found."));
        application.Status = req.Status;
        await db.SaveChangesAsync();

        if (req.Status == ApplicationStatus.Approved)
            email.SendFireAndForget(application.Email, application.FullName,
                "Application Approved — ORU",
                EmailService.ApplicationApproved(application.FullName, application.SelectedProgram));
        else if (req.Status == ApplicationStatus.Rejected)
            email.SendFireAndForget(application.Email, application.FullName,
                "Application Update — ORU",
                EmailService.ApplicationRejected(application.FullName, application.SelectedProgram));

        return Results.Ok(ApiResponse.Ok(ApplicationMapper.ToResponse(application), "Application status updated successfully."));
    }

    static async Task<IResult> AdmitStudent(Guid id, ORUDbContext db, EmailService email)
    {
        var application = await db.Applications.FindAsync(id);
        if (application is null) return Results.NotFound(ApiResponse.Error("Application not found."));

        if (application.Status != ApplicationStatus.Approved)
            return Results.BadRequest(ApiResponse.Error("Application must be Approved before admission."));

        if (await db.Students.AnyAsync(s => s.Email == application.Email))
            return Results.Conflict(ApiResponse.Error("A student account already exists for this email."));

        var matric = await GenerateMatricNumber(application.SelectedProgram, db);

        var student = new Student
        {
            MatricNumber = matric,
            FullName = application.FullName,
            Email = application.Email,
            Program = application.SelectedProgram,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(matric),
        };

        db.Students.Add(student);
        await db.SaveChangesAsync();

        email.SendFireAndForget(application.Email, application.FullName,
            "Welcome to ORU!",
            EmailService.StudentAdmitted(application.FullName, matric));

        return Results.Ok(ApiResponse.Ok(new
        {
            student.MatricNumber,
            student.FullName,
            DefaultPassword = matric
        }, "Student admitted successfully. Default password is their matric number."));
    }

    static async Task<IResult> UploadDocuments(
        Guid id, IFormFileCollection files,
        ORUDbContext db, BlobStorageService blob)
    {
        var application = await db.Applications.FindAsync(id);
        if (application is null) return Results.NotFound(ApiResponse.Error("Application not found."));
        if (files.Count == 0) return Results.BadRequest(ApiResponse.Error("No files provided."));

        try
        {
            var uploadedUrls = new List<string>();
            foreach (var file in files)
            {
                var url = await blob.UploadAsync(file, $"admissions-docs/{id}");
                application.DocumentUrls.Add(url);
                uploadedUrls.Add(url);
            }

            await db.SaveChangesAsync();
            return Results.Ok(ApiResponse.Ok(new { uploadedUrls, total = application.DocumentUrls.Count }, "Documents uploaded successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    private static async Task<string> GenerateMatricNumber(
        string program, ORUDbContext db)
    {
        var prefix = (program.Length >= 3 ? program[..3] : program).ToUpper();
        var year = DateTime.UtcNow.Year;
        var count = await db.Students.CountAsync(s => s.Program == program) + 1;
        return $"{prefix}/{year}/{count:D3}";
    }
}
