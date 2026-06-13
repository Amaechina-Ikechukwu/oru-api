using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ORUApi.Data;
using ORUApi.Models;

namespace ORUApi.Endpoints.Courses;

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        group.MapGet("/", GetAllCourses)
            .WithSummary("List active courses (public)");

        group.MapGet("/{id}", GetCourse)
            .WithSummary("Get a single course (public)");

        var admin = app.MapGroup("/api/admin/courses").WithTags("Admin: Courses");

        admin.MapGet("/", AdminGetAllCourses)
            .RequireAuthorization("AdminOnly")
            .WithSummary("List all courses including deleted (admin)");

        admin.MapPost("/", CreateCourse)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Create a new course");

        admin.MapPatch("/{id}", UpdateCourse)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Edit a course");

        admin.MapDelete("/{id}", DeleteCourse)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Remove a course (soft delete)");
    }

    static async Task<IResult> GetAllCourses(
        ORUDbContext db,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.Courses.Where(c => !c.IsDeleted && c.IsActive);
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => CourseMapper.ToResponse(c))
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(new { total, page, pageSize, items }));
    }

    static async Task<IResult> GetCourse(Guid id, ORUDbContext db)
    {
        var c = await db.Courses.FindAsync(id);
        if (c is null || c.IsDeleted || !c.IsActive) return Results.NotFound(ApiResponse.Error("Course not found."));
        return Results.Ok(ApiResponse.Ok(CourseMapper.ToResponse(c)));
    }

    static async Task<IResult> AdminGetAllCourses(
        ORUDbContext db,
        bool? includeDeleted = false,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.Courses.AsQueryable();
        if (includeDeleted != true)
            query = query.Where(c => !c.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => CourseMapper.ToResponse(c))
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(new { total, page, pageSize, items }));
    }

    static async Task<IResult> CreateCourse(
        CreateCourseRequest req,
        ClaimsPrincipal user,
        ORUDbContext db)
    {
        var adminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        if (await db.Courses.AnyAsync(c => c.Code == req.Code && !c.IsDeleted))
            return Results.Conflict(ApiResponse.Error("A course with this code already exists."));

        var course = new Course
        {
            Code = req.Code,
            Title = req.Title,
            Description = req.Description,
            CreditUnits = req.CreditUnits,
            CreatedByAdminId = adminId,
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/courses/{course.Id}",
            ApiResponse.Ok(CourseMapper.ToResponse(course), "Course created successfully."));
    }

    static async Task<IResult> UpdateCourse(
        Guid id,
        UpdateCourseRequest req,
        ClaimsPrincipal user,
        ORUDbContext db)
    {
        var c = await db.Courses.FindAsync(id);
        if (c is null || c.IsDeleted) return Results.NotFound(ApiResponse.Error("Course not found."));

        var adminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        if (req.Code is not null)
        {
            if (await db.Courses.AnyAsync(x => x.Code == req.Code && x.Id != id && !x.IsDeleted))
                return Results.Conflict(ApiResponse.Error("A course with this code already exists."));
            c.Code = req.Code;
        }
        if (req.Title is not null) c.Title = req.Title;
        if (req.Description is not null) c.Description = req.Description;
        if (req.CreditUnits.HasValue) c.CreditUnits = req.CreditUnits.Value;
        if (req.IsActive.HasValue) c.IsActive = req.IsActive.Value;

        c.UpdatedByAdminId = adminId;
        c.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok(CourseMapper.ToResponse(c), "Course updated successfully."));
    }

    static async Task<IResult> DeleteCourse(
        Guid id,
        ClaimsPrincipal user,
        ORUDbContext db)
    {
        var c = await db.Courses.FindAsync(id);
        if (c is null || c.IsDeleted) return Results.NotFound(ApiResponse.Error("Course not found."));

        var adminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        c.IsDeleted = true;
        c.DeletedByAdminId = adminId;
        c.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok<object?>(null, "Course removed successfully."));
    }
}
