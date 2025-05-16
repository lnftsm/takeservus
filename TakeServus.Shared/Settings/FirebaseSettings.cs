namespace TakeServus.Shared.Settings;

using System.ComponentModel.DataAnnotations;

public class FirebaseSettings
{
  [Required]
  public string CredentialPath { get; set; } = default!;

  [Required]
  public string Bucket { get; set; } = default!;
}
