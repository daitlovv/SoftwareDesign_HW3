namespace FileAnalysis.Application.Services.Interfaces
{
    public interface ITextProcessor
    {
        List<string> ExtractWords(string text);
    }
}