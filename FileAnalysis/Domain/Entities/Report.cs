using System.ComponentModel.DataAnnotations;

namespace FileAnalysis.Domain.Entities;

public class Report
{
    [Key]
    public Guid ReportId { get; set; }
    public string WorkId { get; set; } = null!; 
    public Guid FileId { get; set; }
    public string StudentName { get; set; } = null!;
    public string Result { get; set; } = null!;
    public bool HasPlagiarism { get; set; }
    public double Similarity { get; set; }
    public DateTime CreatedAt { get; set; }
}