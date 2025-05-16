using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using TakeServus.Application.Interfaces;

namespace TakeServus.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
  private readonly ILogger<FileStorageService> _logger;
  private readonly string _basePath = Path.Combine("wwwroot", "uploads");

  public FileStorageService(ILogger<FileStorageService> logger)
  {
    _logger = logger;
    if (!Directory.Exists(_basePath))
      Directory.CreateDirectory(_basePath);
  }

  public async Task<string> UploadFileAsync(IFormFile file, string folder)
  {
    var folderPath = Path.Combine(_basePath, folder);
    Directory.CreateDirectory(folderPath);

    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
    var fullPath = Path.Combine(folderPath, uniqueFileName);

    await using var stream = new FileStream(fullPath, FileMode.Create);
    await file.CopyToAsync(stream);

    var relativePath = Path.Combine("/uploads", folder, uniqueFileName).Replace("\\", "/");
    _logger.LogInformation("Uploaded file locally: {Path}", relativePath);

    return relativePath;
  }

  public Task<bool> DeleteFile(string relativePath)
  {
    return Task.Run(() =>
    {
      try
      {
        var fullPath = Path.Combine(_basePath, relativePath.TrimStart('/')
            .Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath))
        {
          File.Delete(fullPath);
          _logger.LogInformation("Deleted file locally: {Path}", fullPath);
          return true;
        }
        _logger.LogWarning("File not found for deletion: {Path}", fullPath);
        return false;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting file: {Path}", relativePath);
        return false;
      }
    });
  }
}