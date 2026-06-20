namespace CineAI.API.Models;

public class AnalysisResult
{
    public int Id { get; set; }
    public string InputText { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}