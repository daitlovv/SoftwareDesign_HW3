using FileAnalysis.Domain.Entities;
using FileAnalysis.Infrastructure.Repositories;

namespace FileAnalysis.Application.Services;

public class TextAnalyzer : ITextAnalyzer
{
    private readonly IReportRepository reportRepository;
    private readonly IFileContentProvider fileContentProvider;
    private readonly ILogger<TextAnalyzer> logger;

    public TextAnalyzer(
        IReportRepository reportRepository,
        IFileContentProvider fileContentProvider,
        ILogger<TextAnalyzer> logger)
    {
        this.reportRepository = reportRepository;
        this.fileContentProvider = fileContentProvider;
        this.logger = logger;
    }

    public async Task<(bool hasPlagiarism, double similarity, List<string> similarStudents)> 
        AnalyzeForPlagiarismAsync(string currentContent, string workId, Guid currentFileId, string currentStudent)
    {
        IEnumerable<Report> previousReports = await reportRepository.GetByWorkIdAsync(workId);
        
        if (!previousReports.Any(report => report.FileId != currentFileId))
        {
            return (false, 0, new List<string>());
        }

        List<string> currentWords = ExtractWords(currentContent);
        
        if (!currentWords.Any())
        {
            return (false, 0, new List<string>());
        }

        double maxSimilarity = 0.0;
        bool hasPlagiarism = false;
        List<string> similarStudents = new List<string>();

        foreach (Report previousReport in previousReports.Where(report => report.FileId != currentFileId))
        {
            try
            {
                string previousContent = await fileContentProvider.GetFileContentAsync(previousReport.FileId);
                List<string> previousWords = ExtractWords(previousContent);
                
                if (!previousWords.Any()) continue;

                double similarity = CalculateSimilarity(currentWords, previousWords);
                
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                }
                
                if (similarity > 70)
                {
                    hasPlagiarism = true;
                    similarStudents.Add($"{previousReport.StudentName} ({similarity:F1}%)");
                }
            }
            catch (InvalidOperationException invOpEx)
            {
                logger.LogWarning(invOpEx, "Ошибка операции при анализе отчета {ReportId}", previousReport.ReportId);
                continue;
            }
            catch (ArgumentException argEx)
            {
                logger.LogWarning(argEx, "Неверные аргументы при анализе отчета {ReportId}", previousReport.ReportId);
                continue;
            }
        }

        return (hasPlagiarism, maxSimilarity, similarStudents);
    }

    public List<string> ExtractWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        char[] separators = new[] { ' ', '\n', '\r', '\t', '.', ',', ';', '!', '?', ':', '(', ')', '[', ']', '{', '}', '"', '\'' };
        HashSet<string> stopWords = new HashSet<string> { "the", "and", "is", "in", "to", "of", "a", "for", "on", "with", "as", "by", "at" };
        
        return text
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length > 3 && !stopWords.Contains(word))
            .Distinct()
            .ToList();
    }

    public double CalculateSimilarity(List<string> words1, List<string> words2)
    {
        if (!words1.Any() || !words2.Any())
            return 0;

        int commonWords = words1.Intersect(words2).Count();
        int totalWords = Math.Max(words1.Count, words2.Count);
        
        return (double)commonWords / totalWords * 100;
    }
}