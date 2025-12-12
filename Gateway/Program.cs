using Gateway.Application;
using Gateway.Application.DTOs;
using Gateway.Application.Services;
using Gateway.Infrastructure.HttpClients;
using Gateway.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Gateway API",
        Version = "v1",
        Description = "Основной gateway для загрузки работ и проверки плагиата"
    });
});

builder.Services.AddHttpClient<IFileStorageClient, FileStorageClient>(client =>
{
    string fileStorageUrl = builder.Configuration["FILESTORAGE_URL"] ?? "http://file-storage:80";
    client.BaseAddress = new Uri(fileStorageUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IFileAnalysisClient, FileAnalysisClient>(client =>
{
    string fileAnalysisUrl = builder.Configuration["FILEANALYSIS_URL"] ?? "http://file-analysis:80";
    client.BaseAddress = new Uri(fileAnalysisUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<GatewayService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGatewayEndpoints();

app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "Working", 
    time = DateTime.UtcNow,
    services = new 
    {
        fileStorage = builder.Configuration["FILESTORAGE_URL"] ?? "http://file-storage:80",
        fileAnalysis = builder.Configuration["FILEANALYSIS_URL"] ?? "http://file-analysis:80"
    }
}));

app.Run("http://0.0.0.0:80");