using FileStorage.Application.Interfaces;
using FileStorage.Application.Services;
using FileStorage.Infrastructure.Db;
using FileStorage.Infrastructure.Providers;
using FileStorage.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

namespace FileStorage.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Sqlite") ?? "Data Source=filestorage.db";
        
        services.AddDbContext<FileStorageDbContext>(options => 
            options.UseSqlite(connectionString));

        services.AddScoped<IFileRepository, FileRepository>();
        services.AddSingleton<IStorageProvider, LocalStorageProvider>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFileStorageService, FileStorageService>();
        return services;
    }
}