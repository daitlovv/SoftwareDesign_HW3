namespace FileStorage.Infrastructure.Providers;

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _root;

    public LocalStorageProvider(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "files");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file, Guid fileId)
    {
        var path = Path.Combine(_root, fileId.ToString());
        await using var fs = File.Create(path);
        await file.CopyToAsync(fs);
        return path;
    }
}