using FileStorage.Application.DTOs;

namespace FileStorage.Application.Interfaces;

public interface IFileStorageService
{
    Task<FileDto> SaveAsync(IFormFile file);
    Task<FileDto?> GetFileInfoAsync(Guid fileId);
    Task<string> GetFileTextAsync(Guid fileId);
}