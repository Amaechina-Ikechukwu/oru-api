namespace ORUApi.Models;

public class ApplicationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Application Application { get; set; } = null!;
}
