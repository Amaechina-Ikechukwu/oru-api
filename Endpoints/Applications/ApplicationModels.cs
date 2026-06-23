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

public record DocumentResponse(
    Guid Id,
    string Name,
    string FileUrl,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt
);

public record ApplicationResponse(
    Guid Id,
    string FullName,
    string Email,
    string SelectedProgram,
    int StudyLevelId,
    string? StudyLevelName,
    ApplicationStatus Status,
    bool ApplicationFeePaid,
    string? ApplicationFeeReceiptUrl,
    List<DocumentResponse> Documents,
    DateTime SubmittedAt
);

public static class ApplicationMapper
{
    public static ApplicationResponse ToResponse(Application a) => new(
        a.Id, a.FullName, a.Email,
        a.SelectedProgram,
        a.StudyLevelId,
        a.StudyLevelRef?.Name,
        a.Status, a.ApplicationFeePaid, a.ApplicationFeeReceiptUrl,
        a.Documents.Select(DocumentMapper.ToResponse).ToList(),
        a.SubmittedAt
    );
}

public static class DocumentMapper
{
    public static DocumentResponse ToResponse(ApplicationDocument d) => new(
        d.Id, d.Name, d.FileUrl, d.FileName,
        d.ContentType, d.FileSize, d.UploadedAt
    );
}
