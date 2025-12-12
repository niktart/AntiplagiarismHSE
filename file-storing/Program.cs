using FileStoring.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("Default")
           ?? builder.Configuration["ConnectionStrings__Default"]
           ?? "Host=postgres;Port=5432;Database=antiplagiarism;Username=postgres;Password=postgres";

builder.Services.AddDbContext<StoringDbContext>(opt => opt.UseNpgsql(conn));

var app = builder.Build();

// Migrate DB with retry
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoringDbContext>();

    for (int i = 0; i < 10; i++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch
        {
            Console.WriteLine("Postgres not ready, retrying...");
            Thread.Sleep(2000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
