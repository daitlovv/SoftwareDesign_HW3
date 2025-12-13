using Gateway.Application.DTOs;
using Gateway.Infrastructure.HttpClients;

namespace Gateway.Application.Services;

public class GatewayService
{
    private readonly IFileStorageClient fileStorageClient;
    private readonly IFileAnalysisClient fileAnalysisClient;
    private readonly ILogger<GatewayService> logger;

    public GatewayService(
        IFileStorageClient fileStorageClient, 
        IFileAnalysisClient fileAnalysisClient,
        ILogger<GatewayService> logger)
    {
        this.fileStorageClient = fileStorageClient;
        this.fileAnalysisClient = fileAnalysisClient;
        this.logger = logger;
    }

    public async Task<ReportDto> UploadWorkAsync(string studentName, string workId, IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(studentName))
        {
            throw new ArgumentException("Имя студента не может быть пустым");
        }

        if (string.IsNullOrWhiteSpace(workId))
        {
            throw new ArgumentException("Идентификатор работы не может быть пустым");
        }

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Файл не может быть пустым");
        }

        try
        {
            logger.LogInformation("Загрузка работы студента: {Student}, работа: {WorkId}", studentName, workId);
            
            Guid fileId = await fileStorageClient.UploadFileAsync(file);
            logger.LogInformation("Файл загружен с ID: {FileId}", fileId);
            
            ReportDto report = await fileAnalysisClient.AnalyzeFileAsync(fileId, workId, studentName);
            logger.LogInformation("Анализ завершен для файла: {FileId}", fileId);
            
            return report;
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "Ошибка сети при загрузке работы");
            throw new HttpRequestException($"Сервис недоступен: {httpEx.Message}", httpEx);
        }
        catch (TimeoutException timeEx)
        {
            logger.LogError(timeEx, "Таймаут при загрузке работы");
            throw new TimeoutException("Время ожидания ответа от сервиса истекло", timeEx);
        }
        catch (ArgumentException argEx)
        {
            logger.LogError(argEx, "Неверные аргументы при загрузке работы");
            throw new ArgumentException($"Неверные параметры запроса: {argEx.Message}", argEx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Неизвестная ошибка при загрузке работы. Student: {Student}, Work: {WorkId}", studentName, workId);
            throw new InvalidOperationException($"Внутренняя ошибка сервера при обработке запроса: {ex.Message}", ex);
        }
    }
    
    public async Task<System.Collections.Generic.IEnumerable<ReportDto>> GetReportsAsync(string workId)
    {
        if (string.IsNullOrWhiteSpace(workId))
        {
            throw new ArgumentException("Идентификатор работы не может быть пустым");
        }

        try
        {
            logger.LogInformation("Получение отчетов для работы: {WorkId}", workId);
            return await fileAnalysisClient.GetReportsAsync(workId);
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "Ошибка сети при получении отчетов");
            throw new HttpRequestException($"Сервис анализа недоступен: {httpEx.Message}", httpEx);
        }
        catch (KeyNotFoundException keyEx)
        {
            logger.LogWarning(keyEx, "Отчеты для работы {WorkId} не найдены", workId);
            throw new KeyNotFoundException($"Отчеты для работы {workId} не найдены");
        }
        catch (InvalidOperationException invOpEx)
        {
            logger.LogError(invOpEx, "Неверная операция при получении отчетов для {WorkId}", workId);
            throw new InvalidOperationException($"Неверная операция с отчетами: {invOpEx.Message}");
        }
    }
}