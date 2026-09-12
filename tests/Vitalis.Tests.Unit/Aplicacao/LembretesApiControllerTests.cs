using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Vitalis.Models;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;
using Xunit;

namespace Vitalis.Tests.Unit.Aplicacao;

public class LembretesApiControllerTests : IClassFixture<ConfiguracaoFixture>
{
    private readonly ConfiguracaoFixture _fixture;
    private readonly Mock<ILembreteRepository> _repositorioMock;
    private readonly Mock<ILogger<LembretesApiController>> _loggerMock;

    public LembretesApiControllerTests(ConfiguracaoFixture fixture)
    {
        _fixture = fixture;
        _repositorioMock = new Mock<ILembreteRepository>();
        _loggerMock = new Mock<ILogger<LembretesApiController>>();
    }

    private LembretesApiController CriarController(string? serviceToken = null)
        => ConfiguracaoFixture.ComHttpContext(
            new LembretesApiController(
                _repositorioMock.Object,
                _fixture.Configuration,
                _loggerMock.Object),
            serviceToken);

    [Fact]
    public void GetAll_LembretesCadastrados_DeveRetornarOkComTodosOsRegistros()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetAll()).Returns(
        [
            NovoLembrete(id: 1),
            NovoLembrete(id: 2, tipo: TipoLembrete.EXAME)
        ]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll();

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Which.Should().HaveCount(2);
    }

    [Fact]
    public void GetById_LembreteExistente_DeveRetornarOkComOLembrete()
    {
        // Arrange
        var lembrete = NovoLembrete(id: 1);
        _repositorioMock.Setup(r => r.GetById(1)).Returns(lembrete);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1);

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(lembrete);
        _repositorioMock.Verify(r => r.GetById(1), Times.Once);
    }

    [Fact]
    public void GetById_LembreteInexistente_DeveRetornarNotFound()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(999)).Returns((Lembrete?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(999);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void GetByResponsavel_ResponsavelComLembretes_DeveRetornarApenasOsSeusLembretes()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByResponsavelId(1)).Returns([NovoLembrete(id: 1, responsavelId: 1)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetByResponsavel(1);

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<Lembrete>>()
            .Which.Should().OnlyContain(l => l.ResponsavelId == 1);
    }

    [Fact]
    public void GetByResponsavelETipo_TipoInformado_DeveDelegarOFiltroParaORepositorio()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByResponsavelIdETipo(1, TipoLembrete.VACINA))
            .Returns([NovoLembrete(tipo: TipoLembrete.VACINA)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetByResponsavelETipo(1, TipoLembrete.VACINA);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        _repositorioMock.Verify(r => r.GetByResponsavelIdETipo(1, TipoLembrete.VACINA), Times.Once);
    }

    [Fact]
    public void Criar_ServiceTokenValido_DeveSalvarNoRepositorioERetornarCreated()
    {
        // Arrange
        var dto = NovoCriarLembreteDto();
        _repositorioMock.Setup(r => r.Add(It.IsAny<Lembrete>())).Callback<Lembrete>(l => l.Id = 20);
        var controller = CriarController(ConfiguracaoFixture.ServiceTokenValido);

        // Act
        var resultado = controller.Criar(dto);

        // Assert
        var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.RouteValues!["id"].Should().Be(20L);
        // Verifica se o método Add do repositório foi chamado exatamente 1 vez
        _repositorioMock.Verify(r => r.Add(It.Is<Lembrete>(l =>
            l.ResponsavelId == dto.ResponsavelId && l.Tipo == dto.Tipo && l.Mensagem == dto.Mensagem)), Times.Once);
    }

    [Fact]
    public void Criar_SemServiceToken_DeveRetornarUnauthorizedSemSalvar()
    {
        // Arrange
        var controller = CriarController();

        // Act
        var resultado = controller.Criar(NovoCriarLembreteDto());

        // Assert
        resultado.Should().BeOfType<UnauthorizedObjectResult>();
        _repositorioMock.Verify(r => r.Add(It.IsAny<Lembrete>()), Times.Never);
    }

    [Fact]
    public void Criar_ServiceTokenInvalido_DeveRetornarUnauthorized()
    {
        // Arrange
        var controller = CriarController("token-invalido");

        // Act
        var resultado = controller.Criar(NovoCriarLembreteDto());

        // Assert
        resultado.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void Criar_ModelStateInvalido_DeveRetornarBadRequestSemSalvar()
    {
        // Arrange
        var controller = CriarController(ConfiguracaoFixture.ServiceTokenValido);
        controller.ModelState.AddModelError(nameof(CriarLembreteDto.Mensagem), "Mensagem obrigatória");

        // Act
        var resultado = controller.Criar(NovoCriarLembreteDto());

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
        _repositorioMock.Verify(r => r.Add(It.IsAny<Lembrete>()), Times.Never);
    }

    [Fact]
    public void AtualizarStatus_LembreteExistente_DeveAtualizarERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(1)).Returns(NovoLembrete(id: 1));
        var controller = CriarController();

        // Act
        var resultado = controller.AtualizarStatus(1, new AtualizarStatusDto { Status = StatusLembrete.ENVIADO });

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.AtualizarStatus(1, StatusLembrete.ENVIADO), Times.Once);
    }

    [Fact]
    public void AtualizarStatus_LembreteInexistente_DeveRetornarNotFoundSemAtualizar()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(999)).Returns((Lembrete?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.AtualizarStatus(999, new AtualizarStatusDto { Status = StatusLembrete.FALHOU });

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _repositorioMock.Verify(r => r.AtualizarStatus(It.IsAny<long>(), It.IsAny<StatusLembrete>()), Times.Never);
    }

    [Fact]
    public void Delete_LembreteExistente_DeveRemoverERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(1)).Returns(NovoLembrete(id: 1));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(1);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public void Delete_LembreteInexistente_DeveRetornarNotFoundSemRemover()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(999)).Returns((Lembrete?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(999);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _repositorioMock.Verify(r => r.Delete(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public void Criar_RepositorioIndisponivel_DeveRegistrarLogDeErroERetornar500()
    {
        // Arrange
        var dto = NovoCriarLembreteDto();
        _repositorioMock.Setup(r => r.Add(It.IsAny<Lembrete>()))
            .Throws(new InvalidOperationException("Banco de dados indisponível"));
        var controller = CriarController(ConfiguracaoFixture.ServiceTokenValido);

        // Act
        var resultado = controller.Criar(dto);

        // Assert
        var erro = resultado.Should().BeOfType<ObjectResult>().Subject;
        erro.StatusCode.Should().Be(500);
        _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    private static Lembrete NovoLembrete(
        long id = 1, long responsavelId = 1, TipoLembrete tipo = TipoLembrete.VACINA) => new()
    {
        Id = id, ResponsavelId = responsavelId, PetId = 7, Tipo = tipo,
        DataAgendada = new DateOnly(2026, 9, 15), Mensagem = "Vacina antirrábica agendada"
    };

    private static CriarLembreteDto NovoCriarLembreteDto() => new()
    {
        ResponsavelId = 1, PetId = 7, Tipo = TipoLembrete.CONSULTA,
        DataAgendada = new DateOnly(2026, 9, 20), Mensagem = "Consulta de retorno"
    };
}
