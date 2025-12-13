namespace FileAnalysis.Application.Services.Interfaces
{
    public interface ISimilarityCalculator
    {
        double CalculateSimilarity(List<string> words1, List<string> words2);
    }
}