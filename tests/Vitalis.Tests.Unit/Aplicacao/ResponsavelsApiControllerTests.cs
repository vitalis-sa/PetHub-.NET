using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;
using Xunit;

namespace Vitalis.Tests.Unit.Aplicacao;

public class ResponsavelsApiControllerTests : IClassFixture<ConfiguracaoFixture>
{
    private readonly ConfiguracaoFixture _fixture;
    private readonly Mock<IResponsavelRepository> _repositorioMock;
    private readonly Mock<ILogger<ResponsavelsApiController>> _loggerMock;

    public ResponsavelsApiControllerTests(ConfiguracaoFixture fixture)
    {
        _fixture = fixture;
        _repositorioMock = new Mock<IResponsavelRepository>();
        _loggerMock = new Mock<ILogger<ResponsavelsApiController>>();
    }

    private ResponsavelsApiController CriarController(string? serviceToken = null)
        => ConfiguracaoFixture.ComHttpContext(
            new ResponsavelsApiController(
                _repositorioMock.Object,
                _fixture.Configuration,
                _loggerMock.Object),
            serviceToken);

    [Fact]
    public void GetAll_ResponsaveisCadastrados_DeveRetornarOkComTodosOsRegistros()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetAll()).Returns(
        [
            new Responsavel { Id = 1, Nome = "Ana",  Cpf = "11111111111", Email = "a@pethub.com", Senha = "x" },
            new Responsavel { Id = 2, Nome = "Lucas", Cpf = "22222222222", Email = "b@pethub.com", Senha = "x" }
        ]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll();

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Which.Should().HaveCount(2);
        _repositorioMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public void GetById_ResponsavelExistente_DeveRetornarOkComOsDados()
    {
        // Arrange
        var existente = NovoResponsavel(id: 1);
        _repositorioMock.Setup(r => r.GetById(1)).Returns(existente);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        _repositorioMock.Verify(r => r.GetById(1), Times.Once);
    }

    [Fact]
    public void GetById_ResponsavelInexistente_DeveRetornarNotFound()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(999)).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(999);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void BuscarPorCpf_ServiceTokenValido_DeveRetornarOkComOResponsavel()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByCpf("12345678901")).Returns(NovoResponsavel());
        var controller = CriarController(ConfiguracaoFixture.ServiceTokenValido);

        // Act
        var resultado = controller.BuscarPorCpf("12345678901");

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        _repositorioMock.Verify(r => r.GetByCpf("12345678901"), Times.Once);
    }

    [Fact]
    public void BuscarPorCpf_ServiceTokenInvalido_DeveRetornarUnauthorizedSemConsultarORepositorio()
    {
        // Arrange
        var controller = CriarController("token-errado");

        // Act
        var resultado = controller.BuscarPorCpf("12345678901");

        // Assert
        resultado.Should().BeOfType<UnauthorizedObjectResult>();
        _repositorioMock.Verify(r => r.GetByCpf(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void BuscarPorCpf_CpfEmBranco_DeveRetornarBadRequest()
    {
        // Arrange
        var controller = CriarController(ConfiguracaoFixture.ServiceTokenValido);

        // Act
        var resultado = controller.BuscarPorCpf("   ");

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void BuscarPorCpf_CpfNaoCadastrado_DeveRetornarNotFound()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByCpf("00000000000")).Returns((Responsavel?)null);
        var controller = CriarController(ConfiguracaoFixture.ServiceTokenValido);

        // Act
        var resultado = controller.BuscarPorCpf("00000000000");

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void Cadastrar_DadosValidos_DeveSalvarNoRepositorioERetornarCreated()
    {
        // Arrange
        var dto = NovoCadastroDto();
        _repositorioMock.Setup(r => r.GetByCpf(dto.Cpf)).Returns((Responsavel?)null);
        _repositorioMock.Setup(r => r.Add(It.IsAny<Responsavel>()))
            .Callback<Responsavel>(r => r.Id = 10);
        var controller = CriarController();

        // Act
        var resultado = controller.Cadastrar(dto);

        // Assert
        var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.RouteValues!["id"].Should().Be(10L);
        _repositorioMock.Verify(r => r.Add(It.Is<Responsavel>(x => x.Cpf == dto.Cpf && x.Ativo)), Times.Once);
    }

    [Fact]
    public void Cadastrar_CpfJaExistente_DeveRetornarConflictSemSalvar()
    {
        // Arrange
        var dto = NovoCadastroDto();
        _repositorioMock.Setup(r => r.GetByCpf(dto.Cpf)).Returns(NovoResponsavel());
        var controller = CriarController();

        // Act
        var resultado = controller.Cadastrar(dto);

        // Assert
        resultado.Should().BeOfType<ConflictObjectResult>();
        _repositorioMock.Verify(r => r.Add(It.IsAny<Responsavel>()), Times.Never);
    }

    [Fact]
    public void Cadastrar_ModelStateInvalido_DeveRetornarBadRequestSemSalvar()
    {
        // Arrange
        var controller = CriarController();
        controller.ModelState.AddModelError(nameof(CadastrarResponsavelDto.Email), "E-mail inválido");

        // Act
        var resultado = controller.Cadastrar(NovoCadastroDto());

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
        _repositorioMock.Verify(r => r.Add(It.IsAny<Responsavel>()), Times.Never);
    }

    [Fact]
    public void Login_CredenciaisValidas_DeveRetornarOkComOsDadosDoResponsavel()
    {
        // Arrange
        var responsavel = NovoResponsavelComSenhaHasheada();
        _repositorioMock.Setup(r => r.GetByEmail(responsavel.Email)).Returns(responsavel);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto { Email = responsavel.Email, Senha = SenhaPadrao });

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Login_SenhaIncorreta_DeveRetornarUnauthorized()
    {
        // Arrange
        var responsavel = NovoResponsavelComSenhaHasheada();
        _repositorioMock.Setup(r => r.GetByEmail(responsavel.Email)).Returns(responsavel);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto { Email = responsavel.Email, Senha = "senha-errada" });

        // Assert
        resultado.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void Login_EmailNaoCadastrado_DeveRetornarUnauthorized()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByEmail("ninguem@pethub.com")).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto { Email = "ninguem@pethub.com", Senha = SenhaPadrao });

        // Assert
        resultado.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void Login_ContaDesativada_DeveRetornarUnauthorized()
    {
        // Arrange
        var responsavel = NovoResponsavelComSenhaHasheada();
        responsavel.Ativo = false;
        _repositorioMock.Setup(r => r.GetByEmail(responsavel.Email)).Returns(responsavel);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto { Email = responsavel.Email, Senha = SenhaPadrao });

        // Assert
        resultado.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public void Update_ResponsavelExistente_DeveAtualizarERetornarNoContent()
    {
        // Arrange
        var existente = NovoResponsavel(id: 5);
        _repositorioMock.Setup(r => r.GetById(5)).Returns(existente);
        var controller = CriarController();
        var dto = NovoCadastroDto(nome: "Nome Atualizado", email: "novo@pethub.com");

        // Act
        var resultado = controller.Update(5, dto);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        existente.Nome.Should().Be("Nome Atualizado");
        existente.Email.Should().Be("novo@pethub.com");
        _repositorioMock.Verify(r => r.Update(existente), Times.Once);
    }

    [Fact]
    public void Update_ResponsavelInexistente_DeveRetornarNotFoundSemAtualizar()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(404)).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Update(404, NovoCadastroDto());

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _repositorioMock.Verify(r => r.Update(It.IsAny<Responsavel>()), Times.Never);
    }

    [Fact]
    public void Delete_ResponsavelExistente_DeveRemoverERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(3)).Returns(NovoResponsavel(id: 3));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(3);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.Delete(3), Times.Once);
    }

    [Fact]
    public void Delete_ResponsavelInexistente_DeveRetornarNotFoundSemRemover()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(404)).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(404);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _repositorioMock.Verify(r => r.Delete(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public void Cadastrar_RepositorioIndisponivel_DeveRegistrarLogDeErroERetornar500()
    {
        // Arrange
        var dto = NovoCadastroDto();
        _repositorioMock.Setup(r => r.GetByCpf(dto.Cpf)).Returns((Responsavel?)null);
        _repositorioMock.Setup(r => r.Add(It.IsAny<Responsavel>()))
            .Throws(new InvalidOperationException("Banco de dados indisponível"));
        var controller = CriarController();

        // Act
        var resultado = controller.Cadastrar(dto);

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

    private const string SenhaPadrao = "SenhaSegura@123";

    private static Responsavel NovoResponsavel(long id = 1) => new()
    {
        Id = id, Nome = "Pedro Chasci", Cpf = "12345678901",
        Email = "pedro@pethub.com", Senha = SenhaPadrao, Ativo = true
    };

    private static Responsavel NovoResponsavelComSenhaHasheada()
    {
        var responsavel = NovoResponsavel();
        responsavel.Senha = BCrypt.Net.BCrypt.HashPassword(SenhaPadrao);
        return responsavel;
    }

    private static CadastrarResponsavelDto NovoCadastroDto(
        string nome = "Pedro Chasci", string email = "pedro@pethub.com") => new()
    {
        Nome = nome, Cpf = "12345678901", Email = email, Senha = SenhaPadrao
    };
}
