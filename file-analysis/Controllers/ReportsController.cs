using FileAnalysis.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FileAnalysis.Controllers
{
    public record AnalyzeRequest(Guid SubmissionId, string StudentId, string AssignmentId, DateTime UploadedAt, string DownloadUrl);

    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly AnalysisDbContext _db;
        private readonly IHttpClientFactory _hc;
        private readonly IConfiguration _cfg;

        public ReportsController(AnalysisDbContext db, IHttpClientFactory hc, IConfiguration cfg)
        {
            _db = db;
            _hc = hc;
            _cfg = cfg;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest req)
        {
            // download file
            var client = _hc.CreateClient();
            HttpResponseMessage fileResp;
            try
            {
                fileResp = await client.GetAsync(req.DownloadUrl);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cannot download file: {ex.Message}");
            }

            if (!fileResp.IsSuccessStatusCode) return StatusCode((int)fileResp.StatusCode, "Failed to download file.");

            var text = await fileResp.Content.ReadAsStringAsync();

            // simple tokenize
            var tokens = Tokenize(text);
            var freq = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
            var top = freq.OrderByDescending(kv => kv.Value).Take(50).ToList();

            // build wordcloud text by repeating words proportional to frequency (capped)
            var wcWords = new List<string>();
            foreach (var kv in top)
            {
                int repeat = Math.Clamp(kv.Value, 1, 10); // cap repeats to 10
                for (int i = 0; i < repeat; i++) wcWords.Add(kv.Key);
            }
            var wcText = string.Join(' ', wcWords);
            var wcUrl = $"https://quickchart.io/wordcloud?text={System.Net.WebUtility.UrlEncode(wcText)}&format=png";

            // plagiarism check: call file-storing to get earlier submissions by assignment
            var fsUrl = _cfg["FILE_STORING_URL"] ?? "http://file-storing";
            List<SubmissionInfo> earlierSubmissions = new();
            bool isPlag = false;
            try
            {
                var listResp = await client.GetAsync($"{fsUrl}/api/files/submissions/{req.AssignmentId}");
                if (listResp.IsSuccessStatusCode)
                {
                    var body = await listResp.Content.ReadAsStringAsync();
                    earlierSubmissions = JsonSerializer.Deserialize<List<SubmissionInfo>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    var earlier = earlierSubmissions
                        .Where(s => s.studentId != req.StudentId && DateTime.TryParse(s.uploadedAt, out var dt) && dt < req.UploadedAt)
                        .ToList();
                    if (earlier.Any()) isPlag = true;
                }
            }
            catch
            {
                // ignore network errors but log ideally
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                SubmissionId = req.SubmissionId,
                StudentId = req.StudentId,
                AssignmentId = req.AssignmentId,
                CreatedAt = DateTime.UtcNow,
                IsPlagiarism = isPlag,
                DetailsJson = JsonSerializer.Serialize(new { topWords = top, candidates = earlierSubmissions }),
                WordCloudUrl = wcUrl
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return Ok(report);
        }

        [HttpGet("{submissionId:guid}")]
        public async Task<IActionResult> GetReport(Guid submissionId)
        {
            var r = await _db.Reports.FirstOrDefaultAsync(x => x.SubmissionId == submissionId);
            if (r == null) return NotFound();
            return Ok(r);
        }

        [HttpGet("/api/works/{assignmentId}/reports")]
        public async Task<IActionResult> GetByAssignment(string assignmentId)
        {
            var list = await _db.Reports
                .Where(r => r.AssignmentId == assignmentId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new { r.Id, r.SubmissionId, r.StudentId, r.IsPlagiarism, r.CreatedAt, r.WordCloudUrl })
                .ToListAsync();
            return Ok(list);
        }

        static IEnumerable<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            text = text.ToLowerInvariant();
            // remove non-letter/digit chars
            var matches = Regex.Matches(text, @"\p{L}+");
            var tokens = matches.Select(m => m.Value).Where(s => s.Length >= 2).ToList();

            // simple stopwords removal
            var stopwords = new HashSet<string>(new[] {
                "the","and","for","that","with","this","from","have","you","your","are","but","not","was","but","they","their","our","who","whom","what","when","where","why","how","which","a","an","to","in","on","of","is","it"
            });
            return tokens.Where(t => !stopwords.Contains(t));
        }

        private class SubmissionInfo
        {
            public Guid id { get; set; }
            public string studentId { get; set; } = "";
            public string assignmentId { get; set; } = "";
            public string uploadedAt { get; set; } = "";
        }
    }
}
