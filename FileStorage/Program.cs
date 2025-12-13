using FileStorage.Infrastructure.Db;
using FileStorage.Infrastructure.Extensions;
using FileStorage.Presentation.Endpoints;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "File Storage API",
        Version = "v1",
        Description = "Микросервис для хранения файлов"
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    FileStorageDbContext db = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "File Storage API v1");
    options.RoutePrefix = "swagger";
});

app.MapFilesEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run("http://0.0.0.0:80");