using FileAnalysis.Domain.Entities;

namespace FileAnalysis.Infrastructure.Repositories;

public interface IReportRepository
{
    Task AddAsync(Report report);
    Task<Report?> GetByIdAsync(Guid reportId);
    Task<IEnumerable<Report>> GetByWorkIdAsync(string workId);
    Task UpdateAsync(Report report);
}