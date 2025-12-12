namespace FileAnalysis.Application.Services;

public interface ITextAnalyzer
{
    Task<(bool hasPlagiarism, double similarity, List<string> similarStudents)> 
        AnalyzeForPlagiarismAsync(string currentContent, string workId, Guid currentFileId, string currentStudent);
    
    List<string> ExtractWords(string text);
    double CalculateSimilarity(List<string> words1, List<string> words2);
}