using FileAnalysis.Application.DTOs;
using FileAnalysis.Domain.Entities;

namespace FileAnalysis.Application.Services;

public class ReportResultBuilder : IReportResultBuilder
{
    public string BuildResultText(bool hasPlagiarism, List<string> similarStudents, double maxSimilarity, int totalReports)
    {
        if (hasPlagiarism && similarStudents.Any())
        {
            string sources = string.Join(", ", similarStudents);
            return $"Обнаружен плагиат. Сходство с работами: {sources}";
        }
        
        if (totalReports == 1)
        {
            return "Первая сдача по этой работе. Плагиат не обнаружен.";
        }
        
        return maxSimilarity > 0 
            ? $"Сходство с другими работами: {maxSimilarity:F3}% (без плагиата)"
            : "Плагиат не обнаружен";
    }

    public ReportDto BuildReportDto(Report report, string resultText, double similarity)
    {
        return new ReportDto(
            Id: report.ReportId,
            FileId: report.FileId,
            WorkId: report.WorkId,
            StudentName: report.StudentName,
            Plagiarism: report.HasPlagiarism,
            Similarity: similarity,
            Result: resultText,
            CreatedAt: report.CreatedAt
        );
    }

    public ReportDto BuildErrorReportDto(Exception ex, Guid fileId, string workId, string studentName)
    {
        Report errorReport = new Report
        {
            ReportId = Guid.NewGuid(),
            WorkId = workId,
            FileId = fileId,
            StudentName = studentName,
            Result = $"Ошибка анализа: {ex.Message}",
            HasPlagiarism = false,
            Similarity = 0,
            CreatedAt = DateTime.UtcNow
        };

        return new ReportDto(
            Id: errorReport.ReportId,
            FileId: errorReport.FileId,
            WorkId: errorReport.WorkId,
            StudentName: errorReport.StudentName,
            Plagiarism: errorReport.HasPlagiarism,
            Similarity: errorReport.Similarity,
            Result: errorReport.Result,
            CreatedAt: errorReport.CreatedAt
        );
    }
}