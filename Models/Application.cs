using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ORUApi.Models
{
    public class Application
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateOnly DateOfBirth { get; set; }
        public string Address { get; set; } = "";

        public string SelectedProgram { get; set; } = "";

        public int StudyLevelId { get; set; }

        [ForeignKey(nameof(StudyLevelId))]
        public StudyLevel? StudyLevelRef { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public bool ApplicationFeePaid { get; set; } = false;
        public string? PaymentReference { get; set; }

        public List<string> DocumentUrls { get; set; } = [];

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ApplicationStatus { Pending, UnderReview, Approved, Rejected }
}
