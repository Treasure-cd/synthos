using Microsoft.EntityFrameworkCore;
using Synthos.Core.Entities;
using Synthos.Core.Interfaces;
using Synthos.Infrastructure.Data;

namespace Synthos.Api.Endpoints;

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audio");

        group.MapPost("/", async (IFormFile file, AppDbContext db, IFileStorageService storage) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var storedFileName = await storage.SaveFileAsync(stream, file.FileName);

            //Create the metadata object for the database
            var metadata = new AudioMetadata
            {
                Id = Guid.NewGuid(),
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            db.AudioFiles.Add(metadata);
            await db.SaveChangesAsync();

            return Results.Created($"/api/audio/{metadata.Id}", metadata);
        })
        .DisableAntiforgery();


        group.MapGet("/", async (AppDbContext db) =>
        {
            var files = await db.AudioFiles.ToListAsync();
            return Results.Ok(files);
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var file = await db.AudioFiles.FindAsync(id);

            if (file is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(file);
                });
    }
}
