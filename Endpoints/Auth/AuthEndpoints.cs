using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ORUApi.Data;
using ORUApi.Models;
using ORUApi.Services;

namespace ORUApi.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/student/login", StudentLogin)
            .AllowAnonymous()
            .WithSummary("Student login");

        group.MapPost("/student/activate", ActivateStudent)
            .AllowAnonymous()
            .WithSummary("Activate student account (first-time setup)");

        group.MapPost("/admin/login", AdminLogin)
            .AllowAnonymous()
            .WithSummary("Admin/staff login");

        group.MapPost("/student/change-password", ChangeStudentPassword)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Student changes their password");

        var admin = app.MapGroup("/api/admin/admins").WithTags("Admin: Admins");

        admin.MapPost("/invite", InviteAdmin)
            .RequireAuthorization("SuperAdmin")
            .WithSummary("Invite a new admin account");

        admin.MapPost("/setup", SetupAdmin)
            .AllowAnonymous()
            .WithSummary("Setup an invited admin account");

        admin.MapGet("/", GetAllAdmins)
            .RequireAuthorization("SuperAdmin")
            .WithSummary("List all admin accounts");

        admin.MapGet("/logs", GetActivityLogs)
            .RequireAuthorization("SuperAdmin")
            .WithSummary("View admin activity logs");

        admin.MapDelete("/{id}", DeactivateAdmin)
            .RequireAuthorization("SuperAdmin")
            .WithSummary("Deactivate an admin account");
    }

    static async Task<IResult> StudentLogin(
        StudentLoginRequest req, ORUDbContext db, TokenService tokens)
    {
        var student = await db.Students.FirstOrDefaultAsync(s =>
            s.Email == req.MatricNumberOrEmail ||
            s.MatricNumber == req.MatricNumberOrEmail);

        if (student is null || !BCrypt.Net.BCrypt.Verify(req.Password, student.PasswordHash))
            return Results.Unauthorized();

        if (student.Status == AccountStatus.Suspended || student.Status == AccountStatus.Pending)
            return Results.Forbid();

        var token = tokens.CreateStudentToken(student);
        return Results.Ok(ApiResponse.Ok(
            new AuthResponse(token, "Student", student.FullName, student.Id.ToString()),
            "Login successful."));
    }

    static async Task<IResult> ActivateStudent(
        ActivateStudentRequest req, ORUDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return Results.BadRequest(ApiResponse.Error("Email is required."));

        if (string.IsNullOrWhiteSpace(req.MatricNumber))
            return Results.BadRequest(ApiResponse.Error("Matric number is required."));

        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return Results.BadRequest(ApiResponse.Error("Password must be at least 6 characters."));

        var student = await db.Students.FirstOrDefaultAsync(s =>
            s.Email == req.Email && s.MatricNumber == req.MatricNumber);

        if (student is null)
            return Results.NotFound(ApiResponse.Error("No matching student record found."));

        if (student.Status != AccountStatus.Pending)
            return Results.BadRequest(ApiResponse.Error("Account is already activated."));

        student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        student.Status = AccountStatus.Active;
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Ok(new
        {
            student.MatricNumber,
            student.FullName,
            student.Email
        }, "Account activated successfully. You may now log in."));
    }

    static async Task<IResult> AdminLogin(
        AdminLoginRequest req, ORUDbContext db, TokenService tokens, AdminActivityLogger logger)
    {
        var admin = await db.Admins
            .FirstOrDefaultAsync(a => a.Email == req.Email && a.IsActive && a.Status == AdminStatus.Active);

        if (admin is null || !BCrypt.Net.BCrypt.Verify(req.Password, admin.PasswordHash))
            return Results.Unauthorized();

        await logger.LogAsync(admin.Id, "Login", "Admin logged in successfully.");

        var token = tokens.CreateAdminToken(admin);
        return Results.Ok(ApiResponse.Ok(
            new AuthResponse(token, admin.Role.ToString(), admin.FullName, admin.Id.ToString()),
            "Login successful."));
    }

    static async Task<IResult> ChangeStudentPassword(
        ChangePasswordRequest req, ClaimsPrincipal user, ORUDbContext db)
    {
        var id = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var student = await db.Students.FindAsync(id);
        if (student is null) return Results.NotFound(ApiResponse.Error("Student not found."));

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, student.PasswordHash))
            return Results.BadRequest(ApiResponse.Error("Current password is incorrect."));

        student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok<object?>(null, "Password updated successfully."));
    }

    static async Task<IResult> InviteAdmin(InviteAdminRequest req, ORUDbContext db, EmailService email, ClaimsPrincipal user, AdminActivityLogger logger)
    {
        if (await db.Admins.AnyAsync(a => a.Email == req.Email))
            return Results.Conflict(ApiResponse.Error("An admin with this email already exists."));

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var admin = new Admin
        {
            Email = req.Email,
            Role = req.Role,
            Permissions = req.Permissions,
            Status = AdminStatus.Pending,
            VerificationToken = token,
            TokenExpiresAt = DateTime.UtcNow.AddDays(2)
        };

        db.Admins.Add(admin);
        await db.SaveChangesAsync();

        var setupUrl = $"{req.FrontendSetupUrl}?token={Uri.EscapeDataString(token)}";

        email.SendFireAndForget(req.Email, "Admin",
            "Admin Invitation — ORU",
            EmailService.AdminInvitation(req.Email, req.Role.ToString(), setupUrl));

        var currentAdminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await logger.LogAsync(currentAdminId, "Admin Invited", $"Invited {req.Email} as {req.Role}");

        return Results.Ok(ApiResponse.Ok<object?>(null, $"Invitation sent successfully to {req.Email}"));
    }

    static async Task<IResult> SetupAdmin(SetupAdminRequest req, ORUDbContext db, AdminActivityLogger logger)
    {
        var admin = await db.Admins.FirstOrDefaultAsync(a => a.VerificationToken == req.Token);
        if (admin is null)
            return Results.BadRequest(ApiResponse.Error("Invalid or expired verification token."));

        if (admin.Status != AdminStatus.Pending)
            return Results.BadRequest(ApiResponse.Error("This account has already been set up."));

        if (admin.TokenExpiresAt < DateTime.UtcNow)
            return Results.BadRequest(ApiResponse.Error("Verification token has expired. Please request a new invitation."));

        var count = await db.Admins.CountAsync() + 1;
        var baseId = $"ORU-STAFF-{count:D3}";
        while (await db.Admins.AnyAsync(a => a.StaffId == baseId))
        {
            baseId = $"ORU-STAFF-{new Random().Next(1000, 9999)}";
        }

        admin.FullName = req.FullName;
        admin.StaffId = baseId;
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        admin.Status = AdminStatus.Active;
        admin.VerificationToken = null;
        admin.TokenExpiresAt = null;

        await db.SaveChangesAsync();
        await logger.LogAsync(admin.Id, "Account Setup", "Admin verified token and set up account.");

        return Results.Ok(ApiResponse.Ok(new
        {
            admin.Id, admin.StaffId, admin.FullName, admin.Email, admin.Role
        }, "Admin account verified and setup successfully. You may now log in."));
    }

    static async Task<IResult> GetAllAdmins(ORUDbContext db)
    {
        var admins = await db.Admins
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.StaffId, a.FullName, a.Email, a.Role, a.Permissions, a.CreatedAt })
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(admins));
    }

    static async Task<IResult> DeactivateAdmin(Guid id, ORUDbContext db, ClaimsPrincipal user, AdminActivityLogger logger)
    {
        var admin = await db.Admins.FindAsync(id);
        if (admin is null) return Results.NotFound(ApiResponse.Error("Admin not found."));
        admin.IsActive = false;
        await db.SaveChangesAsync();

        var currentAdminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await logger.LogAsync(currentAdminId, "Deactivated Admin", $"Deactivated admin {admin.Email}");

        return Results.Ok(ApiResponse.Ok<object?>(null, $"{admin.FullName} has been deactivated."));
    }

    static async Task<IResult> GetActivityLogs(ORUDbContext db, Guid? adminId, string? action)
    {
        var query = db.AdminActivityLogs.Include(l => l.Admin).AsQueryable();
        
        if (adminId.HasValue)
            query = query.Where(l => l.AdminId == adminId);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        var logs = await query.OrderByDescending(l => l.Timestamp)
            .Take(100)
            .Select(l => new {
                l.Id,
                l.AdminId,
                AdminName = l.Admin != null ? l.Admin.FullName : "Unknown",
                l.Action,
                l.Details,
                l.Timestamp
            }).ToListAsync();

        return Results.Ok(ApiResponse.Ok(logs));
    }
}
