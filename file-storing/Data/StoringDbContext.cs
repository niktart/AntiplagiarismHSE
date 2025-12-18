using Microsoft.EntityFrameworkCore;
using FileStoring.Data;

namespace FileStoring.Data
{
    public class StoringDbContext : DbContext
    {
        public StoringDbContext(DbContextOptions<StoringDbContext> options) : base(options) { }

        public DbSet<WorkSubmission> WorkSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Убедитесь, что таблица создается с правильным именем
            modelBuilder.Entity<WorkSubmission>().ToTable("WorkSubmissions");

            // Настройте ключ и индексы если нужно
            modelBuilder.Entity<WorkSubmission>()
                .HasKey(w => w.Id);

            modelBuilder.Entity<WorkSubmission>()
                .HasIndex(w => w.StudentId);

            modelBuilder.Entity<WorkSubmission>()
                .HasIndex(w => w.AssignmentId);
        }
    }
}