using FileAnalysis.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileAnalysis.Application.Services
{
    public class TextProcessor : ITextProcessor
    {
        private readonly ILogger<TextProcessor> logger;

        public TextProcessor(ILogger<TextProcessor> logger)
        {
            this.logger = logger;
        }

        public List<string> ExtractWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            char[] separators = GetSeparators();
            string cleanedText = CleanText(text);
            
            return cleanedText
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.ToLowerInvariant())
                .Where(word => IsValidWord(word))
                .Distinct()
                .ToList();
        }

        public Dictionary<string, int> GetWordFrequencies(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new Dictionary<string, int>();
            }

            char[] separators = GetSeparators();
            string cleanedText = CleanText(text);
            
            return cleanedText
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.ToLowerInvariant())
                .Where(word => IsValidWord(word))
                .GroupBy(word => word)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        public string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string cleaned = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", string.Empty);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^\p{L}\p{N}\s-']", " ");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
            
            return cleaned.Trim();
        }

        private static char[] GetSeparators()
        {
            return new[] 
            { 
                ' ', '\n', '\r', '\t', '.', ',', ';', '!', '?', ':',
                '(', ')', '[', ']', '{', '}', '"', '\'', '-', '_'
            };
        }

        private static bool IsValidWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;
            
            if (word.Length < 3)
                return false;
            
            if (word.All(char.IsDigit))
                return false;
            
            return true;
        }
    }
}