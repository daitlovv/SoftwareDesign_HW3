using FileAnalysis.Application;
using FileAnalysis.Application.Interfaces;
using FileAnalysis.Application.Services;
using FileAnalysis.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileAnalysis.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileAnalysisServices(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<FileAnalysisDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddHttpClient<IFileContentProvider, HttpClientFileContentProvider>((provider, client) =>
            {
                client.BaseAddress = new Uri("http://file-storage:80");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddScoped<IReportRepository, ReportRepository>();

            services.AddScoped<ITextAnalyzer, TextAnalyzer>();
            services.AddScoped<IWordCloudGenerator, WordCloudGenerator>();
            services.AddScoped<IReportResultBuilder, ReportResultBuilder>();
            services.AddScoped<IFileAnalysisService, FileAnalysisService>();

            return services;
        }
    }
}