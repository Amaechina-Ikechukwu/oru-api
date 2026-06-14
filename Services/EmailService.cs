using System.Net.Http.Json;

namespace ORUApi.Services;

public class EmailService
{
    private readonly HttpClient _http = new();
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var token = _config["ZeptoMail:Token"];
        var fromAddress = _config["ZeptoMail:FromEmail"] ?? "noreply@oru.edu.ng";
        var fromName = _config["ZeptoMail:FromName"] ?? "ORU PH Admissions";

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("ZeptoMail token not configured. Skipping email to {Email}", toEmail);
            return;
        }

        var payload = new
        {
            from = new { address = fromAddress, name = fromName },
            to = new[] { new { email_address = new { address = toEmail, name = toName } } },
            subject,
            htmlbody = htmlBody
        };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.zeptomail.com/v1.1/email")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("Authorization", token);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("ZeptoMail failed ({Status}) for {Email}: {Body}", (int)response.StatusCode, toEmail, body);
            }
            else
            {
                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZeptoMail send failed for {Email}", toEmail);
        }
    }

    public void SendFireAndForget(string toEmail, string toName, string subject, string htmlBody)
    {
        _ = Task.Run(() => SendAsync(toEmail, toName, subject, htmlBody));
    }

    // ---- Layout helpers ----

    private static string Layout(string preheader, string title, string bodyInner)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
            <title>{title}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f9fafb;">
            <div style="display:none;max-height:0;overflow:hidden;mso-hide:all;font-size:1px;color:#f9fafb;line-height:1px;">{preheader}&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;&#x200C;</div>
            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="background-color:#f9fafb;">
            <tr><td align="center" style="padding:30px 20px;">
            <table width="600" cellpadding="0" cellspacing="0" role="presentation" style="background-color:#ffffff;border-radius:8px;overflow:hidden;">
            <tr><td style="padding:28px 30px 22px 30px;text-align:center;border-bottom:3px solid #be123c;">
            <span style="font-family:Georgia,serif;font-size:22px;font-weight:700;color:#16233c;letter-spacing:1px;">ORU</span>
            </td></tr>
            <tr><td style="padding:30px;font-family:Helvetica,Arial,sans-serif;color:#333333;font-size:15px;line-height:1.6;">
            {bodyInner}
            </td></tr>
            <tr><td style="padding:22px 30px;background-color:#16233c;color:#94a3b8;font-family:Helvetica,Arial,sans-serif;font-size:12px;line-height:1.8;text-align:center;">
            ORU Study Centre<br>This is an automated message. Please do not reply to this email.
            </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    private static string Callout(string label, string value)
    {
        return $"""
            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="background-color:#fff1f2;border-left:4px solid #be123c;margin:22px 0;">
            <tr><td style="padding:14px 20px;">
            <span style="font-family:Helvetica,Arial,sans-serif;font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px;color:#be123c;">{label}</span><br>
            <span style="font-family:Helvetica,Arial,sans-serif;font-size:16px;font-weight:700;color:#16233c;">{value}</span>
            </td></tr>
            </table>
            """;
    }

    private static string Button(string text, string url)
    {
        return $"""
            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:26px 0;">
            <tr><td align="center">
            <a href="{url}" style="display:inline-block;background-color:#be123c;color:#ffffff;text-decoration:none;font-weight:700;font-family:Helvetica,Arial,sans-serif;font-size:15px;padding:14px 30px;border-radius:4px;">{text}</a>
            </td></tr>
            </table>
            """;
    }

    // ---- Template helpers ----

    public static string ApplicationSubmitted(string name, string program)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your application for <strong style="color:#16233c;">{program}</strong> has been received and is under review.</p>
            <p style="margin:0;">We will notify you once a decision has been made.</p>
            """
            + Callout("APPLICATION STATUS", "Under Review")
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Admissions</p>
            """;
        return Layout(
            "Thank you for your application. We will review it and get back to you shortly.",
            "Application Received",
            body);
    }

    public static string ApplicationApproved(string name, string program)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Congratulations! Your application for <strong style="color:#16233c;">{program}</strong> has been approved.</p>
            <p style="margin:0;">You will receive your admission details shortly.</p>
            """
            + Callout("APPLICATION STATUS", "Approved")
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Admissions</p>
            """;
        return Layout(
            "Congratulations! Your application has been approved.",
            "Application Approved",
            body);
    }

    public static string ApplicationRejected(string name, string program)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">We regret to inform you that your application for <strong style="color:#16233c;">{program}</strong> was not successful at this time.</p>
            <p style="margin:0;">You may reapply in the next admission cycle.</p>
            """
            + Callout("APPLICATION STATUS", "Not Successful")
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Admissions</p>
            """;
        return Layout(
            "An update regarding your application status.",
            "Application Update",
            body);
    }

    public static string StudentAdmitted(string name, string matricNumber)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your admission has been confirmed. Welcome to ORU!</p>
            <p style="margin:0;">Please visit the student portal to activate your account and set your password.</p>
            """
            + Callout("MATRIC NUMBER", matricNumber)
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Admissions</p>
            """;
        return Layout(
            "Welcome to ORU! Your admission has been confirmed.",
            "Welcome to ORU",
            body);
    }

    public static string PaymentReceived(string name, decimal amount, string reference)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your payment has been received and processed successfully.</p>
            """
            + Callout("AMOUNT PAID", $"₦{amount:N2}")
            + Callout("REFERENCE", reference)
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Bursary</p>
            """;
        return Layout(
            $"Your payment of ₦{amount:N2} has been received.",
            "Payment Received",
            body);
    }

    public static string InstallmentApproved(string name, int installmentNumber, decimal amount)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your installment #{installmentNumber} of <strong style="color:#16233c;">₦{amount:N2}</strong> has been approved.</p>
            <p style="margin:0;">The amount has been credited to your account.</p>
            """
            + Callout("INSTALLMENT", $"#{installmentNumber} — ₦{amount:N2}")
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Bursary</p>
            """;
        return Layout(
            "Your installment payment has been approved.",
            "Installment Approved",
            body);
    }

    public static string InstallmentRejected(string name, int installmentNumber, decimal amount)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your installment #{installmentNumber} of <strong style="color:#16233c;">₦{amount:N2}</strong> was rejected.</p>
            <p style="margin:0;">Please resubmit with correct proof of payment.</p>
            """
            + Callout("INSTALLMENT", $"#{installmentNumber} — ₦{amount:N2}")
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Bursary</p>
            """;
        return Layout(
            "Your installment payment requires attention.",
            "Installment Rejected",
            body);
    }

    public static string AdminCreated(string name, string staffId, string email, string role, string password)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">An admin account has been created for you on the ORU platform.</p>
            <p style="margin:0 0 12px 0;">Please log in and change your password immediately.</p>
            """
            + Callout("STAFF ID", staffId)
            + Callout("EMAIL", email)
            + Callout("ROLE", role)
            + Callout("TEMPORARY PASSWORD", password)
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Administration</p>
            """;
        return Layout(
            "Your admin account has been created on the ORU platform.",
            "Admin Account Created",
            body);
    }

    public static string StudentSuspended(string name, string matricNumber)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your account has been temporarily suspended.</p>
            <p style="margin:0;">Please contact the administration for further details.</p>
            """
            + Callout("MATRIC NUMBER", matricNumber)
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Administration</p>
            """;
        return Layout(
            "Your account status has been updated.",
            "Account Suspended",
            body);
    }

    public static string StudentReactivated(string name, string matricNumber)
    {
        var body = $"""
            <p style="font-family:Georgia,serif;font-size:18px;color:#16233c;margin:0 0 20px 0;">Dear {name},</p>
            <p style="margin:0 0 12px 0;">Your account has been reactivated.</p>
            <p style="margin:0;">You may now log in and resume your activities.</p>
            """
            + Callout("MATRIC NUMBER", matricNumber)
            + """
            <p style="margin:28px 0 0 0;color:#94a3b8;font-size:13px;">— ORU Administration</p>
            """;
        return Layout(
            "Your account has been reactivated.",
            "Account Reactivated",
            body);
    }
}
