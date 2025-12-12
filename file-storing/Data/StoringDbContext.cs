using Microsoft.EntityFrameworkCore;

namespace FileStoring.Data
{
    public class StoringDbContext : DbContext
    {
        public StoringDbContext(DbContextOptions<StoringDbContext> options) : base(options) { }
        public DbSet<WorkSubmission> WorkSubmissions { get; set; }
    }
}
