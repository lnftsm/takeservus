using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TakeServus.Application.Interfaces;
using TakeServus.Shared.Settings;

namespace TakeServus.Infrastructure.Services;

public class FirebaseStorageService : IFirebaseStorageService
{
  private readonly ILogger<FirebaseStorageService> _logger;
  private readonly StorageClient _storageClient;
  private readonly string _bucketName;

  public FirebaseStorageService(IOptions<FirebaseSettings> firebaseOptions, ILogger<FirebaseStorageService> logger)
  {
    _logger = logger;

    var settings = firebaseOptions.Value;

    if (string.IsNullOrWhiteSpace(settings.CredentialPath))
      throw new ArgumentException("Firebase credential path is missing in configuration.");

    if (!File.Exists(settings.CredentialPath))
      throw new FileNotFoundException($"Firebase credential file not found at: {settings.CredentialPath}");

    var googleCredential = GoogleCredential.FromFile(settings.CredentialPath);

    if (FirebaseApp.DefaultInstance == null)
    {
      FirebaseApp.Create(new AppOptions
      {
        Credential = googleCredential
      });
    }

    _storageClient = StorageClient.Create(googleCredential);

    if (!string.IsNullOrWhiteSpace(settings.Bucket))
    {
      _bucketName = settings.Bucket;
    }
    else
    {
      var cred = googleCredential.UnderlyingCredential as ServiceAccountCredential;
      if (cred == null)
        throw new InvalidOperationException("Cannot extract project ID from service account.");

      _bucketName = $"{cred.ProjectId}.appspot.com";
    }

    _logger.LogInformation("Firebase bucket configured: {Bucket}", _bucketName);
  }

  public async Task<string> UploadFileAsync(IFormFile file, string folder)
  {
    var objectName = $"{folder}/{Guid.NewGuid()}_{file.FileName}";
    await using var stream = file.OpenReadStream();

    var dataObject = await _storageClient.UploadObjectAsync(
        _bucketName,
        objectName,
        file.ContentType,
        stream
    );

    _logger.LogInformation("Uploaded file '{File}' to Firebase bucket '{Bucket}'.", objectName, _bucketName);

    return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
  }

  public async Task<bool> DeleteFile(string photoUrl)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(photoUrl))
        throw new ArgumentException("Photo URL cannot be empty.");

      // Extract object path from full URL
      var prefix = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/";
      if (!photoUrl.StartsWith(prefix))
        throw new ArgumentException("Invalid Firebase URL format.");

      var objectName = photoUrl.Substring(prefix.Length);

      await _storageClient.DeleteObjectAsync(_bucketName, objectName);
      _logger.LogInformation("Deleted file from Firebase: {Object}", objectName);

      return true;
    }
    catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
    {
      _logger.LogWarning("File not found in Firebase Storage.");
      return false;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting file from Firebase.");
      return false;
    }
  }
}