using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ORUApi.Data;
using ORUApi.Models;

namespace ORUApi.Services;

public class AdminActivityLogger
{
    private readonly ORUDbContext _db;
    private readonly ILogger<AdminActivityLogger> _logger;

    public AdminActivityLogger(ORUDbContext db, ILogger<AdminActivityLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(Guid adminId, string action, string details, string? ipAddress = null)
    {
        try
        {
            var log = new AdminActivityLog
            {
                AdminId = adminId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _db.AdminActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write admin activity log to database.");
        }
    }
}
