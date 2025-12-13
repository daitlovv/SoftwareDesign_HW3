using System.Net.Http.Json;

namespace FileAnalysis.Application.Services;

public class HttpClientFileContentProvider : IFileContentProvider
{
    private readonly HttpClient httpClient;
    private readonly ILogger<HttpClientFileContentProvider> logger;

    public HttpClientFileContentProvider(
        HttpClient httpClient,
        ILogger<HttpClientFileContentProvider> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<string> GetFileContentAsync(Guid fileId)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync($"/api/files/{fileId}/text");
            response.EnsureSuccessStatusCode();
            
            string content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Содержимое файла пустое");
            }
            
            return content;
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "Ошибка HTTP при получении содержимого файла {FileId}", fileId);
            throw new HttpRequestException($"Не удалось получить содержимое файла: {httpEx.Message}", httpEx);
        }
        catch (InvalidOperationException invOpEx)
        {
            logger.LogError(invOpEx, "Неверная операция при получении файла {FileId}", fileId);
            throw new InvalidOperationException($"Неверная операция с файлом: {invOpEx.Message}", invOpEx);
        }
        catch (TaskCanceledException cancelEx)
        {
            logger.LogError(cancelEx, "Таймаут при получении файла {FileId}", fileId);
            throw new TimeoutException("Время ожидания ответа истекло", cancelEx);
        }
    }
}