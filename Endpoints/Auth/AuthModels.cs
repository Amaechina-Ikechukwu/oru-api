using ORUApi.Models;

namespace ORUApi.Endpoints.Auth;
public record StudentLoginRequest(string MatricNumberOrEmail, string Password);
public record AdminLoginRequest(string Email, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ActivateStudentRequest(string Email, string MatricNumber, string NewPassword);

public record CreateAdminRequest(
    string StaffId, string FullName, string Email,
    string Password, AdminRole Role, AdminPermissions Permissions
);

public record AuthResponse(string Token, string Role, string Name, string Id);