using System;

namespace ORUApi.Models
{
    public class StudyLevel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Duration { get; set; } = "";
        public int SortOrder { get; set; }
        public decimal ApplicationFee { get; set; }
        public decimal TuitionFee { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
