using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ORUApi.Models
{
  // Models/Announcement.cs
public class Announcement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public AnnouncementCategory Category { get; set; }
    public List<string> ImageUrls { get; set; } = [];  // Required for Gallery
    public bool IsPublished { get; set; } = true;
    public Guid PostedByAdminId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

public enum AnnouncementCategory { News, Event, Seminar, Academic, Gallery }
}