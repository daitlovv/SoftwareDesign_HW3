using FileAnalysis.Application;
using FileAnalysis.Application.DTOs;
using FileAnalysis.Application.Interfaces;
using FileAnalysis.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFileAnalysisServices("Data Source=fileanalysis.db");

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    FileAnalysisDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileAnalysisDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API анализа файлов v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapPost("/api/analyze", async (AnalyzeRequestDto request, IFileAnalysisService service) =>
{
    ReportDto report = await service.AnalyzeFileAsync(request.FileId, request.WorkId, request.StudentName);
    return Results.Ok(report);
});

app.MapGet("/api/reports/{workId}", async (string workId, IFileAnalysisService service) =>
{
    IEnumerable<ReportDto> reports = await service.GetReportsByWorkAsync(workId);
    return Results.Ok(reports);
});

app.MapGet("/api/reports/{reportId}/wordcloud", async (Guid reportId, IFileAnalysisService service) =>
{
    try
    {
        string url = await service.GenerateWordCloudAsync(reportId);
        return Results.Ok(new { url = url, reportId = reportId });
    }
    catch (KeyNotFoundException keyEx)
    {
        return Results.NotFound(new { error = $"Отчет с ID {reportId} не найден", details = keyEx.Message });
    }
    catch (InvalidOperationException invOpEx)
    {
        return Results.BadRequest(new { error = "Неверная операция", details = invOpEx.Message });
    }
    catch (HttpRequestException httpEx)
    {
        return Results.Problem($"Ошибка сети при генерации облака слов: {httpEx.Message}");
    }
    catch (ArgumentException argEx)
    {
        return Results.BadRequest(new { error = "Неверные параметры запроса", details = argEx.Message });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "Работает", время = DateTime.UtcNow }));

app.Run();