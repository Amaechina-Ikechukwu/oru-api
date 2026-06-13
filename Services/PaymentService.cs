using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ORUApi.Data;
using ORUApi.Models;

namespace ORUApi.Services;

public class PaymentService(IConfiguration config, ORUDbContext db)
{
    public bool VerifyPaystackSignature(string requestBody, string signatureHeader)
    {
        var secret = config["Paystack:SecretKey"]!;
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody))).ToLower();
        return hash == signatureHeader;
    }

    public async Task<bool> RecordPaymentAsync(
        string email,
        decimal amountInKobo,
        string reference,
        string gateway = "paystack")
    {
        var amount = amountInKobo / 100m;

        var student = await db.Students.FirstOrDefaultAsync(s => s.Email == email);
        if (student is not null)
        {
            student.TotalAmountPaid += amount;
            student.PaymentHistory.Add(new PaymentInstallment
            {
                Amount = amount,
                PaymentReference = reference,
                Gateway = gateway,
                PaidAt = DateTime.UtcNow
            });
        }

        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Email == email && !a.ApplicationFeePaid);
        if (application is not null)
        {
            application.ApplicationFeePaid = true;
            application.PaymentReference = reference;
        }

        if (student is null && application is null) return false;

        await db.SaveChangesAsync();
        return true;
    }
}