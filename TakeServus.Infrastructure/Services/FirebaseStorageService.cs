using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
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

    if (string.IsNullOrWhiteSpace(credentialPath))
      throw new ArgumentException("Firebase credential path is missing in configuration.", nameof(credentialPath));

    if (!File.Exists(credentialPath))
      throw new FileNotFoundException($"Firebase credential file not found at: {credentialPath}");

    var googleCredential = GoogleCredential.FromFile(credentialPath);

    if (FirebaseApp.DefaultInstance == null)
    {
      FirebaseApp.Create(new AppOptions
      {
        Credential = googleCredential
      });
    }

    // ✅ Pass credentials explicitly to avoid ADC error
    _storageClient = StorageClient.Create(googleCredential);

    var cred = googleCredential.UnderlyingCredential as ServiceAccountCredential;
    if (cred == null)
      throw new InvalidOperationException("Failed to extract ServiceAccountCredential from Firebase credentials.");

    _bucketName = $"{cred.ProjectId}.appspot.com";
  }

  public async Task<string> UploadFileAsync(IFormFile file, string folder)
  {
    var objectName = $"{folder}/{Guid.NewGuid()}_{file.FileName}";
    using var stream = file.OpenReadStream();

    var dataObject = await _storageClient.UploadObjectAsync(
        _bucketName,
        objectName,
        file.ContentType,
        stream
    );

    _logger.LogInformation("Uploaded file '{File}' to Firebase bucket '{Bucket}'.", objectName, _bucketName);

    return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
  }
}