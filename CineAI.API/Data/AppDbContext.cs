using CineAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CineAI.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
}