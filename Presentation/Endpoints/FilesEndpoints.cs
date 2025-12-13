using FileStorage.Application.DTOs;
using FileStorage.Application.Interfaces;

namespace FileStorage.Presentation.Endpoints;

public static class FilesEndpoints
{
    public static void MapFilesEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/files").WithTags("Файлы");

        group.MapPost("", async (HttpRequest request, IFileStorageService fileService) =>
        {
            if (!request.HasFormContentType) 
            {
                return Results.BadRequest(new { error = "Требуется форма с файлом" });
            }

            IFormCollection form = await request.ReadFormAsync();
            IFormFile? file = form.Files.GetFile("file");
            
            if (file == null || file.Length == 0) 
            {
                return Results.BadRequest(new { error = "Файл отсутствует или пустой" });
            }

            try
            {
                FileDto dto = await fileService.SaveAsync(file);
                return Results.Created($"/api/files/{dto.FileId}", dto);
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(new { error = "Неверные параметры файла", details = argEx.Message });
            }
            catch (UnauthorizedAccessException authEx)
            {
                return Results.Json(new { error = "Нет доступа к файловой системе", details = authEx.Message }, 
                    statusCode: 403);
            }
            catch (IOException ioEx)
            {
                return Results.Json(new { error = "Ошибка ввода-вывода", details = ioEx.Message }, 
                    statusCode: 500);
            }
            catch (InvalidOperationException invOpEx)
            {
                return Results.BadRequest(new { error = "Неверная операция с файлом", details = invOpEx.Message });
            }
        });

        group.MapGet("/{id:guid}", async (Guid id, IFileStorageService fileService) =>
        {
            try
            {
                FileDto? meta = await fileService.GetFileInfoAsync(id);
                
                if (meta == null || !File.Exists(meta.StoragePath)) 
                {
                    return Results.NotFound(new { error = $"Файл с ID {id} не найден" });
                }

                FileStream fileStream = File.OpenRead(meta.StoragePath);
                return Results.File(fileStream, "application/octet-stream", meta.OriginalName);
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(new { error = "Неверный идентификатор файла", details = argEx.Message });
            }
            catch (FileNotFoundException fileEx)
            {
                return Results.NotFound(new { error = "Файл не найден", details = fileEx.Message });
            }
            catch (UnauthorizedAccessException authEx)
            {
                return Results.Json(new { error = "Нет доступа к файлу", details = authEx.Message }, 
                    statusCode: 403);
            }
        });

        group.MapGet("/{id:guid}/meta", async (Guid id, IFileStorageService fileService) =>
        {
            try
            {
                FileDto? meta = await fileService.GetFileInfoAsync(id);
                
                if (meta == null) 
                {
                    return Results.NotFound(new { error = $"Файл с ID {id} не найден" });
                }
                
                return Results.Ok(meta);
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(new { error = "Неверный идентификатор файла", details = argEx.Message });
            }
        });

        group.MapGet("/{id:guid}/text", async (Guid id, IFileStorageService fileService) =>
        {
            try
            {
                string text = await fileService.GetFileTextAsync(id);
                return Results.Text(text, "text/plain; charset=utf-8");
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(new { error = "Неверный идентификатор файла", details = argEx.Message });
            }
            catch (FileNotFoundException fileEx)
            {
                return Results.NotFound(new { error = "Файл не найден", details = fileEx.Message });
            }
            catch (InvalidOperationException invOpEx)
            {
                return Results.BadRequest(new { error = "Файл пустой", details = invOpEx.Message });
            }
            catch (UnauthorizedAccessException authEx)
            {
                return Results.Json(new { error = "Нет доступа к файлу", details = authEx.Message }, 
                    statusCode: 403);
            }
            catch (IOException ioEx)
            {
                return Results.Json(new { error = "Ошибка чтения файла", details = ioEx.Message }, 
                    statusCode: 500);
            }
        });
    }
}