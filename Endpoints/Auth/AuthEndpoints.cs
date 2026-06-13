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

        group.MapPost("/admin/login", AdminLogin)
            .AllowAnonymous()
            .WithSummary("Admin/staff login");

        group.MapPost("/student/change-password", ChangeStudentPassword)
            .RequireAuthorization("StudentOnly")
            .WithSummary("Student changes their password");

        var admin = app.MapGroup("/api/admin/admins").WithTags("Admin: Admins");

        admin.MapPost("/", CreateAdmin)
            .RequireAuthorization("SuperAdmin")
            .WithSummary("Create a new admin account");

        admin.MapGet("/", GetAllAdmins)
            .RequireAuthorization("SuperAdmin")
            .WithSummary("List all admin accounts");

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

        if (student.Status == AccountStatus.Suspended)
            return Results.Forbid();

        var token = tokens.CreateStudentToken(student);
        return Results.Ok(ApiResponse.Ok(
            new AuthResponse(token, "Student", student.FullName, student.Id.ToString()),
            "Login successful."));
    }

    static async Task<IResult> AdminLogin(
        AdminLoginRequest req, ORUDbContext db, TokenService tokens)
    {
        var admin = await db.Admins
            .FirstOrDefaultAsync(a => a.Email == req.Email && a.IsActive);

        if (admin is null || !BCrypt.Net.BCrypt.Verify(req.Password, admin.PasswordHash))
            return Results.Unauthorized();

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

    static async Task<IResult> CreateAdmin(CreateAdminRequest req, ORUDbContext db)
    {
        if (await db.Admins.AnyAsync(a => a.Email == req.Email))
            return Results.Conflict(ApiResponse.Error("An admin with this email already exists."));

        if (await db.Admins.AnyAsync(a => a.StaffId == req.StaffId))
            return Results.Conflict(ApiResponse.Error("An admin with this staff ID already exists."));

        var admin = new Admin
        {
            StaffId = req.StaffId,
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
            Permissions = req.Permissions,
        };

        db.Admins.Add(admin);
        await db.SaveChangesAsync();

        return Results.Created($"/api/admin/admins/{admin.Id}",
            ApiResponse.Ok(new
            {
                admin.Id, admin.StaffId, admin.FullName, admin.Email, admin.Role
            }, "Admin account created successfully."));
    }

    static async Task<IResult> GetAllAdmins(ORUDbContext db)
    {
        var admins = await db.Admins
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.StaffId, a.FullName, a.Email, a.Role, a.Permissions, a.CreatedAt })
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(admins));
    }

    static async Task<IResult> DeactivateAdmin(Guid id, ORUDbContext db)
    {
        var admin = await db.Admins.FindAsync(id);
        if (admin is null) return Results.NotFound(ApiResponse.Error("Admin not found."));
        admin.IsActive = false;
        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok<object?>(null, $"{admin.FullName} has been deactivated."));
    }
}
