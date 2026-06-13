using System;

namespace ORUApi.Models
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public int CreditUnits { get; set; } = 3;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public Guid CreatedByAdminId { get; set; }
        public Guid? UpdatedByAdminId { get; set; }
        public Guid? DeletedByAdminId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
