using Gateway.Application.Services;
using Gateway.Infrastructure.HttpClients;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IFileStorageClient, FileStorageClient>(client =>
{
    client.BaseAddress = new Uri("http://file-storage:80");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IFileAnalysisClient, FileAnalysisClient>(client =>
{
    client.BaseAddress = new Uri("http://file-analysis:80");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<GatewayService>();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
    options.RoutePrefix = "swagger";
});

app.MapPost("/works/upload", async (HttpRequest request, [FromServices] GatewayService service) =>
{
    string studentName = request.Query["studentName"].ToString();
    string workId = request.Query["workId"].ToString();
    
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Ожидается multipart/form-data");
    }
    
    IFormCollection form = await request.ReadFormAsync();
    IFormFile file = form.Files["file"];
    
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("Файл обязателен для загрузки");
    }

    if (string.IsNullOrWhiteSpace(studentName))
    {
        return Results.BadRequest("Имя студента обязательно");
    }

    if (string.IsNullOrWhiteSpace(workId))
    {
        return Results.BadRequest("Идентификатор работы обязателен");
    }

    try
    {
        Gateway.Application.DTOs.ReportDto report = await service.UploadWorkAsync(studentName, workId, file);
        return Results.Created($"/works/{workId}/reports", report);
    }
    catch (ArgumentException argEx)
    {
        return Results.BadRequest(new { error = "Неверные параметры запроса", details = argEx.Message });
    }
    catch (HttpRequestException httpEx)
    {
        return Results.Json(new { error = "Сервис недоступен", details = httpEx.Message }, 
            statusCode: 503);
    }
    catch (TimeoutException timeEx)
    {
        return Results.Json(new { error = "Время ожидания истекло", details = timeEx.Message }, 
            statusCode: 504);
    }
    catch (UnauthorizedAccessException authEx)
    {
        return Results.Json(new { error = "Нет доступа к сервису", details = authEx.Message }, 
            statusCode: 403);
    }
    catch (InvalidOperationException invOpEx)
    {
        return Results.Json(new { error = "Неверная операция", details = invOpEx.Message }, 
            statusCode: 400);
    }
    catch (FileNotFoundException fileEx)
    {
        return Results.NotFound(new { error = "Файл не найден", details = fileEx.Message });
    }
    catch (KeyNotFoundException keyEx)
    {
        return Results.NotFound(new { error = "Ресурс не найден", details = keyEx.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}", statusCode: 500);
    }
})
.WithName("UploadWork");

app.MapGet("/works/{workId}/reports", async (string workId, [FromServices] GatewayService service) =>
{
    if (string.IsNullOrWhiteSpace(workId))
    {
        return Results.BadRequest(new { error = "Идентификатор работы обязателен" });
    }

    try
    {
        System.Collections.Generic.IEnumerable<Gateway.Application.DTOs.ReportDto> reports = await service.GetReportsAsync(workId);
        return Results.Ok(reports);
    }
    catch (ArgumentException argEx)
    {
        return Results.BadRequest(new { error = "Неверный идентификатор работы", details = argEx.Message });
    }
    catch (HttpRequestException httpEx)
    {
        return Results.Json(new { error = "Сервис анализа недоступен", details = httpEx.Message }, 
            statusCode: 503);
    }
    catch (KeyNotFoundException keyEx)
    {
        return Results.NotFound(new { error = "Отчеты не найдены", details = keyEx.Message });
    }
    catch (InvalidOperationException invOpEx)
    {
        return Results.Json(new { error = "Неверная операция", details = invOpEx.Message }, 
            statusCode: 400);
    }
    catch (TimeoutException timeEx)
    {
        return Results.Json(new { error = "Время ожидания истекло", details = timeEx.Message }, 
            statusCode: 504);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Внутренняя ошибка: {ex.Message}", statusCode: 500);
    }
})
.WithName("GetWorkReports");

app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "Working", 
    time = DateTime.UtcNow,
    services = new 
    {
        fileStorage = builder.Configuration["FILESTORAGE_URL"] ?? "http://file-storage:80",
        fileAnalysis = builder.Configuration["FILEANALYSIS_URL"] ?? "http://file-analysis:80"
    }
}))
.WithName("HealthCheck");

app.Run("http://0.0.0.0:6000");