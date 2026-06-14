using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ORUApi.Models
{
  // Models/Student.cs
public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MatricNumber { get; set; } = "";  // e.g. "CSC/2024/001"
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";  // BCrypt hashed
    public string Program { get; set; } = "";

    // Academic
    public int CurrentSemester { get; set; }
    public decimal GPA { get; set; }
    public List<EnrolledCourse> EnrolledCourses { get; set; } = [];

    // Finance
    public decimal TotalTuitionDue { get; set; }
    public decimal TotalAmountPaid { get; set; }
    public decimal OutstandingBalance => TotalTuitionDue - TotalAmountPaid;
    public List<PaymentInstallment> PaymentHistory { get; set; } = [];
    public List<InstallmentSubmission> InstallmentSubmissions { get; set; } = [];

    public AccountStatus Status { get; set; } = AccountStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EnrolledCourse
{
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string? Grade { get; set; }
    public int CreditUnits { get; set; }
    public int Semester { get; set; }
}

public class PaymentInstallment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; }
    public string PaymentReference { get; set; } = "";
    public string Gateway { get; set; } = "";  // "paystack", "monnify", etc.
    public DateTime PaidAt { get; set; }
}

public enum AccountStatus { Pending = 3, Active = 0, Suspended = 1, Graduated = 2 }

public class InstallmentSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public List<string> ScreenshotUrls { get; set; } = [];
    public string? Notes { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
}

public enum InstallmentStatus { Pending, Approved, Rejected }
}