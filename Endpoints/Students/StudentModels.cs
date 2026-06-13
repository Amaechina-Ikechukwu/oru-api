using ORUApi.Models;
namespace ORUApi.Endpoints.Students;
public record StudentProfileResponse(
    Guid Id, string MatricNumber, string FullName,
    string Email, string Program, int CurrentSemester,
    decimal GPA, AccountStatus Status
);

public record AcademicRecordResponse(
    string MatricNumber, string FullName, string Program,
    int CurrentSemester, decimal GPA, List<EnrolledCourse> EnrolledCourses
);

public record FinancialRecordResponse(
    string MatricNumber, string FullName,
    decimal TotalTuitionDue, decimal TotalAmountPaid,
    decimal OutstandingBalance, List<PaymentInstallment> PaymentHistory,
    List<InstallmentSubmissionResponse> InstallmentSubmissions
);

public record UpdateAcademicsRequest(int? CurrentSemester, decimal? GPA);

public record UpdateCourseGradeRequest(
    string CourseCode, string CourseTitle,
    string Grade, int Semester, int CreditUnits
);

public record UpdateAccountStatusRequest(AccountStatus Status);
public record UpdateTuitionRequest(decimal TotalTuitionDue);

public record SubmitInstallmentRequest(int InstallmentNumber, decimal Amount, string? Notes);
public record ReviewInstallmentRequest(InstallmentStatus Status);

public record InstallmentSubmissionResponse(
    Guid Id, int InstallmentNumber, decimal Amount,
    List<string> ScreenshotUrls, string? Notes,
    InstallmentStatus Status, DateTime SubmittedAt,
    DateTime? ReviewedAt, Guid? ReviewedByAdminId
);

public static class StudentMapper
{
    public static StudentProfileResponse ToProfile(Student s) => new(
        s.Id, s.MatricNumber, s.FullName,
        s.Email, s.Program, s.CurrentSemester, s.GPA, s.Status);

    public static AcademicRecordResponse ToAcademics(Student s) => new(
        s.MatricNumber, s.FullName, s.Program,
        s.CurrentSemester, s.GPA, s.EnrolledCourses);

    public static FinancialRecordResponse ToFinancials(Student s) => new(
        s.MatricNumber, s.FullName, s.TotalTuitionDue,
        s.TotalAmountPaid, s.OutstandingBalance, s.PaymentHistory,
        s.InstallmentSubmissions.Select(i => new InstallmentSubmissionResponse(
            i.Id, i.InstallmentNumber, i.Amount,
            i.ScreenshotUrls, i.Notes,
            i.Status, i.SubmittedAt,
            i.ReviewedAt, i.ReviewedByAdminId
        )).ToList()
    );
}