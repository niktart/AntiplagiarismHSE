using System.ComponentModel.DataAnnotations;

namespace Gateway.Models
{
    public class StoredResponse
    {
        public Guid submissionId { get; set; }
        public string? studentId { get; set; }
        public string? assignmentId { get; set; }
        public DateTime uploadedAt { get; set; }
    }

    public class FileUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string AssignmentId { get; set; }
    }

    public class FileUploadFromPathDto
    {
        [Required]
        public string FullFilePath { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string AssignmentId { get; set; }
    }
}
