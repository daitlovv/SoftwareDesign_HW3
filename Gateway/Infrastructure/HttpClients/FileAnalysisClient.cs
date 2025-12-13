using System.Net.Http.Json;
using System.Text.Json;
using Gateway.Application.DTOs;

namespace Gateway.Infrastructure.HttpClients;

public class FileAnalysisClient : IFileAnalysisClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<FileAnalysisClient> logger;

    public FileAnalysisClient(HttpClient httpClient, ILogger<FileAnalysisClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<ReportDto> AnalyzeFileAsync(Guid fileId, string workId, string studentName)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        if (string.IsNullOrWhiteSpace(workId))
        {
            throw new ArgumentException("Идентификатор работы не может быть пустым");
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            throw new ArgumentException("Имя студента не может быть пустым");
        }

        try
        {
            logger.LogDebug("Анализ файла: {FileId}, работа: {WorkId}", fileId, workId);
            
            object request = new
            {
                FileId = fileId,
                WorkId = workId,
                StudentName = studentName
            };

            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/analyze", request);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Ошибка анализа: {StatusCode}, {Content}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException($"Работа {workId} не найдена");
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new ArgumentException($"Неверный запрос анализа: {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
            }

            ReportDto? report = await response.Content.ReadFromJsonAsync<ReportDto>();
            
            if (report == null) 
            {
                throw new InvalidOperationException("Не удалось десериализовать ответ анализа");
            }

            return report;
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "Ошибка HTTP при анализе файла {FileId}", fileId);
            throw new HttpRequestException($"Ошибка соединения с сервисом анализа: {httpEx.Message}", httpEx);
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Ошибка формата JSON при анализе файла {FileId}", fileId);
            throw new InvalidOperationException($"Неверный формат ответа от сервиса анализа: {jsonEx.Message}");
        }
        catch (TaskCanceledException cancelEx)
        {
            logger.LogError(cancelEx, "Таймаут при анализе файла {FileId}", fileId);
            throw new TimeoutException("Время ожидания анализа истекло", cancelEx);
        }
    }

    public async Task<IEnumerable<ReportDto>> GetReportsAsync(string workId)
    {
        if (string.IsNullOrWhiteSpace(workId))
        {
            throw new ArgumentException("Идентификатор работы не может быть пустым");
        }

        try
        {
            logger.LogDebug("Получение отчетов для работы: {WorkId}", workId);
            
            HttpResponseMessage response = await httpClient.GetAsync($"/api/reports/{workId}");
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Ошибка получения отчетов: {StatusCode}, {Content}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Enumerable.Empty<ReportDto>();
                }
                
                response.EnsureSuccessStatusCode();
            }

            IEnumerable<ReportDto>? reports = await response.Content.ReadFromJsonAsync<IEnumerable<ReportDto>>();
            return reports ?? Enumerable.Empty<ReportDto>();
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "Ошибка HTTP при получении отчетов для работы {WorkId}", workId);
            throw new HttpRequestException($"Ошибка соединения с сервисом анализа: {httpEx.Message}", httpEx);
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Ошибка формата JSON при получении отчетов для работы {WorkId}", workId);
            throw new InvalidOperationException($"Неверный формат ответа от сервиса анализа: {jsonEx.Message}");
        }
        catch (TaskCanceledException cancelEx)
        {
            logger.LogError(cancelEx, "Таймаут при получении отчетов для работы {WorkId}", workId);
            throw new TimeoutException("Время ожидания получения отчетов истекло", cancelEx);
        }
    }
}