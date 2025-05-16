using Microsoft.AspNetCore.Http;

namespace TakeServus.Application.Interfaces;

public interface IFirebaseStorageService
{
  Task<string> UploadFileAsync(IFormFile file, string folder);

  Task<bool> DeleteFile(string photoUrl);
}