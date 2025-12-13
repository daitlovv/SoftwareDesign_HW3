using Gateway.Application.DTOs;

namespace Gateway.Infrastructure.HttpClients;

public interface IFileAnalysisClient
{
    Task<ReportDto> AnalyzeFileAsync(Guid fileId, string workId, string studentName);
    Task<IEnumerable<ReportDto>> GetReportsAsync(string workId);
}