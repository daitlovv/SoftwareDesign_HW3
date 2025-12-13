namespace FileAnalysis.Application.DTOs;

public record AnalyzeRequestDto(
    Guid FileId,
    string WorkId,
    string StudentName
);