namespace FileStorage.Infrastructure.Providers;

public interface IStorageProvider
{
    Task<string> SaveAsync(IFormFile file, Guid fileId);
}