using FileAnalysis.Application.DTOs;

namespace FileAnalysis.Application.Interfaces;

public interface IFileAnalysisService
{
    Task<ReportDto> AnalyzeFileAsync(Guid fileId, string workId, string studentName);
    Task<IEnumerable<ReportDto>> GetReportsByWorkAsync(string workId);
    Task<string> GenerateWordCloudAsync(Guid reportId);
}