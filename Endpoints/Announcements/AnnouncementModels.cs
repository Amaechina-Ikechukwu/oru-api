using ORUApi.Models;

namespace ORUApi.Endpoints.Announcements;
public record CreateAnnouncementRequest(
    string Title, string Content, AnnouncementCategory Category);

public record UpdateAnnouncementRequest(
    string? Title, string? Content,
    AnnouncementCategory? Category, bool? IsPublished);

public record AnnouncementResponse(
    Guid Id, string Title, string Content,
    AnnouncementCategory Category, List<string> ImageUrls,
    Guid PostedByAdminId, DateTime PublishedAt, bool IsPublished
);

public static class AnnouncementMapper
{
    public static AnnouncementResponse ToResponse(Announcement a) => new(
        a.Id, a.Title, a.Content, a.Category,
        a.ImageUrls, a.PostedByAdminId, a.PublishedAt, a.IsPublished);
}