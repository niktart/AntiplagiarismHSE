using Gateway.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Gateway.Controllers
{
    [ApiController]
    [Route("works")]
    public class WorksController : ControllerBase
    {
        private readonly IHttpClientFactory _hc;
        private readonly IConfiguration _cfg;

        public WorksController(IHttpClientFactory hc, IConfiguration cfg)
        {
            _hc = hc;
            _cfg = cfg;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromForm] IFormFile? file, [FromForm] string? studentId, [FromForm] string? assignmentId)
        {
            if (file == null) return BadRequest("file missing");
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(assignmentId))
                return BadRequest("studentId and assignmentId required");

            var client = _hc.CreateClient();
            var fsUrl = _cfg["FILE_STORING_URL"] ?? "http://file-storing";

            // prepare multipart to file-storing
            using var content = new MultipartFormDataContent();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            content.Add(new StreamContent(ms), "file", file.FileName);
            content.Add(new StringContent(studentId), "studentId");
            content.Add(new StringContent(assignmentId), "assignmentId");

            var resp = await client.PostAsync($"{fsUrl}/api/files/upload", content);
            if (!resp.IsSuccessStatusCode)
            {
                var txt = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode, $"File storing failed: {txt}");
            }

            var respText = await resp.Content.ReadAsStringAsync();
            var stored = JsonSerializer.Deserialize<StoredResponse>(respText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // call analysis
            var faUrl = _cfg["FILE_ANALYSIS_URL"] ?? "http://file-analysis";
            var analyzeRequest = new
            {
                SubmissionId = stored!.submissionId,
                StudentId = stored!.studentId ?? studentId,
                AssignmentId = stored!.assignmentId ?? assignmentId,
                UploadedAt = stored!.uploadedAt,
                DownloadUrl = $"{fsUrl}/api/files/{stored!.submissionId}"
            };

            var arJson = new StringContent(JsonSerializer.Serialize(analyzeRequest), Encoding.UTF8, "application/json");
            var arResp = await client.PostAsync($"{faUrl}/api/reports/analyze", arJson);

            string analysisResult = arResp.IsSuccessStatusCode ? await arResp.Content.ReadAsStringAsync() : null;

            return Ok(new { stored = stored, analysis = JsonDocument.Parse(analysisResult ?? "{}") });
        }
    }
}
