using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ORUApi.Models
{
    // Models/Admin.cs
public class Admin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StaffId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public AdminRole Role { get; set; }
    public AdminPermissions Permissions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public AdminStatus Status { get; set; } = AdminStatus.Pending;
    public string? VerificationToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AdminPermissions
{
    public bool CanApproveApplications { get; set; }
    public bool CanPostAnnouncements { get; set; }
    public bool CanUpdateGrades { get; set; }
    public bool CanManageFinance { get; set; }
    public bool CanManageAdmins { get; set; }
}

public enum AdminRole { SuperAdmin, AdmissionsOfficer, Bursar, AcademicAdvisor }
public enum AdminStatus { Pending, Active, Suspended }
}