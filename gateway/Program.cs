var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("FileStoring", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["FILE_STORING_URL"] ?? "http://file-storing:80"
    );
});

builder.Services.AddHttpClient("FileAnalysis", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["FILE_ANALYSIS_URL"] ?? "http://file-analysis:80"
    );
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.UseUrls("http://+:80");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
