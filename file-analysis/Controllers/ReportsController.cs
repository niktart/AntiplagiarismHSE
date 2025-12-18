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
            var client = _hc.CreateClient();

            // 1. Скачать файл текущей работы
            string text;
            try
            {
                // Убедимся, что URL правильный
                var downloadUrl = req.DownloadUrl;
                var resp = await client.GetAsync(downloadUrl);
                resp.EnsureSuccessStatusCode();
                text = await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cannot download file: {ex.Message}");
            }

            // 2. Получить предыдущие сабмишены для задания
            var fsUrl = (_cfg["FILE_STORING_URL"] ?? "http://file-storing").TrimEnd('/');
            List<SubmissionInfo> earlierSubmissions = new();
            try
            {
                // FIX: Правильный URL без двойного слеша
                var listResp = await client.GetAsync($"{fsUrl}/api/files/submissions/{req.AssignmentId}");
                if (listResp.IsSuccessStatusCode)
                {
                    var body = await listResp.Content.ReadAsStringAsync();
                    earlierSubmissions = JsonSerializer.Deserialize<List<SubmissionInfo>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching submissions: {ex.Message}");
            }

            // 3. Проверка на плагиат с шинглами
            bool isPlag = false;
            var similarSubmissions = new List<Guid>();

            foreach (var s in earlierSubmissions.Where(s => s.studentId != req.StudentId))
            {
                try
                {
                    var fileResp = await client.GetAsync($"{fsUrl}/api/files/{s.id}");
                    if (!fileResp.IsSuccessStatusCode) continue;
                    var otherText = await fileResp.Content.ReadAsStringAsync();

                    if (AreTextsSimilar(text, otherText))
                    {
                        isPlag = true;
                        similarSubmissions.Add(s.id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error comparing with submission {s.id}: {ex.Message}");
                    continue;
                }
            }

            // 4. Статистика слов
            var tokens = Tokenize(text);
            var freq = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
            var top = freq.OrderByDescending(kv => kv.Value).Take(50).ToList();
            var wcWords = new List<string>();
            foreach (var kv in top)
            {
                int repeat = Math.Clamp(kv.Value, 1, 10);
                for (int i = 0; i < repeat; i++) wcWords.Add(kv.Key);
            }
            var wcText = string.Join(' ', wcWords);
            var wcUrl = $"https://quickchart.io/wordcloud?text={System.Net.WebUtility.UrlEncode(wcText)}&format=png";

            // 5. Сохраняем отчет
            var report = new Report
            {
                Id = Guid.NewGuid(),
                SubmissionId = req.SubmissionId,
                StudentId = req.StudentId,
                AssignmentId = req.AssignmentId,
                CreatedAt = DateTime.UtcNow,
                IsPlagiarism = isPlag,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    topWords = top.Select(kv => new { word = kv.Key, count = kv.Value }),
                    similarSubmissions = similarSubmissions,
                    totalCompared = earlierSubmissions.Count
                }),
                WordCloudUrl = wcUrl
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Analysis completed",
                reportId = report.Id,
                isPlagiarism = isPlag,
                wordCloudUrl = wcUrl
            });
        }

        [HttpGet("{submissionId:guid}")]
        public async Task<IActionResult> GetReportBySubmissionId(Guid submissionId)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.SubmissionId == submissionId);
            if (report == null) return NotFound($"Report for submission {submissionId} not found");
            return Ok(report);
        }

        [HttpGet("assignment/{assignmentId}")]
        public async Task<IActionResult> GetReportsByAssignment(string assignmentId)
        {
            var reports = await _db.Reports
                .Where(r => r.AssignmentId == assignmentId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.SubmissionId,
                    r.StudentId,
                    r.IsPlagiarism,
                    r.CreatedAt,
                    r.WordCloudUrl
                })
                .ToListAsync();

            return Ok(new
            {
                assignmentId = assignmentId,
                totalReports = reports.Count,
                reports = reports
            });
        }

        // Вспомогательные методы остаются без изменений
        private static IEnumerable<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            text = text.ToLowerInvariant();
            var matches = Regex.Matches(text, @"\p{L}+");
            var tokens = matches.Select(m => m.Value).Where(s => s.Length >= 2).ToList();
            var stopwords = new HashSet<string>(new[] {
                "the","and","for","that","with","this","from","have","you","your","are","but","not","was","they","their","our","who","whom","what","when","where","why","how","which","a","an","to","in","on","of","is","it"
            });
            return tokens.Where(t => !stopwords.Contains(t));
        }

        private static bool AreTextsSimilar(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;

            var tokensA = Tokenize(a).ToList();
            var tokensB = Tokenize(b).ToList();

            // Если тексты слишком короткие
            if (tokensA.Count < 3 || tokensB.Count < 3)
                return tokensA.SequenceEqual(tokensB);

            var shinglesA = GetShingles(tokensA, 3);
            var shinglesB = GetShingles(tokensB, 3);

            var intersection = shinglesA.Intersect(shinglesB).Count();
            var union = shinglesA.Union(shinglesB).Count();

            if (union == 0) return false;

            double similarity = (double)intersection / union;
            return similarity > 0.3;
        }

        private static HashSet<string> GetShingles(List<string> tokens, int k)
        {
            var shingles = new HashSet<string>();
            for (int i = 0; i <= tokens.Count - k; i++)
            {
                shingles.Add(string.Join(" ", tokens.Skip(i).Take(k)));
            }
            return shingles;
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