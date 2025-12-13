namespace Gateway.Application.DTOs;

public record FileDto(
    Guid FileId,
    string OriginalName,
    string StoragePath,
    string Checksum,
    long Size,
    DateTime UploadedAt
);