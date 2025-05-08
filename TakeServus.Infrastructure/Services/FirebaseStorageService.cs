using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TakeServus.Application.Interfaces;
using TakeServus.Application.Settings;

namespace TakeServus.Infrastructure.Services;

public class FirebaseStorageService : IFileStorageService
{
  private readonly ILogger<FirebaseStorageService> _logger;
  private readonly StorageClient _storageClient;
  private readonly string _bucketName;

  public FirebaseStorageService(IOptions<FirebaseSettings> firebaseOptions, ILogger<FirebaseStorageService> logger)
  {
    _logger = logger;
    var credentialPath = firebaseOptions.Value.CredentialPath;

    if (FirebaseApp.DefaultInstance == null)
    {
      FirebaseApp.Create(new AppOptions
      {
        Credential = GoogleCredential.FromFile(credentialPath)
      });
    }

    _storageClient = StorageClient.Create();
    var credential = FirebaseApp.DefaultInstance?.Options?.Credential?.UnderlyingCredential;
    if (credential is ServiceAccountCredential cred)
    {
      _bucketName = $"{cred.ProjectId}.appspot.com";
    }
    else
    {
      throw new InvalidOperationException("Unable to resolve Firebase bucket from ServiceAccountCredential.");
    }
  }

  public async Task<string> UploadFileAsync(IFormFile file, string folder)
  {
    var objectName = $"{folder}/{Guid.NewGuid()}_{file.FileName}";
    using var stream = file.OpenReadStream();

    var dataObject = await _storageClient.UploadObjectAsync(
        _bucketName,
        objectName,
        file.ContentType,
        stream);

    _logger.LogInformation("Uploaded {File} to Firebase Storage.", objectName);

    return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
  }
}
