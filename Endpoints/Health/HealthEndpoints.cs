using ORUApi.Models;
using ORUApi.Services;

namespace ORUApi.Endpoints.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/health").WithTags("Health");
        
        group.MapGet("/basic", () =>
            Results.Ok(ApiResponse.Ok(new { Status = "Ok", Message = "Basic System Ok" })));

        group.MapGet("/diagnose", (IConfiguration config) =>
        {
            var rawToken = config["ZeptoMail:Token"];
            var fromAddress = config["ZeptoMail:FromEmail"] ?? "noreply@oru.edu.ng";
            var fromName = config["ZeptoMail:FromName"] ?? "ORU PH Admissions";
            var configuredApiUrl = config["ZeptoMail:ApiUrl"];
            var apiUrl = string.IsNullOrWhiteSpace(configuredApiUrl) 
                ? "https://api.zeptomail.com/v1.1/email" 
                : configuredApiUrl.Trim();

            var providers = new List<string>();
            if (config is IConfigurationRoot root)
            {
                providers.AddRange(root.Providers.Select(p => p.GetType().Name));
            }

            var diagnosis = new
            {
                IsTokenConfigured = !string.IsNullOrEmpty(rawToken),
                TokenLength = rawToken?.Length ?? 0,
                FromEmail = fromAddress,
                FromName = fromName,
                ApiUrl = apiUrl,
                ActiveConfigurationProviders = providers,
                KeyVaultName = config["KeyVault:Name"]
            };

            return Results.Ok(ApiResponse.Ok(diagnosis, "Configuration diagnostics retrieved successfully."));
        });

        group.MapGet("/test-email", async (string toEmail, string? toName, EmailService email) =>
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return Results.BadRequest(ApiResponse.Error("Recipient 'toEmail' query parameter is required."));
            }

            var name = string.IsNullOrWhiteSpace(toName) ? "Test User" : toName;
            var subject = "Diagnostic Test Email — ORU";
            var body = EmailService.ApplicationSubmitted(name, "Test Program (B.Sc. Computer Science)");

            var result = await email.SendAsync(toEmail, name, subject, body);
            if (result.Success)
            {
                return Results.Ok(ApiResponse.Ok(result, "Test email sent successfully."));
            }
            else
            {
                return Results.BadRequest(new ApiResponse<EmailService.EmailSendResult>(
                    false, 
                    $"Failed to send email: {result.Message}", 
                    result
                ));
            }
        });
    }
}
