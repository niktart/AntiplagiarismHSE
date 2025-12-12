using System;
using System.ComponentModel.DataAnnotations;

namespace FileStoring.Data
{
    public class WorkSubmission
    {
        [Key]
        public Guid Id { get; set; }
        public string StudentId { get; set; } = "";
        public string AssignmentId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime UploadedAt { get; set; }
    }
}
