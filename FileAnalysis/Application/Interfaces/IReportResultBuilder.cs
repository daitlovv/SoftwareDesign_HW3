using FileAnalysis.Application.DTOs;
using FileAnalysis.Domain.Entities;

namespace FileAnalysis.Application.Services;

public interface IReportResultBuilder
{
    string BuildResultText(bool hasPlagiarism, List<string> similarStudents, double maxSimilarity, int totalReports);
    ReportDto BuildReportDto(Report report, string resultText, double similarity);
    ReportDto BuildErrorReportDto(Exception ex, Guid fileId, string workId, string studentName);
}