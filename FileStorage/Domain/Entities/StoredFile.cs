using System.ComponentModel.DataAnnotations;

namespace FileStorage.Domain.Entities;

public class StoredFile
{
    [Key]
    public Guid FileId { get; set; }
    public string OriginalName { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string Checksum { get; set; } = null!;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
}