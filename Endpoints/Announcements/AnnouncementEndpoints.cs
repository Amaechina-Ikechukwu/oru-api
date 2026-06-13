using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ORUApi.Data;
using ORUApi.Models;
using ORUApi.Services;

namespace ORUApi.Endpoints.Announcements;

public static class AnnouncementEndpoints
{
    public static void MapAnnouncementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/announcements").WithTags("Announcements");

        group.MapGet("/", GetAllAnnouncements)
            .WithSummary("Get published announcements (public)");

        group.MapGet("/{id}", GetAnnouncement)
            .WithSummary("Get a single announcement (public)");

        var admin = app.MapGroup("/api/admin/announcements").WithTags("Admin: Announcements");

        admin.MapPost("/", CreateAnnouncement)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Create an announcement");

        admin.MapPatch("/{id}", UpdateAnnouncement)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Edit an announcement");

        admin.MapDelete("/{id}", DeleteAnnouncement)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Delete an announcement");

        admin.MapPost("/{id}/images", UploadImages)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Upload images for an announcement")
            .DisableAntiforgery();

        admin.MapDelete("/{id}/images", DeleteImage)
            .RequireAuthorization("AdminOnly")
            .WithSummary("Remove an image from an announcement");
    }

    static async Task<IResult> GetAllAnnouncements(
        ORUDbContext db,
        string? category = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = db.Announcements.Where(a => a.IsPublished).AsQueryable();

        if (Enum.TryParse<AnnouncementCategory>(category, out var parsed))
            query = query.Where(a => a.Category == parsed);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => AnnouncementMapper.ToResponse(a))
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok(new { total, page, pageSize, items }));
    }

    static async Task<IResult> GetAnnouncement(Guid id, ORUDbContext db)
    {
        var a = await db.Announcements.FindAsync(id);
        if (a is null || !a.IsPublished) return Results.NotFound(ApiResponse.Error("Announcement not found."));
        return Results.Ok(ApiResponse.Ok(AnnouncementMapper.ToResponse(a)));
    }

    static async Task<IResult> CreateAnnouncement(
        CreateAnnouncementRequest req,
        ClaimsPrincipal user,
        ORUDbContext db)
    {
        var adminId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var announcement = new Announcement
        {
            Title = req.Title,
            Content = req.Content,
            Category = req.Category,
            PostedByAdminId = adminId,
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/announcements/{announcement.Id}",
            ApiResponse.Ok(AnnouncementMapper.ToResponse(announcement), "Announcement created successfully."));
    }

    static async Task<IResult> UpdateAnnouncement(
        Guid id, UpdateAnnouncementRequest req, ORUDbContext db)
    {
        var a = await db.Announcements.FindAsync(id);
        if (a is null) return Results.NotFound(ApiResponse.Error("Announcement not found."));

        if (req.Title is not null) a.Title = req.Title;
        if (req.Content is not null) a.Content = req.Content;
        if (req.Category.HasValue) a.Category = req.Category.Value;
        if (req.IsPublished.HasValue) a.IsPublished = req.IsPublished.Value;

        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok(AnnouncementMapper.ToResponse(a), "Announcement updated successfully."));
    }

    static async Task<IResult> DeleteAnnouncement(Guid id, ORUDbContext db)
    {
        var a = await db.Announcements.FindAsync(id);
        if (a is null) return Results.NotFound(ApiResponse.Error("Announcement not found."));
        db.Announcements.Remove(a);
        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse.Ok<object?>(null, "Announcement deleted successfully."));
    }

    static async Task<IResult> UploadImages(
        Guid id, IFormFileCollection files,
        ORUDbContext db, BlobStorageService blob)
    {
        var a = await db.Announcements.FindAsync(id);
        if (a is null) return Results.NotFound(ApiResponse.Error("Announcement not found."));

        try
        {
            var uploadedUrls = new List<string>();
            foreach (var file in files)
            {
                var url = await blob.UploadAsync(file, $"news-media/{id}");
                a.ImageUrls.Add(url);
                uploadedUrls.Add(url);
            }

            await db.SaveChangesAsync();
            return Results.Ok(ApiResponse.Ok(new { uploadedUrls }, "Images uploaded successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }

    static async Task<IResult> DeleteImage(
        Guid id, string imageUrl,
        ORUDbContext db, BlobStorageService blob)
    {
        var a = await db.Announcements.FindAsync(id);
        if (a is null) return Results.NotFound(ApiResponse.Error("Announcement not found."));
        if (!a.ImageUrls.Contains(imageUrl))
            return Results.BadRequest(ApiResponse.Error("Image URL not found on this announcement."));

        try
        {
            await blob.DeleteAsync(imageUrl);
            a.ImageUrls.Remove(imageUrl);
            await db.SaveChangesAsync();
            return Results.Ok(ApiResponse.Ok<object?>(null, "Image removed successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Error(ex.Message));
        }
    }
}
