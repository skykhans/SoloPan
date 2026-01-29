namespace PanSystem.Services
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(Stream stream, string fileName);
        string GetFullPath(string relativePath);
        Task DeleteFileAsync(string relativePath);
    }
}
