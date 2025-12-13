using FileAnalysis.Domain.Entities;
using FileAnalysis.Infrastructure.Repositories;

namespace FileAnalysis.Application.Services;

public class WordCloudGenerator : IWordCloudGenerator
{
    private readonly IReportRepository reportRepository;
    private readonly IFileContentProvider fileContentProvider;
    private readonly ILogger<WordCloudGenerator> logger;

    public WordCloudGenerator(
        IReportRepository reportRepository,
        IFileContentProvider fileContentProvider,
        ILogger<WordCloudGenerator> logger)
    {
        this.reportRepository = reportRepository;
        this.fileContentProvider = fileContentProvider;
        this.logger = logger;
    }

    public async Task<string> GenerateWordCloudAsync(Guid reportId)
    {
        try
        {
            var report = await reportRepository.GetByIdAsync(reportId);
            
            if (report == null)
            {
                throw new KeyNotFoundException($"Отчет с ID {reportId} не найден");
            }

            string fileContent = await fileContentProvider.GetFileContentAsync(report.FileId);
            
            if (string.IsNullOrEmpty(fileContent))
            {
                return GenerateWordCloudUrl("Содержимое+файла+пустое");
            }
            
            string processedText = ProcessTextForWordCloud(fileContent);
            return GenerateWordCloudUrl(processedText);
        }
        catch (KeyNotFoundException keyEx)
        {
            logger.LogError(keyEx, "Отчет не найден для генерации облака слов {ReportId}", reportId);
            return GenerateWordCloudUrl("Отчет+не+найден");
        }
        catch (InvalidOperationException invOpEx)
        {
            logger.LogError(invOpEx, "Ошибка операции при генерации облака слов для отчета {ReportId}", reportId);
            return GenerateWordCloudUrl($"Ошибка+операции");
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogError(httpEx, "Ошибка HTTP при генерации облака слов для отчета {ReportId}", reportId);
            return GenerateWordCloudUrl($"Ошибка+сети");
        }
        catch (ArgumentException argEx)
        {
            logger.LogError(argEx, "Неверные аргументы при генерации облака слов для отчета {ReportId}", reportId);
            return GenerateWordCloudUrl($"Неверные+данные");
        }
    }

    public string GenerateWordCloudUrl(string text)
    {
        return $"https://quickchart.io/wordcloud?text={text}&width=600&height=400&backgroundColor=white&fontFamily=Arial";
    }

    private string ProcessTextForWordCloud(string text)
    {
        char[] separators = new[] { ' ', '\n', '\r', '\t', '.', ',', ';', '!', '?', ':', '(', ')', '[', ']', '{', '}', '"', '\'', '-', '_' };
        HashSet<string> stopWords = new HashSet<string> { "the", "and", "is", "in", "to", "of", "a", "for", "on", "with", "as", "by", "at" };
        
        Dictionary<string, int> wordCounts = text
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .Where(word => word.Length > 2 && !stopWords.Contains(word) && !word.Any(character => char.IsDigit(character)))
            .GroupBy(word => word)
            .ToDictionary(group => group.Key, group => group.Count());

        if (!wordCounts.Any())
        {
            return "Не+найдено+слов";
        }

        List<KeyValuePair<string, int>> topWords = wordCounts
            .OrderByDescending(pair => pair.Value)
            .Take(30)
            .ToList();
        
        List<string> cloudText = new List<string>();
        
        foreach (KeyValuePair<string, int> word in topWords)
        {
            int repetitions = Math.Min(word.Value, 5);
            
            for (int i = 0; i < repetitions; i++)
            {
                cloudText.Add(word.Key);
            }
        }
        
        return Uri.EscapeDataString(string.Join(" ", cloudText));
    }
}