using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Proyecto.Models;

public class User
{
    [Key]
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;

    public static string GetHash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashedBytes = MD5.HashData(inputBytes);
        return BitConverter.ToString(hashedBytes).Replace("-", "");;
    }
}

public class UserCredentials
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class CreateUserViewModel : IValidatableObject
{
    [Required]
    public string? Name { get; set; }
    [Required]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public string? PasswordConfirm { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if(Password != PasswordConfirm)
        {
            yield return new ValidationResult("Las contraseñas deben coincidir");
        }
    }
}
