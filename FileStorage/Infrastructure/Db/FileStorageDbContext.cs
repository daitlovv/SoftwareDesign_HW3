using Microsoft.EntityFrameworkCore;
using FileStorage.Domain.Entities;

namespace FileStorage.Infrastructure.Db;

public class FileStorageDbContext : DbContext
{
    public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options) : base(options) { }

    public DbSet<StoredFile> StoredFiles { get; set; }
}