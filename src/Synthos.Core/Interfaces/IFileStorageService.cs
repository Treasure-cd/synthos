namespace Synthos.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string originalFileName);
    Task DeleteFileAsync(string storedFileName);
}
