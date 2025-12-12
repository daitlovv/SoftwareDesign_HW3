namespace FileAnalysis.Application.Services;

public interface IFileContentProvider
{
    Task<string> GetFileContentAsync(Guid fileId);
}