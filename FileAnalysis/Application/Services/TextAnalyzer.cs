using FileAnalysis.Application.Services.Interfaces;
using FileAnalysis.Domain.Entities;
using FileAnalysis.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace FileAnalysis.Application.Services;

public class TextAnalyzer : ITextAnalyzer
{
    private readonly IReportRepository reportRepository;
    private readonly IFileContentProvider fileContentProvider;
    private readonly ITextProcessor textProcessor;
    private readonly ISimilarityCalculator similarityCalculator;
    private readonly ILogger<TextAnalyzer> logger;

    public TextAnalyzer(
        IReportRepository reportRepository,
        IFileContentProvider fileContentProvider,
        ITextProcessor textProcessor,
        ISimilarityCalculator similarityCalculator,
        ILogger<TextAnalyzer> logger)
    {
        this.reportRepository = reportRepository;
        this.fileContentProvider = fileContentProvider;
        this.textProcessor = textProcessor;
        this.similarityCalculator = similarityCalculator;
        this.logger = logger;
    }

    public async Task<(bool hasPlagiarism, double similarity, List<string> similarStudents)>
        AnalyzeForPlagiarismAsync(
            string currentContent,
            string workId,
            Guid currentFileId,
            string currentStudent)
    {
        IEnumerable<Report> previousReports = await reportRepository.GetByWorkIdAsync(workId);

        if (!previousReports.Any(report => report.FileId != currentFileId))
        {
            return (false, 0, new List<string>());
        }

        List<string> currentWords = textProcessor.ExtractWords(currentContent);

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
                List<string> previousWords = textProcessor.ExtractWords(previousContent);

                if (!previousWords.Any()) continue;

                double similarity = similarityCalculator.CalculateSimilarity(currentWords, previousWords);

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
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Ошибка при обновлении предыдущих отчетов для работы {WorkId}", workId);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Неверные аргументы при обновлении отчетов для работы {WorkId}", workId);
            }
        }

        return (hasPlagiarism, maxSimilarity, similarStudents);
    }
}