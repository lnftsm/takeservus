using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TakeServus.Application.Interfaces;

public interface IFileStorageService
{
  Task<string> UploadFileAsync(IFormFile file, string folder);
  Task<bool> DeleteFile(string relativePath);
}