using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;

namespace Vitalis.Tests.Unit.Dominio;

public class ResponsavelContatoTests
{
    [Fact]
    public void Validar_ContatoCompleto_DeveRetornarSemErros()
    {
        // Arrange (Preparação)
        var contato = NovoContatoValido();

        // Act (Ação)
        var erros = Validar(contato);

        // Assert (Validação/Asserção)
        erros.Should().BeEmpty();
        contato.Telefone.Should().Be("11988887777");
    }

    [Theory]
    [InlineData("CELULAR")]
    [InlineData("TELEFONE")]
    public void Validar_TipoAceitoPeloDominio_DeveRetornarSemErros(string tipo)
    {
        // Arrange
        var contato = NovoContatoValido();
        contato.Tipo = tipo;

        // Act
        var erros = Validar(contato);

        // Assert
        erros.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validar_TipoInvalido_DeveRetornarErroNoCampoTipo(string? tipoInvalido)
    {
        // Arrange
        var contato = NovoContatoValido();
        contato.Tipo = tipoInvalido!;

        // Act
        var erros = Validar(contato);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelContato.Tipo)));
    }

    [Fact]
    public void Validar_ContatoSemTelefone_DeveRetornarErroNoCampoTelefone()
    {
        // Arrange
        var contato = NovoContatoValido();
        contato.Telefone = null!;

        // Act
        var erros = Validar(contato);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelContato.Telefone)));
    }

    [Fact]
    public void NovoContato_QuandoInstanciado_NaoDeveSerPrincipalPorPadrao()
    {
        // Arrange & Act
        var contato = NovoContatoValido();

        // Assert
        contato.Principal.Should().BeFalse();
    }

    [Fact]
    public void NovoContato_MarcadoComoPrincipal_DeveManterOVinculoComOResponsavel()
    {
        // Arrange
        var contato = NovoContatoValido();

        // Act
        contato.Principal = true;

        // Assert
        contato.Principal.Should().BeTrue();
        contato.ResponsavelId.Should().Be(1);
        Validar(contato).Should().BeEmpty();
    }

    private static ResponsavelContato NovoContatoValido() => new()
    {
        ResponsavelId = 1,
        Tipo          = "CELULAR",
        Telefone      = "11988887777"
    };

    private static List<ValidationResult> Validar(ResponsavelContato contato)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(contato, new ValidationContext(contato), erros, true);
        return erros;
    }
}
