using System.ComponentModel.DataAnnotations;

namespace TakeServus.Application.Settings;

public class FirebaseSettings
{
  [Required]
  public string CredentialPath { get; set; } = default!;
}
