using System.Net.Http.Headers;
using System.Text.Json;
using Gateway.Application.DTOs;


namespace Gateway.Infrastructure.HttpClients;

public class FileStorageClient : IFileStorageClient
{
    private readonly HttpClient httpClient;

    public FileStorageClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<Guid> UploadFileAsync(IFormFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (file.Length == 0)
        {
            throw new ArgumentException("Файл не может быть пустым");
        }

        if (file.Length > 50 * 1024 * 1024)
        {
            throw new ArgumentException("Размер файла превышает допустимый лимит 50MB");
        }

        using MultipartFormDataContent content = new MultipartFormDataContent();

        StreamContent streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");

        content.Add(streamContent, "file", file.FileName);

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync("/api/files", content);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new ArgumentException($"Неверный запрос загрузки файла: {errorContent}");
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
                {
                    throw new ArgumentException("Файл слишком большой");
                }
                
                response.EnsureSuccessStatusCode();
            }

            Stream responseStream = await response.Content.ReadAsStreamAsync();
            var dto = await JsonSerializer.DeserializeAsync<FileDto>(
                responseStream, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (dto == null) 
            {
                throw new InvalidOperationException("Не удалось десериализовать ответ от хранилища файлов");
            }

            return dto.FileId;
        }
        catch (HttpRequestException httpEx)
        {
            throw new HttpRequestException($"Ошибка соединения с хранилищем файлов: {httpEx.Message}", httpEx);
        }
        catch (JsonException jsonEx)
        {
            throw new InvalidOperationException($"Неверный формат ответа от хранилища файлов: {jsonEx.Message}");
        }
        catch (TaskCanceledException cancelEx)
        {
            throw new TimeoutException("Время ожидания загрузки файла истекло", cancelEx);
        }
        catch (UnauthorizedAccessException authEx)
        {
            throw new UnauthorizedAccessException("Нет доступа к хранилищу файлов", authEx);
        }
    }

    public async Task<string> GetFileTextAsync(Guid fileId)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        try
        {
            HttpResponseMessage response = await httpClient.GetAsync($"/api/files/{fileId}/text");
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new FileNotFoundException($"Файл с ID {fileId} не найден");
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new ArgumentException($"Неверный запрос файла: {errorContent}");
                }
                
                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException httpEx)
        {
            throw new HttpRequestException($"Ошибка соединения с хранилищем файлов: {httpEx.Message}", httpEx);
        }
        catch (TaskCanceledException cancelEx)
        {
            throw new TimeoutException("Время ожидания получения файла истекло", cancelEx);
        }
        catch (UnauthorizedAccessException authEx)
        {
            throw new UnauthorizedAccessException("Нет доступа к файлу", authEx);
        }
    }
}