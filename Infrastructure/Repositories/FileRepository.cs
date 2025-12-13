using FileStorage.Application.Interfaces;
using FileStorage.Domain.Entities;
using FileStorage.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace FileStorage.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly FileStorageDbContext dbContext;

    public FileRepository(FileStorageDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IEnumerable<StoredFile>> GetAllAsync()
    {
        return await dbContext.StoredFiles.AsNoTracking().ToListAsync();
    }

    public async Task<StoredFile?> GetAsync(Guid fileId)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        return await dbContext.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(file => file.FileId == fileId);
    }

    public async Task AddAsync(StoredFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (file.FileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        await dbContext.StoredFiles.AddAsync(file);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(StoredFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (file.FileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        dbContext.StoredFiles.Update(file);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid fileId)
    {
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        StoredFile? file = await dbContext.StoredFiles.FindAsync(fileId);
        
        if (file != null)
        {
            dbContext.StoredFiles.Remove(file);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            throw new KeyNotFoundException($"Файл с ID {fileId} не найден");
        }
    }
}