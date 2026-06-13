using Microsoft.EntityFrameworkCore;
using ORUApi.Data;
using ORUApi.Models;

namespace ORUApi.Endpoints.StudyLevels;

public static class StudyLevelEndpoints
{
    public static void MapStudyLevelEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/study-levels").WithTags("Admin: Study Levels");

        admin.MapGet("/", GetAllLevels)
            .RequireAuthorization("AdminOnly")
            .WithSummary("List all study levels");

        admin.MapPost("/", CreateLevel)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Create a study level");

        admin.MapPatch("/{id}", UpdateLevel)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Edit a study level");

        admin.MapDelete("/{id}", DeleteLevel)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Deactivate a study level");
    }

    static async Task<IResult> GetAllLevels(ORUDbContext db)
    {
        var levels = await db.StudyLevels
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(levels));
    }

    static async Task<IResult> CreateLevel(
        CreateStudyLevelRequest req, ORUDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(ApiResponse.Error("Name is required."));

        var level = new StudyLevel
        {
            Name = req.Name,
            Description = req.Description ?? req.Name,
            Duration = req.Duration ?? "",
            SortOrder = req.SortOrder,
        };

        db.StudyLevels.Add(level);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/study-levels/{level.Id}",
            ApiResponse.Ok(level, "Study level created successfully."));
    }

    static async Task<IResult> UpdateLevel(
        int id, UpdateStudyLevelRequest req, ORUDbContext db)
    {
        var level = await db.StudyLevels.FindAsync(id);
        if (level is null) return Results.NotFound(ApiResponse.Error("Study level not found."));

        if (req.Name is not null) level.Name = req.Name;
        if (req.Description is not null) level.Description = req.Description;
        if (req.Duration is not null) level.Duration = req.Duration;
        if (req.SortOrder.HasValue) level.SortOrder = req.SortOrder.Value;
        if (req.IsActive.HasValue) level.IsActive = req.IsActive.Value;

        level.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Ok(level, "Study level updated successfully."));
    }

    static async Task<IResult> DeleteLevel(int id, ORUDbContext db)
    {
        var level = await db.StudyLevels.FindAsync(id);
        if (level is null) return Results.NotFound(ApiResponse.Error("Study level not found."));

        level.IsActive = false;
        level.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Ok<object?>(null, "Study level deactivated successfully."));
    }
}

public record CreateStudyLevelRequest(
    string Name, string? Description, string? Duration, int SortOrder);

public record UpdateStudyLevelRequest(
    string? Name, string? Description, string? Duration, int? SortOrder, bool? IsActive);
