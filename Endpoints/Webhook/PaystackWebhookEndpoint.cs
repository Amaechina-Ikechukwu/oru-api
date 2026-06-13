using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ORUApi.Models;
using ORUApi.Services;

namespace ORUApi.Endpoints.Webhook;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks").WithTags("Webhooks");

        group.MapPost("/paystack", HandlePaystack)
            .AllowAnonymous().WithSummary("Paystack payment webhook");

        group.MapPost("/monnify", HandleMonnify)
            .AllowAnonymous().WithSummary("Monnify payment webhook");

        group.MapPost("/flutterwave", HandleFlutterwave)
            .AllowAnonymous().WithSummary("Flutterwave payment webhook");
    }

    static async Task<IResult> HandlePaystack(
        HttpRequest request, PaymentService payments, ILogger logger)
    {
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var signature = request.Headers["x-paystack-signature"].ToString();

        if (!payments.VerifyPaystackSignature(body, signature))
        {
            logger.LogWarning("Paystack webhook: invalid signature");
            return Results.Unauthorized();
        }

        var payload = JsonSerializer.Deserialize<PaystackEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload?.Event != "charge.success")
            return Results.Ok(ApiResponse.Ok<object?>(null, "Webhook received — non-success event ignored."));

        await payments.RecordPaymentAsync(
            payload.Data.Customer.Email, payload.Data.Amount,
            payload.Data.Reference, "paystack");

        return Results.Ok(ApiResponse.Ok<object?>(null, "Payment recorded successfully."));
    }

    static async Task<IResult> HandleMonnify(
        HttpRequest request, PaymentService payments,
        IConfiguration config, ILogger logger)
    {
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var signature = request.Headers["monnify-signature"].ToString();
        var secret = config["Monnify:SecretKey"]!;

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLower();

        if (hash != signature)
        {
            logger.LogWarning("Monnify webhook: invalid signature");
            return Results.Unauthorized();
        }

        var payload = JsonSerializer.Deserialize<MonnifyEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload?.EventType != "SUCCESSFUL_TRANSACTION")
            return Results.Ok(ApiResponse.Ok<object?>(null, "Webhook received — non-success event ignored."));

        await payments.RecordPaymentAsync(
            payload.EventData.Customer.Email,
            payload.EventData.AmountPaid * 100,
            payload.EventData.PaymentReference, "monnify");

        return Results.Ok(ApiResponse.Ok<object?>(null, "Payment recorded successfully."));
    }

    static async Task<IResult> HandleFlutterwave(
        HttpRequest request, PaymentService payments,
        IConfiguration config, ILogger logger)
    {
        var verifyHash = request.Headers["verif-hash"].ToString();
        if (verifyHash != config["Flutterwave:SecretHash"]!)
        {
            logger.LogWarning("Flutterwave webhook: invalid hash");
            return Results.Unauthorized();
        }

        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<FlutterwaveEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload?.Event != "charge.completed" || payload.Data.Status != "successful")
            return Results.Ok(ApiResponse.Ok<object?>(null, "Webhook received — non-success event ignored."));

        await payments.RecordPaymentAsync(
            payload.Data.Customer.Email,
            payload.Data.Amount * 100,
            payload.Data.TxRef, "flutterwave");

        return Results.Ok(ApiResponse.Ok<object?>(null, "Payment recorded successfully."));
    }
}

public record PaystackEvent(string Event, PaystackData Data);
public record PaystackData(decimal Amount, string Reference, PaystackCustomer Customer);
public record PaystackCustomer(string Email);

public record MonnifyEvent(string EventType, MonnifyEventData EventData);
public record MonnifyEventData(decimal AmountPaid, string PaymentReference, MonnifyCustomer Customer);
public record MonnifyCustomer(string Email);

public record FlutterwaveEvent(string Event, FlutterwaveData Data);
public record FlutterwaveData(string Status, decimal Amount, string TxRef, FlutterwaveCustomer Customer);
public record FlutterwaveCustomer(string Email);
