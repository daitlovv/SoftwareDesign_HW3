using FileStorage.Application.Interfaces;
using FileStorage.Application.Services;
using FileStorage.Infrastructure.Db;
using FileStorage.Infrastructure.Providers;
using FileStorage.Infrastructure.Repositories;
using FileStorage.Presentation.Endpoints;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API хранилища файлов",
        Version = "v1",
        Description = "Микросервис для хранения и управления файлами"
    });
    
    options.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

string sqliteConnection = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=filestorage.db";
builder.Services.AddDbContext<FileStorageDbContext>(options =>
    options.UseSqlite(sqliteConnection));

builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    FileStorageDbContext dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API хранилища файлов v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapFilesEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "Работает", время = DateTime.UtcNow }));

app.Run();