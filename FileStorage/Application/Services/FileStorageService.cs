using System.Security.Cryptography;
using FileStorage.Application.DTOs;
using FileStorage.Application.Interfaces;
using FileStorage.Domain.Entities;
using FileStorage.Infrastructure.Providers;
using FileStorage.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;

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

        if (file.Length > 50 * 1024 * 1024) // 50MB лимит
        {
            throw new ArgumentException("Размер файла превышает допустимый лимит 50MB");
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
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

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
        if (fileId == Guid.Empty)
        {
            throw new ArgumentException("Неверный идентификатор файла");
        }

        StoredFile? entity = await repository.GetAsync(fileId);
        
        if (entity == null)
        {
            throw new FileNotFoundException($"Файл с ID {fileId} не найден в репозитории");
        }

        if (!File.Exists(entity.StoragePath))
        {
            throw new FileNotFoundException($"Физический файл не найден по пути: {entity.StoragePath}");
        }

        try
        {
            string fileContent = await File.ReadAllTextAsync(entity.StoragePath);
            
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                throw new InvalidOperationException("Файл пустой");
            }
            
            return fileContent;
        }
        catch (UnauthorizedAccessException authEx)
        {
            throw new UnauthorizedAccessException($"Нет доступа к файлу {entity.StoragePath}", authEx);
        }
        catch (IOException ioEx)
        {
            throw new IOException($"Ошибка ввода-вывода при чтении файла {entity.StoragePath}", ioEx);
        }
        catch (ArgumentException argEx)
        {
            throw new ArgumentException($"Неверные параметры для чтения файла", argEx);
        }
    }
}