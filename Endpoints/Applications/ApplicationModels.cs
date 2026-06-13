using ORUApi.Models;

namespace ORUApi.Endpoints.Applications;

public record SubmitApplicationRequest(
    string FullName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    string Address,
    string SelectedProgram,
    int StudyLevelId
);

public record UpdateStatusRequest(ApplicationStatus Status);

public record ApplicationResponse(
    Guid Id,
    string FullName,
    string Email,
    string SelectedProgram,
    int StudyLevelId,
    string? StudyLevelName,
    ApplicationStatus Status,
    bool ApplicationFeePaid,
    List<string> DocumentUrls,
    DateTime SubmittedAt
);

public static class ApplicationMapper
{
    public static ApplicationResponse ToResponse(Application a) => new(
        a.Id, a.FullName, a.Email,
        a.SelectedProgram,
        a.StudyLevelId,
        a.StudyLevelRef?.Name,
        a.Status, a.ApplicationFeePaid,
        a.DocumentUrls, a.SubmittedAt
    );
}
