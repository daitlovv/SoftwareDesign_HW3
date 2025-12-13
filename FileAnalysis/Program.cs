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
    FileAnalysisDbContext db = scope.ServiceProvider.GetRequiredService<FileAnalysisDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "File Analysis API v1");
    options.RoutePrefix = "swagger";
});

app.MapPost("/api/analyze", async (AnalyzeRequestDto request, IFileAnalysisService service) =>
{
    ReportDto report = await service.AnalyzeFileAsync(request.FileId, request.WorkId, request.StudentName);
    return Results.Ok(report);
});

app.MapGet("/api/reports/{workId}", async (string workId, IFileAnalysisService service) =>
{
    System.Collections.Generic.IEnumerable<ReportDto> reports = await service.GetReportsByWorkAsync(workId);
    return Results.Ok(reports);
});

app.MapGet("/api/reports/{reportId}/wordcloud", async (Guid reportId, IFileAnalysisService service) =>
{
    try
    {
        string url = await service.GenerateWordCloudAsync(reportId);
        return Results.Ok(new { url = url, reportId = reportId });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run("http://0.0.0.0:80");