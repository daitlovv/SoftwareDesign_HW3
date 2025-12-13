using FileAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileAnalysis.Infrastructure;

public class FileAnalysisDbContext : DbContext
{
    public FileAnalysisDbContext(DbContextOptions<FileAnalysisDbContext> options) : base(options) { }

    public DbSet<Report> Reports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId);
            
            entity.Property(e => e.WorkId)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.StudentName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.FileId)
                .IsRequired();
                
            entity.Property(e => e.Result)
                .IsRequired()
                .HasMaxLength(1000);
                
            entity.Property(e => e.HasPlagiarism)
                .IsRequired();
                
            entity.Property(e => e.Similarity)
                .IsRequired();
                
            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });
    }
}