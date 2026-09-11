using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;

namespace Vitalis.Tests.Unit.Dominio;

public class ResponsavelEnderecoTests
{
    [Fact]
    public void Validar_EnderecoCompleto_DeveRetornarSemErros()
    {
        // Arrange
        var endereco = NovoEnderecoValido();

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().BeEmpty();
    }

    [Fact]
    public void Validar_EstadoAcimaDeDoisCaracteres_DeveRetornarErroNoCampoEstado()
    {
        // Arrange
        var endereco = NovoEnderecoValido();
        endereco.Estado = "São Paulo";

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelEndereco.Estado)));
    }

    [Fact]
    public void Validar_CepAcimaDeOitoCaracteres_DeveRetornarErroNoCampoCep()
    {
        // Arrange
        var endereco = NovoEnderecoValido();
        endereco.Cep = "013101000000";

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelEndereco.Cep)));
    }

    [Fact]
    public void Validar_SemComplemento_DeveRetornarSemErrosPorSerCampoOpcional()
    {
        // Arrange
        var endereco = NovoEnderecoValido();
        endereco.Complemento = null;

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().BeEmpty();
    }

    [Fact]
    public void NovoEndereco_QuandoInstanciado_NaoDeveSerPrincipalPorPadrao()
    {
        // Arrange & Act
        var endereco = NovoEnderecoValido();

        // Assert
        endereco.Principal.Should().BeFalse();
    }

    [Fact]
    public void Validar_ContatoSemTelefone_DeveRetornarErroNoCampoTelefone()
    {
        // Arrange
        var contato = new ResponsavelContato
        {
            ResponsavelId = 1,
            Tipo = "CELULAR",
            Telefone = null!
        };

        // Act
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(contato, new ValidationContext(contato), erros, true);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelContato.Telefone)));
    }

    private static ResponsavelEndereco NovoEnderecoValido() => new()
    {
        ResponsavelId = 1,
        Logradouro    = "Av. Paulista",
        Numero        = "1000",
        Complemento   = "Sala 42",
        Bairro        = "Bela Vista",
        Cidade        = "São Paulo",
        Estado        = "SP",
        Cep           = "01310100"
    };

    private static List<ValidationResult> Validar(ResponsavelEndereco endereco)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(endereco, new ValidationContext(endereco), erros, true);
        return erros;
    }
}
