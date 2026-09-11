using System.ComponentModel.DataAnnotations;

public class CadastrarResponsavelDto
{
    [Required] public string Nome  { get; set; } = null!;
    [Required] public string Cpf   { get; set; } = null!;
    [Required][EmailAddress] public string Email { get; set; } = null!;
    [Required] public string Senha { get; set; } = null!;
}