
namespace FileAnalysis.Application.Services.Interfaces;
public interface ITextAnalyzer
{
    Task<(bool hasPlagiarism, double similarity, List<string> similarStudents)> 
        AnalyzeForPlagiarismAsync(
            string currentContent, 
            string workId, 
            Guid currentFileId, 
            string currentStudent);
}