using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;

namespace Vitalis.Tests.Unit.Dominio;

public class ResponsavelTests
{
    [Fact]
    public void Validar_DadosValidos_DeveCriarInstanciaSemErros()
    {
        // Arrange (Preparação)
        var responsavel = new Responsavel
        {
            Nome  = "Pedro Chasci",
            Cpf   = "12345678901",
            Email = "pedro@pethub.com",
            Senha = "SenhaSegura@123"
        };

        // Act (Ação)
        var erros = Validar(responsavel);

        // Assert (Validação/Asserção)
        erros.Should().BeEmpty();
        responsavel.Nome.Should().Be("Pedro Chasci");
        responsavel.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validar_NomeInvalido_DeveRetornarErroNoCampoNome(string? nomeInvalido)
    {
        // Arrange
        var responsavel = NovoResponsavelValido();
        responsavel.Nome = nomeInvalido!;

        // Act
        var erros = Validar(responsavel);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(Responsavel.Nome)));
    }

    [Fact]
    public void Validar_CpfAcimaDoLimite_DeveRetornarErroNoCampoCpf()
    {
        // Arrange
        var responsavel = NovoResponsavelValido();
        responsavel.Cpf = "123456789012345";

        // Act
        var erros = Validar(responsavel);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(Responsavel.Cpf)));
    }

    [Fact]
    public void Validar_NomeAcimaDe150Caracteres_DeveRetornarErroNoCampoNome()
    {
        // Arrange
        var responsavel = NovoResponsavelValido();
        responsavel.Nome = new string('a', 151);

        // Act
        var erros = Validar(responsavel);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(Responsavel.Nome)));
    }

    [Fact]
    public void NovoResponsavel_QuandoInstanciado_DeveIniciarAtivoComColecoesVazias()
    {
        // Arrange & Act
        var responsavel = NovoResponsavelValido();

        // Assert
        responsavel.Ativo.Should().BeTrue();
        responsavel.Enderecos.Should().BeEmpty();
        responsavel.Contatos.Should().BeEmpty();
        responsavel.Lembretes.Should().BeEmpty();
    }

    private static Responsavel NovoResponsavelValido() => new()
    {
        Nome  = "Pedro Chasci",
        Cpf   = "12345678901",
        Email = "pedro@pethub.com",
        Senha = "SenhaSegura@123"
    };

    private static List<ValidationResult> Validar(Responsavel responsavel)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(responsavel, new ValidationContext(responsavel), erros, true);
        return erros;
    }
}
