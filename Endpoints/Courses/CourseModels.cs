using ORUApi.Models;

namespace ORUApi.Endpoints.Courses;

public record CreateCourseRequest(
    string Code, string Title, string? Description, int CreditUnits);

public record UpdateCourseRequest(
    string? Code, string? Title, string? Description, int? CreditUnits, bool? IsActive);

public record CourseResponse(
    Guid Id, string Code, string Title, string? Description,
    int CreditUnits, bool IsActive,
    Guid CreatedByAdminId, Guid? UpdatedByAdminId,
    DateTime CreatedAt, DateTime? UpdatedAt);

public static class CourseMapper
{
    public static CourseResponse ToResponse(Course c) => new(
        c.Id, c.Code, c.Title, c.Description,
        c.CreditUnits, c.IsActive,
        c.CreatedByAdminId, c.UpdatedByAdminId,
        c.CreatedAt, c.UpdatedAt);
}
