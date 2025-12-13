using FileAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysis.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly FileAnalysisDbContext dbContext;

    public ReportRepository(FileAnalysisDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(Report report)
    {
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Report?> GetByIdAsync(Guid reportId)
    {
        return await dbContext.Reports.AsNoTracking()
            .FirstOrDefaultAsync(report => report.ReportId == reportId);
    }

    public async Task<IEnumerable<Report>> GetByWorkIdAsync(string workId)
    {
        return await dbContext.Reports.AsNoTracking()
            .Where(report => report.WorkId == workId)
            .ToListAsync();
    }
    
    public async Task UpdateAsync(Report report)
    {
        dbContext.Reports.Update(report);
        await dbContext.SaveChangesAsync();
    }
}