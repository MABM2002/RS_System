using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Threading.Tasks;

namespace Rs_system.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folderName);
    Task<bool> DeleteFileAsync(string filePath);
    string GetFileUrl(string filePath);
}

public class FileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _environment;
    private readonly string _uploadsFolder;

    public FileStorageService(IHostEnvironment environment)
    {
        _environment = environment;
        _uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
        
        // Ensure uploads folder exists
        if (!Directory.Exists(_uploadsFolder))
        {
            Directory.CreateDirectory(_uploadsFolder);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            return string.Empty;

        // Create folder if it doesn't exist
        var folderPath = Path.Combine(_uploadsFolder, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(folderPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path for database storage
        return Path.Combine("uploads", folderName, fileName).Replace("\\", "/");
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return true;

        try
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, "wwwroot", filePath);
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                return true;
            }
            
            return true; // File doesn't exist, consider it deleted
        }
        catch
        {
            return false;
        }
    }

    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        return $"/{filePath}";
    }
}