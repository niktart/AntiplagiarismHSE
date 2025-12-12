using System;
using System.ComponentModel.DataAnnotations;

namespace FileAnalysis.Data
{
    public class Report
    {
        [Key]
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public string StudentId { get; set; } = "";
        public string AssignmentId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsPlagiarism { get; set; }
        public string DetailsJson { get; set; } = "";
        public string WordCloudUrl { get; set; } = "";
    }
}
