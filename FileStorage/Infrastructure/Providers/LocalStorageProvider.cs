using Microsoft.AspNetCore.Http;

namespace FileStorage.Infrastructure.Providers;

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _root;

    public LocalStorageProvider(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file, Guid fileId)
    {
        string path = Path.Combine(_root, fileId.ToString());
        await using FileStream stream = new FileStream(path, FileMode.CreateNew);
        await file.CopyToAsync(stream);
        return path;
    }
}