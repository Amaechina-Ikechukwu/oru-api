using System;
using System.Text.Json.Serialization;

namespace ORUApi.Models;

public class AdminActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminId { get; set; }
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public Admin? Admin { get; set; }
}
