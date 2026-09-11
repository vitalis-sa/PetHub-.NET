using System.ComponentModel.DataAnnotations;

public class LoginDto
{
    [Required][EmailAddress] public string Email { get; set; } = null!;
    [Required] public string Senha { get; set; } = null!;
}