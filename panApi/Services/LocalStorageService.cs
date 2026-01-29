namespace PanSystem.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _uploadPath;

        public LocalStorageService(IConfiguration configuration)
        {
            _uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> SaveFileAsync(Stream stream, string fileName)
        {
            var datePath = DateTime.Now.ToString("yyyy/MM/dd");
            var fullDatePath = Path.Combine(_uploadPath, datePath);
            if (!Directory.Exists(fullDatePath))
            {
                Directory.CreateDirectory(fullDatePath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var fullPath = Path.Combine(fullDatePath, uniqueFileName);

            using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            return Path.Combine(datePath, uniqueFileName).Replace("\\", "/");
        }

        public string GetFullPath(string relativePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), _uploadPath, relativePath);
            return Path.GetFullPath(fullPath);
        }

        public Task DeleteFileAsync(string relativePath)
        {
            var fullPath = GetFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }
    }
}
