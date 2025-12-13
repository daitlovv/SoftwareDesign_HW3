namespace FileAnalysis.Application.Services;

public interface IWordCloudGenerator
{
    Task<string> GenerateWordCloudAsync(Guid reportId);
    string GenerateWordCloudUrl(string text);
}