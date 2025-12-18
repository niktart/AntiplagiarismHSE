
//using Gateway.Models;
//using Microsoft.AspNetCore.Mvc;
//using System.Text.Json;

//namespace Gateway.Controllers
//{
//    [ApiController]
//    [Route("api/works")]
//    public class WorksController : ControllerBase
//    {
//        private readonly HttpClient _fileStoringClient;
//        private readonly HttpClient _fileAnalysisClient;
//        private readonly ILogger<WorksController> _logger;

//        public WorksController(
//            IHttpClientFactory httpClientFactory,
//            ILogger<WorksController> logger)
//        {
//            _fileStoringClient = httpClientFactory.CreateClient("FileStoring");
//            _fileAnalysisClient = httpClientFactory.CreateClient("FileAnalysis");
//            _logger = logger;
//        }

//        // 1. Сдать работу (объединяет загрузку файла + создание отчета)
//        [HttpPost("submit")]
//        public async Task<IActionResult> SubmitWork([FromForm] FileUploadDto dto)
//        {
//            try
//            {
//                // 1. Загружаем файл в FileStoring
//                var formData = new MultipartFormDataContent();
//                formData.Add(new StreamContent(dto.File.OpenReadStream()), "File", dto.File.FileName);
//                formData.Add(new StringContent(dto.StudentId), "StudentId");
//                formData.Add(new StringContent(dto.AssignmentId), "AssignmentId");

//                var uploadResponse = await _fileStoringClient.PostAsync("/api/files/upload", formData);
//                if (!uploadResponse.IsSuccessStatusCode)
//                {
//                    return StatusCode((int)uploadResponse.StatusCode,
//                        $"File upload failed: {await uploadResponse.Content.ReadAsStringAsync()}");
//                }

//                var uploadResultJson = await uploadResponse.Content.ReadAsStringAsync();
//                var uploadResult = JsonSerializer.Deserialize<StoredResponse>(uploadResultJson,
//                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//                if (uploadResult == null)
//                {
//                    return BadRequest("Failed to parse upload response");
//                }

//                // 2. Создаем отчет в FileAnalysis
//                var analysisRequest = new
//                {
//                    SubmissionId = uploadResult.submissionId,
//                    StudentId = dto.StudentId,
//                    AssignmentId = dto.AssignmentId,
//                    UploadedAt = uploadResult.uploadedAt,
//                    DownloadUrl = $"{_fileStoringClient.BaseAddress}/api/files/{uploadResult.submissionId}"
//                };

//                var analysisResponse = await _fileAnalysisClient.PostAsJsonAsync("/api/reports/analyze", analysisRequest);
//                if (!analysisResponse.IsSuccessStatusCode)
//                {
//                    // Можно удалить файл или оставить без анализа
//                    _logger.LogWarning($"Analysis failed for submission {uploadResult.submissionId}, but file was saved");
//                }

//                var analysisResult = await analysisResponse.Content.ReadAsStringAsync();

//                return Ok(new
//                {
//                    Message = "Work submitted successfully",
//                    Submission = uploadResult,
//                    AnalysisStatus = analysisResponse.IsSuccessStatusCode ? "Started" : "Failed",
//                    SubmissionId = uploadResult.submissionId
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error submitting work");
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }

//        // 2. Получить отчеты по заданию (ТРЕБОВАНИЕ ИЗ ЗАДАНИЯ)
//        [HttpGet("{assignmentId}/reports")]
//        public async Task<IActionResult> GetReports(string assignmentId)
//        {
//            try
//            {
//                var response = await _fileAnalysisClient.GetAsync($"/api/works/{assignmentId}/reports");

//                if (!response.IsSuccessStatusCode)
//                {
//                    return StatusCode((int)response.StatusCode,
//                        $"Failed to get reports: {await response.Content.ReadAsStringAsync()}");
//                }

//                var reportsJson = await response.Content.ReadAsStringAsync();
//                return Content(reportsJson, "application/json");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting reports");
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }

//        // 3. Получить конкретный отчет по submissionId
//        [HttpGet("submission/{submissionId}/report")]
//        public async Task<IActionResult> GetReport(Guid submissionId)
//        {
//            try
//            {
//                var response = await _fileAnalysisClient.GetAsync($"/api/reports/{submissionId}");

//                if (!response.IsSuccessStatusCode)
//                {
//                    return StatusCode((int)response.StatusCode,
//                        $"Failed to get report: {await response.Content.ReadAsStringAsync()}");
//                }

//                var reportJson = await response.Content.ReadAsStringAsync();
//                return Content(reportJson, "application/json");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting report");
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }

//        // 4. Получить список сданных работ по заданию
//        [HttpGet("{assignmentId}/submissions")]
//        public async Task<IActionResult> GetSubmissions(string assignmentId)
//        {
//            try
//            {
//                var response = await _fileStoringClient.GetAsync($"/api/files/submissions/{assignmentId}");

//                if (!response.IsSuccessStatusCode)
//                {
//                    return StatusCode((int)response.StatusCode,
//                        $"Failed to get submissions: {await response.Content.ReadAsStringAsync()}");
//                }

//                var submissionsJson = await response.Content.ReadAsStringAsync();
//                return Content(submissionsJson, "application/json");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting submissions");
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }
//    }
//}



using Gateway.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Gateway.Controllers
{
    [ApiController]
    [Route("api/works")]
    public class WorksController : ControllerBase
    {
        private readonly HttpClient _fileStoringClient;
        private readonly HttpClient _fileAnalysisClient;
        private readonly ILogger<WorksController> _logger;

        public WorksController(
            IHttpClientFactory httpClientFactory,
            ILogger<WorksController> logger)
        {
            _fileStoringClient = httpClientFactory.CreateClient("FileStoring");
            _fileAnalysisClient = httpClientFactory.CreateClient("FileAnalysis");
            _logger = logger;
        }

        // 1. Сдать работу (объединяет загрузку файла + создание отчета)
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitWork([FromForm] FileUploadDto dto)
        {
            try
            {
                // 1. Загружаем файл в FileStoring
                var formData = new MultipartFormDataContent();
                formData.Add(new StreamContent(dto.File.OpenReadStream()), "File", dto.File.FileName);
                formData.Add(new StringContent(dto.StudentId), "StudentId");
                formData.Add(new StringContent(dto.AssignmentId), "AssignmentId");

                var uploadResponse = await _fileStoringClient.PostAsync("/api/files/upload", formData);
                if (!uploadResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)uploadResponse.StatusCode,
                        $"File upload failed: {await uploadResponse.Content.ReadAsStringAsync()}");
                }

                var uploadResultJson = await uploadResponse.Content.ReadAsStringAsync();
                var uploadResult = JsonSerializer.Deserialize<StoredResponse>(uploadResultJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (uploadResult == null)
                {
                    return BadRequest("Failed to parse upload response");
                }

                // FIX: Убедимся, что URL не содержит двойного слеша
                var baseAddress = _fileStoringClient.BaseAddress?.ToString().TrimEnd('/');

                // 2. Создаем отчет в FileAnalysis
                var analysisRequest = new
                {
                    SubmissionId = uploadResult.submissionId,
                    StudentId = dto.StudentId,
                    AssignmentId = dto.AssignmentId,
                    UploadedAt = uploadResult.uploadedAt,
                    DownloadUrl = $"{baseAddress}/api/files/{uploadResult.submissionId}"
                };

                var analysisResponse = await _fileAnalysisClient.PostAsJsonAsync("/api/reports/analyze", analysisRequest);
                if (!analysisResponse.IsSuccessStatusCode)
                {
                    // Можно удалить файл или оставить без анализа
                    _logger.LogWarning($"Analysis failed for submission {uploadResult.submissionId}, but file was saved");
                }

                var analysisResult = await analysisResponse.Content.ReadAsStringAsync();

                return Ok(new
                {
                    Message = "Work submitted successfully",
                    Submission = uploadResult,
                    AnalysisStatus = analysisResponse.IsSuccessStatusCode ? "Started" : "Failed",
                    SubmissionId = uploadResult.submissionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting work");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // 2. Получить отчеты по заданию (ТРЕБОВАНИЕ ИЗ ЗАДАНИЯ)
        // FIX: Изменяем маршрут - используем правильный endpoint из FileAnalysis
        [HttpGet("{assignmentId}/reports")]
        public async Task<IActionResult> GetReports(string assignmentId)
        {
            try
            {
                // FIX: Правильный маршрут - reports/assignment/{assignmentId}
                var response = await _fileAnalysisClient.GetAsync($"/api/reports/assignment/{assignmentId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound(new
                        {
                            Message = $"No reports found for assignment {assignmentId}",
                            AssignmentId = assignmentId
                        });
                    }

                    return StatusCode((int)response.StatusCode,
                        $"Failed to get reports: {await response.Content.ReadAsStringAsync()}");
                }

                var reportsJson = await response.Content.ReadAsStringAsync();
                return Content(reportsJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // 3. Получить конкретный отчет по submissionId
        [HttpGet("submission/{submissionId}/report")]
        public async Task<IActionResult> GetReport(Guid submissionId)
        {
            try
            {
                // FIX: Правильный маршрут - reports/{submissionId}
                var response = await _fileAnalysisClient.GetAsync($"/api/reports/{submissionId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound(new
                        {
                            Message = $"Report for submission {submissionId} not found",
                            SubmissionId = submissionId
                        });
                    }

                    return StatusCode((int)response.StatusCode,
                        $"Failed to get report: {await response.Content.ReadAsStringAsync()}");
                }

                var reportJson = await response.Content.ReadAsStringAsync();
                return Content(reportJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // 4. Получить список сданных работ по заданию
        [HttpGet("{assignmentId}/submissions")]
        public async Task<IActionResult> GetSubmissions(string assignmentId)
        {
            try
            {
                var response = await _fileStoringClient.GetAsync($"/api/files/submissions/{assignmentId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound(new
                        {
                            Message = $"No submissions found for assignment {assignmentId}",
                            AssignmentId = assignmentId
                        });
                    }

                    return StatusCode((int)response.StatusCode,
                        $"Failed to get submissions: {await response.Content.ReadAsStringAsync()}");
                }

                var submissionsJson = await response.Content.ReadAsStringAsync();
                return Content(submissionsJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submissions");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // 5. Дополнительный метод: получить файл по ID
        [HttpGet("file/{submissionId}")]
        public async Task<IActionResult> GetFile(Guid submissionId)
        {
            try
            {
                var response = await _fileStoringClient.GetAsync($"/api/files/{submissionId}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode,
                        $"Failed to get file: {await response.Content.ReadAsStringAsync()}");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var content = await response.Content.ReadAsByteArrayAsync();

                return File(content, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
