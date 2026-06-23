using ORUApi.Models;

namespace ORUApi.Endpoints.Auth;
public record StudentLoginRequest(string MatricNumberOrEmail, string Password);
public record AdminLoginRequest(string Email, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ActivateStudentRequest(string Email, string MatricNumber, string NewPassword);

public record InviteAdminRequest(
    string Email, AdminRole Role, AdminPermissions Permissions, string FrontendSetupUrl
);

public record SetupAdminRequest(
    string Token, string FullName, string StaffId, string Password
);

public record AuthResponse(string Token, string Role, string Name, string Id);