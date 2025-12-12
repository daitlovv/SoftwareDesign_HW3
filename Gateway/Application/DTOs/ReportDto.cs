namespace Gateway.Application.DTOs;

public record ReportDto(
    Guid Id,
    Guid FileId,
    string WorkId,
    string StudentName,
    bool Plagiarism,
    double Similarity,
    string Result,
    DateTime CreatedAt
);