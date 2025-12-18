//using System.ComponentModel.DataAnnotations;

//namespace Gateway.Models
//{
//    public class StoredResponse
//    {
//        public Guid submissionId { get; set; }
//        public string? studentId { get; set; }
//        public string? assignmentId { get; set; }
//        public DateTime uploadedAt { get; set; }
//    }

//    public class FileUploadDto
//    {
//        [Required]
//        public IFormFile File { get; set; }

//        [Required]
//        public string StudentId { get; set; }

//        [Required]
//        public string AssignmentId { get; set; }
//    }

//    public class FileUploadFromPathDto
//    {
//        [Required]
//        public string FullFilePath { get; set; }

//        [Required]
//        public string StudentId { get; set; }

//        [Required]
//        public string AssignmentId { get; set; }
//    }
//}


using System.ComponentModel.DataAnnotations;

namespace Gateway.Models
{
    // Существующие DTO
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

    // Новые DTO для отчетов
    public class ReportResponse
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public string StudentId { get; set; }
        public string AssignmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPlagiarism { get; set; }
        public string WordCloudUrl { get; set; }
        public string DetailsJson { get; set; }
    }

    public class WorkSubmissionResponse
    {
        public Guid Id { get; set; }
        public string StudentId { get; set; }
        public string AssignmentId { get; set; }
        public DateTime UploadedAt { get; set; }
        public string FileName { get; set; }
        public ReportResponse? Report { get; set; }
    }

    public class SubmitWorkResponse
    {
        public string Message { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid? ReportId { get; set; }
        public string AnalysisStatus { get; set; }
    }
}