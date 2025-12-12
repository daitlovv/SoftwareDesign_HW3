namespace Gateway.Application.DTOs;

public record AnalysisResult(
    Guid Id,
    Guid FileId,
    string WorkId,
    string StudentName,
    bool Plagiarism,
    double Similarity,
    string Result,
    DateTime CreatedAt
);