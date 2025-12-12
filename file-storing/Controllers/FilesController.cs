using FileStoring.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
//using FileStoring.Data.Models; // Если WorkSubmission здесь

namespace FileStoring.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly StoringDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;

        public FilesController(StoringDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _db = db;
            _env = env;
            _cfg = cfg;
        }

        // DTO для обычной загрузки файла
        public class FileUploadDto
        {
            [Required]
            public IFormFile File { get; set; }

            [Required]
            public string StudentId { get; set; }

            [Required]
            public string AssignmentId { get; set; }
        }

        // DTO для загрузки по полному пути
        public class FileUploadFromPathDto
        {
            [Required]
            public string FullFilePath { get; set; }

            [Required]
            public string StudentId { get; set; }

            [Required]
            public string AssignmentId { get; set; }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] FileUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file provided.");

            var storagePath = _cfg["FILE_STORAGE_PATH"] ?? Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(storagePath);

            var submission = new WorkSubmission
            {
                Id = Guid.NewGuid(),
                StudentId = dto.StudentId,
                AssignmentId = dto.AssignmentId,
                FileName = dto.File.FileName,
                UploadedAt = DateTime.UtcNow
            };

            var savedName = $"{submission.Id}_{Path.GetFileName(dto.File.FileName)}";
            var savedPath = Path.Combine(storagePath, savedName);

            using var stream = System.IO.File.Create(savedPath);
            await dto.File.CopyToAsync(stream);

            submission.FilePath = savedPath;
            _db.WorkSubmissions.Add(submission);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                submissionId = submission.Id,
                studentId = submission.StudentId,
                assignmentId = submission.AssignmentId,
                fileName = submission.FileName,
                uploadedAt = submission.UploadedAt
            });
        }

        [HttpPost("upload-from-path")]
        public async Task<IActionResult> UploadFromPath([FromForm] FileUploadFromPathDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullFilePath))
                return BadRequest("FullFilePath is required.");

            if (!System.IO.File.Exists(dto.FullFilePath))
                return BadRequest("File not found at the specified path.");

            var storagePath = _cfg["FILE_STORAGE_PATH"] ?? Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(storagePath);

            var fileName = Path.GetFileName(dto.FullFilePath);

            var submission = new WorkSubmission
            {
                Id = Guid.NewGuid(),
                StudentId = dto.StudentId,
                AssignmentId = dto.AssignmentId,
                FileName = fileName,
                UploadedAt = DateTime.UtcNow
            };

            var savedName = $"{submission.Id}_{fileName}";
            var savedPath = Path.Combine(storagePath, savedName);

            System.IO.File.Copy(dto.FullFilePath, savedPath, overwrite: true);
            submission.FilePath = savedPath;

            _db.WorkSubmissions.Add(submission);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                submissionId = submission.Id,
                studentId = submission.StudentId,
                assignmentId = submission.AssignmentId,
                fileName = submission.FileName,
                uploadedAt = submission.UploadedAt
            });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var s = await _db.WorkSubmissions.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            if (!System.IO.File.Exists(s.FilePath)) return NotFound("File not found on disk.");
            var fs = System.IO.File.OpenRead(s.FilePath);
            return File(fs, "application/octet-stream", s.FileName);
        }

        [HttpGet("submissions/{assignmentId}")]
        public async Task<IActionResult> GetByAssignment(string assignmentId)
        {
            var list = await _db.WorkSubmissions
                .Where(w => w.AssignmentId == assignmentId)
                .OrderBy(w => w.UploadedAt)
                .Select(w => new
                {
                    id = w.Id,
                    studentId = w.StudentId,
                    assignmentId = w.AssignmentId,
                    uploadedAt = w.UploadedAt
                })
                .ToListAsync();
            return Ok(list);
        }
    }
}
