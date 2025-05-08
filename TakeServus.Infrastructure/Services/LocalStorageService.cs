using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using TakeServus.Application.Interfaces;

namespace TakeServus.Infrastructure.Services;

public class LocalStorageService : IFileStorageService
{
  private readonly ILogger<LocalStorageService> _logger;
  private readonly string _basePath = Path.Combine("wwwroot", "photoUpload");

  public LocalStorageService(ILogger<LocalStorageService> logger)
  {
    _logger = logger;
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

    var relativePath = Path.Combine("/photoUpload", folder, uniqueFileName).Replace("\\", "/");
    _logger.LogInformation("Uploaded file locally: {Path}", relativePath);

    return relativePath;
  }
}