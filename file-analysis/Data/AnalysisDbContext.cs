using Microsoft.EntityFrameworkCore;

namespace FileAnalysis.Data
{
    public class AnalysisDbContext : DbContext
    {
        public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options) { }
        public DbSet<Report> Reports { get; set; }
    }
}
