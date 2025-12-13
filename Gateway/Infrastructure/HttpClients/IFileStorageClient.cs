namespace Gateway.Infrastructure.HttpClients;

public interface IFileStorageClient
{
    Task<Guid> UploadFileAsync(IFormFile file);
    Task<string> GetFileTextAsync(Guid fileId);
}