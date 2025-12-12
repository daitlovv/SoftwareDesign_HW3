using FileStorage.Domain.Entities;

namespace FileStorage.Infrastructure.Repositories;

public interface IFileRepository
{
    Task AddAsync(StoredFile entity);
    Task<StoredFile?> GetAsync(Guid fileId);
}