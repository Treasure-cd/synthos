using Microsoft.EntityFrameworkCore;
using Synthos.Infrastructure.Data;
using Synthos.Core.Interfaces;
using Synthos.Infrastructure.Services;
using Synthos.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapAudioEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();
