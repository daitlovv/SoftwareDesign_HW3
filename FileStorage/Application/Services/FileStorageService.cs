using System.Security.Cryptography;
using FileStorage.Application.DTOs;
using FileStorage.Application.Interfaces;
using FileStorage.Domain.Entities;
using FileStorage.Infrastructure.Providers;
using FileStorage.Infrastructure.Repositories;

namespace FileStorage.Application.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IStorageProvider storageProvider;
    private readonly IFileRepository repository;

    public FileStorageService(IStorageProvider storageProvider, IFileRepository repository)
    {
        this.storageProvider = storageProvider;
        this.repository = repository;
    }

    public async Task<FileDto> SaveAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Файл пустой или не был передан");
        }

        Guid fileId = Guid.NewGuid();
        string storagePath = await storageProvider.SaveAsync(file, fileId);
        
        string checksum;
        
        await using (FileStream fileStream = File.OpenRead(storagePath))
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = await sha256.ComputeHashAsync(fileStream);
            checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        StoredFile entity = new StoredFile
        {
            FileId = fileId,
            OriginalName = file.FileName,
            StoragePath = storagePath,
            Checksum = checksum,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        await repository.AddAsync(entity);

        return new FileDto(
            entity.FileId, 
            entity.OriginalName, 
            entity.StoragePath, 
            entity.Checksum, 
            entity.Size, 
            entity.UploadedAt);
    }

    public async Task<FileDto?> GetFileInfoAsync(Guid fileId)
    {
        StoredFile? entity = await repository.GetAsync(fileId);
        
        if (entity == null) 
        {
            return null;
        }

        return new FileDto(
            entity.FileId, 
            entity.OriginalName, 
            entity.StoragePath, 
            entity.Checksum, 
            entity.Size, 
            entity.UploadedAt);
    }

    public async Task<string> GetFileTextAsync(Guid fileId)
    {
        StoredFile? entity = await repository.GetAsync(fileId);
        
        if (entity == null)
        {
            throw new FileNotFoundException($"Файл с ID {fileId} не найден");
        }

        if (!File.Exists(entity.StoragePath))
        {
            throw new FileNotFoundException($"Физический файл не найден по пути: {entity.StoragePath}");
        }

        return await File.ReadAllTextAsync(entity.StoragePath);
    }
}