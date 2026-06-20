using CineAI.API.Data;
using CineAI.API.Models;
using CineAI.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly HuggingFaceService _huggingFace;
    private readonly AppDbContext _db;

    public AnalysisController(HuggingFaceService huggingFace, AppDbContext db)
    {
        _huggingFace = huggingFace;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Текст не может быть пустым.");

        var (label, confidence) = await _huggingFace.ClassifyAsync(request.Text);

        var result = new AnalysisResult
        {
            InputText = request.Text,
            Label = label,
            Confidence = confidence
        };

        _db.AnalysisResults.Add(result);
        await _db.SaveChangesAsync();

        return Ok(new { label, confidence });
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var results = await _db.AnalysisResults
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Ok(results);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearHistory()
    {
        _db.AnalysisResults.RemoveRange(_db.AnalysisResults);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _db.AnalysisResults
            .GroupBy(r => r.Label)
            .Select(g => new { genre = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToListAsync();

        return Ok(stats);
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchByTitle([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Название фильма не может быть пустым.");

        var (label, confidence) = await _huggingFace.SearchByTitleAsync(request.Title);

        var result = new AnalysisResult
        {
            InputText = request.Title,
            Label = label,
            Confidence = confidence
        };

        _db.AnalysisResults.Add(result);
        await _db.SaveChangesAsync();

        return Ok(new { label, confidence });
    }

    [HttpPost("recommend")]
    public async Task<IActionResult> Recommend([FromBody] RecommendRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Genre))
            return BadRequest("Жанр не может быть пустым.");

        var movies = await _huggingFace.GetTopMoviesAsync(request.Genre);
        return Ok(movies);
    }
}

public record AnalyzeRequest(string Text);
public record SearchRequest(string Title);
public record RecommendRequest(string Genre);
