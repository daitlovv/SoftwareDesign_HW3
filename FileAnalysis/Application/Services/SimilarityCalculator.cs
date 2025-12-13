using FileAnalysis.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileAnalysis.Application.Services;

public class SimilarityCalculator : ISimilarityCalculator
{
    private readonly ILogger<SimilarityCalculator> logger;

    public SimilarityCalculator(ILogger<SimilarityCalculator> logger)
    {
        this.logger = logger;
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