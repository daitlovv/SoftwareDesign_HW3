using FileAnalysis.Application.DTOs;
using FileAnalysis.Application.Interfaces;
using FileAnalysis.Application.Services;
using FileAnalysis.Domain.Entities;
using FileAnalysis.Infrastructure.Repositories;

namespace FileAnalysis.Application;

public class FileAnalysisService : IFileAnalysisService
{
    private readonly IReportRepository reportRepository;
    private readonly ITextAnalyzer textAnalyzer;
    private readonly IWordCloudGenerator wordCloudGenerator;
    private readonly IReportResultBuilder resultBuilder;
    private readonly IFileContentProvider fileContentProvider;
    private readonly ILogger<FileAnalysisService> logger;

    public FileAnalysisService(
        IReportRepository reportRepository,
        ITextAnalyzer textAnalyzer,
        IWordCloudGenerator wordCloudGenerator,
        IReportResultBuilder resultBuilder,
        IFileContentProvider fileContentProvider,
        ILogger<FileAnalysisService> logger)
    {
        this.reportRepository = reportRepository;
        this.textAnalyzer = textAnalyzer;
        this.wordCloudGenerator = wordCloudGenerator;
        this.resultBuilder = resultBuilder;
        this.fileContentProvider = fileContentProvider;
        this.logger = logger;
    }

    public async Task<ReportDto> AnalyzeFileAsync(Guid fileId, string workId, string studentName)
    {
        try
        {
            logger.LogInformation("Анализ файла: {FileId}, работа: {WorkId}, студент: {StudentName}", 
                fileId, workId, studentName);

            string fileContent = await fileContentProvider.GetFileContentAsync(fileId);
            
            (bool hasPlagiarism, double similarity, List<string> similarStudents) = 
                await textAnalyzer.AnalyzeForPlagiarismAsync(fileContent, workId, fileId, studentName);
            
            Report report = new Report
            {
                ReportId = Guid.NewGuid(),
                WorkId = workId,
                FileId = fileId,
                StudentName = studentName,
                Result = "",
                HasPlagiarism = hasPlagiarism,
                Similarity = similarity,
                CreatedAt = DateTime.UtcNow
            };

            await reportRepository.AddAsync(report);
            
            await UpdateSimilarPreviousReportsAsync(workId, fileId, studentName, fileContent);
            
            return resultBuilder.BuildReportDto(report, "Анализ выполнен", similarity);
        }
        catch (IOException ioEx)
        {
            logger.LogError(ioEx, "Ошибка ввода-вывода при анализе файла {FileId}", fileId);
            throw new InvalidOperationException($"Ошибка доступа к файлу: {ioEx.Message}", ioEx);
        }
        catch (UnauthorizedAccessException authEx)
        {
            logger.LogError(authEx, "Нет доступа к файлу {FileId}", fileId);
            throw new InvalidOperationException($"Нет доступа к файлу: {authEx.Message}", authEx);
        }
        catch (ArgumentNullException argEx)
        {
            logger.LogError(argEx, "Отсутствуют обязательные параметры для файла {FileId}", fileId);
            throw new ArgumentException($"Обязательные параметры не указаны: {argEx.Message}", argEx);
        }
    }

    private async Task UpdateSimilarPreviousReportsAsync(
    string workId, 
    Guid currentFileId, 
    string currentStudent,
    string currentContent)
    {
        try
        {
            IEnumerable<Report> previousReports = await reportRepository.GetByWorkIdAsync(workId);
            List<string> currentWords = textAnalyzer.ExtractWords(currentContent);
            
            if (!currentWords.Any()) return;

            foreach (Report previousReport in previousReports.Where(report => report.FileId != currentFileId))
            {
                try
                {
                    string previousContent = await fileContentProvider.GetFileContentAsync(previousReport.FileId);
                    List<string> previousWords = textAnalyzer.ExtractWords(previousContent);
                    
                    if (!previousWords.Any()) continue;

                    double similarity = textAnalyzer.CalculateSimilarity(currentWords, previousWords);

                    if (similarity > 70 && !previousReport.HasPlagiarism)
                    {
                        previousReport.HasPlagiarism = true;
                        previousReport.Similarity = Math.Max(previousReport.Similarity, similarity);
                        previousReport.Result = $"Обнаружен взаимный плагиат с работой студента {currentStudent}";
                        
                        await reportRepository.UpdateAsync(previousReport);
                        logger.LogInformation("Updated report {ReportId} as plagiarism", previousReport.ReportId);
                    }
                }
                catch (IOException ioEx)
                {
                    logger.LogWarning(ioEx, "Error updating report {ReportId}", previousReport.ReportId);
                    continue;
                }
                catch (UnauthorizedAccessException authEx)
                {
                    logger.LogWarning(authEx, "Error updating report {ReportId}", previousReport.ReportId);
                    continue;
                }
            }
        }
        catch (InvalidOperationException opEx)
        {
            logger.LogError(opEx, "Error in UpdateSimilarPreviousReportsAsync");
        }
        catch (ArgumentException argEx)
        {
            logger.LogError(argEx, "Error in UpdateSimilarPreviousReportsAsync");
        }
    }

    public async Task<IEnumerable<ReportDto>> GetReportsByWorkAsync(string workId)
    {
        List<Report> reports = (await reportRepository.GetByWorkIdAsync(workId)).ToList();
        List<ReportDto> result = new List<ReportDto>();
        
        foreach (Report report in reports)
        {
            try
            {
                string fileContent = await fileContentProvider.GetFileContentAsync(report.FileId);
                
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    result.Add(resultBuilder.BuildReportDto(report, "Файл пустой", 0));
                    continue;
                }
                
                (bool hasPlagiarism, double similarity, List<string> similarStudents) = 
                    await textAnalyzer.AnalyzeForPlagiarismAsync(
                        fileContent, workId, report.FileId, report.StudentName);
                
                string resultText = resultBuilder.BuildResultText(
                    hasPlagiarism, similarStudents, similarity, reports.Count);
                
                result.Add(resultBuilder.BuildReportDto(report, resultText, similarity));
            }
            catch (IOException ioEx)
            {
                logger.LogError(ioEx, "Ошибка ввода-вывода при обработке отчета {ReportId}", report.ReportId);
                result.Add(resultBuilder.BuildErrorReportDto(
                    new InvalidOperationException("Ошибка доступа к файлу отчета", ioEx), 
                    report.FileId, report.WorkId, report.StudentName));
            }
            catch (UnauthorizedAccessException authEx)
            {
                logger.LogError(authEx, "Нет доступа к файлу отчета {ReportId}", report.ReportId);
                result.Add(resultBuilder.BuildErrorReportDto(
                    new InvalidOperationException("Нет доступа к файлу отчета", authEx), 
                    report.FileId, report.WorkId, report.StudentName));
            }
        }
        
        return result.OrderBy(report => report.CreatedAt).ToList();
    }

    public async Task<string> GenerateWordCloudAsync(Guid reportId)
    {
        return await wordCloudGenerator.GenerateWordCloudAsync(reportId);
    }
}
