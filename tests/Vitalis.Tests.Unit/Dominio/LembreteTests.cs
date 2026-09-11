using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Vitalis.Models;
using Xunit;

namespace Vitalis.Tests.Unit.Dominio;

public class LembreteTests
{
    [Fact]
    public void NovoLembrete_QuandoInstanciado_DeveIniciarComStatusPendente()
    {
        // Arrange & Act
        var lembrete = NovoLembreteValido();

        // Assert
        lembrete.Status.Should().Be(StatusLembrete.PENDENTE);
    }

    [Fact]
    public void Validar_DadosValidos_DeveRetornarSemErros()
    {
        // Arrange
        var lembrete = NovoLembreteValido();

        // Act
        var erros = Validar(lembrete);

        // Assert
        erros.Should().BeEmpty();
    }

    [Fact]
    public void Validar_SemMensagem_DeveRetornarErroNoCampoMensagem()
    {
        // Arrange
        var lembrete = NovoLembreteValido();
        lembrete.Mensagem = null!;

        // Act
        var erros = Validar(lembrete);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(Lembrete.Mensagem)));
    }

    [Theory]
    [InlineData(TipoLembrete.VACINA)]
    [InlineData(TipoLembrete.CONSULTA)]
    [InlineData(TipoLembrete.EXAME)]
    [InlineData(TipoLembrete.MEDICAMENTO)]
    [InlineData(TipoLembrete.HIDRATACAO)]
    public void NovoLembrete_ParaCadaTipoSuportado_DevePreservarOTipoInformado(TipoLembrete tipo)
    {
        // Arrange & Act
        var lembrete = NovoLembreteValido();
        lembrete.Tipo = tipo;

        // Assert
        lembrete.Tipo.Should().Be(tipo);
    }

    [Fact]
    public void ConverterTipo_ComNomeInexistente_NaoDeveConverter()
    {
        // Arrange
        const string tipoInvalido = "BANHO";

        // Act
        var conseguiuConverter = Enum.TryParse<TipoLembrete>(tipoInvalido, false, out _);

        // Assert
        conseguiuConverter.Should().BeFalse();
    }

    private static Lembrete NovoLembreteValido() => new()
    {
        ResponsavelId = 1,
        PetId         = 7,
        Tipo          = TipoLembrete.VACINA,
        DataAgendada  = new DateOnly(2026, 9, 15),
        Mensagem      = "Vacina antirrábica agendada"
    };

    private static List<ValidationResult> Validar(Lembrete lembrete)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(lembrete, new ValidationContext(lembrete), erros, true);
        return erros;
    }
}
