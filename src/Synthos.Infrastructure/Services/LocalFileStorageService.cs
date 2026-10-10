using Synthos.Core.Interfaces;

namespace Synthos.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageDirectory;

    public LocalFileStorageService()
    {

        _storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Audio");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_storageDirectory, uniqueFileName);

        using var fileStreamOnDisk = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOnDisk);

        return uniqueFileName;
    }

    public Task DeleteFileAsync(string storedFileName)
    {
        var filePath = Path.Combine(_storageDirectory, storedFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
