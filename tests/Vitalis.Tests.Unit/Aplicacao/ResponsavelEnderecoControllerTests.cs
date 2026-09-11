using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;
using Xunit;

namespace Vitalis.Tests.Unit.Aplicacao;

public class ResponsavelEnderecoControllerTests
{
    private readonly Mock<IResponsavelEnderecoRepository> _repositorioMock;

    public ResponsavelEnderecoControllerTests()
    {
        _repositorioMock = new Mock<IResponsavelEnderecoRepository>();
    }

    private ResponsavelEnderecoController CriarController()
        => ConfiguracaoFixture.ComHttpContext(
            new ResponsavelEnderecoController(
                _repositorioMock.Object,
                new Mock<ILogger<ResponsavelEnderecoController>>().Object));

    [Fact]
    public void GetAll_ResponsavelComEnderecos_DeveRetornarOkComALista()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByResponsavelId(1))
            .Returns([NovoEndereco(id: 1, responsavelId: 1, principal: true)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll(1);

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ResponsavelEndereco>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public void GetById_EnderecoDoProprioResponsavel_DeveRetornarOkComOEndereco()
    {
        // Arrange
        var endereco = NovoEndereco(id: 1, responsavelId: 1);
        _repositorioMock.Setup(r => r.GetById(1)).Returns(endereco);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1, 1);

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(endereco);
    }

    [Fact]
    public void GetById_EnderecoDeOutroResponsavel_DeveRetornarNotFound()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(1)).Returns(NovoEndereco(id: 1, responsavelId: 99));
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(responsavelId: 1, id: 1);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void Add_EnderecoValido_DeveAssociarAoResponsavelERetornarCreated()
    {
        // Arrange
        var endereco = NovoEndereco(id: 0, responsavelId: 0);
        _repositorioMock.Setup(r => r.Add(endereco)).Callback<ResponsavelEndereco>(e => e.Id = 7);
        var controller = CriarController();

        // Act
        var resultado = controller.Add(responsavelId: 42, endereco);

        // Assert
        var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.RouteValues!["responsavelId"].Should().Be(42L);
        endereco.ResponsavelId.Should().Be(42);
        _repositorioMock.Verify(r => r.Add(endereco), Times.Once);
    }

    [Fact]
    public void Add_ModelStateInvalido_DeveRetornarBadRequestSemSalvar()
    {
        // Arrange
        var controller = CriarController();
        controller.ModelState.AddModelError(nameof(ResponsavelEndereco.Cep), "CEP obrigatório");

        // Act
        var resultado = controller.Add(1, NovoEndereco());

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
        _repositorioMock.Verify(r => r.Add(It.IsAny<ResponsavelEndereco>()), Times.Never);
    }

    [Fact]
    public void Update_EnderecoExistente_DevePreservarOsIdentificadoresERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(5)).Returns(NovoEndereco(id: 5, responsavelId: 1));
        var controller = CriarController();
        var atualizado = NovoEndereco(id: 0, responsavelId: 0, cidade: "Santos");

        // Act
        var resultado = controller.Update(responsavelId: 1, id: 5, atualizado);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        atualizado.Id.Should().Be(5);
        atualizado.ResponsavelId.Should().Be(1);
        _repositorioMock.Verify(r => r.Update(atualizado), Times.Once);
    }

    [Fact]
    public void Update_EnderecoInexistente_DeveRetornarNotFoundSemAtualizar()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(404)).Returns((ResponsavelEndereco?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Update(1, 404, NovoEndereco());

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _repositorioMock.Verify(r => r.Update(It.IsAny<ResponsavelEndereco>()), Times.Never);
    }

    [Fact]
    public void Delete_EnderecoDoProprioResponsavel_DeveRemoverERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(5)).Returns(NovoEndereco(id: 5, responsavelId: 1));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(1, 5);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.Delete(5), Times.Once);
    }

    [Fact]
    public void SetPrincipal_EnderecoDoProprioResponsavel_DeveDelegarParaORepositorio()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(5)).Returns(NovoEndereco(id: 5, responsavelId: 1));
        var controller = CriarController();

        // Act
        var resultado = controller.SetPrincipal(1, 5);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.SetPrincipal(1, 5), Times.Once);
    }

    [Fact]
    public void SetPrincipal_EnderecoDeOutroResponsavel_DeveRetornarNotFound()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(5)).Returns(NovoEndereco(id: 5, responsavelId: 99));
        var controller = CriarController();

        // Act
        var resultado = controller.SetPrincipal(1, 5);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        _repositorioMock.Verify(r => r.SetPrincipal(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }

    private static ResponsavelEndereco NovoEndereco(
        long id = 1, long responsavelId = 1, bool principal = false, string cidade = "São Paulo") => new()
    {
        Id = id, ResponsavelId = responsavelId,
        Logradouro = "Av. Paulista", Numero = "1000", Complemento = "Sala 42",
        Bairro = "Bela Vista", Cidade = cidade, Estado = "SP", Cep = "01310100",
        Principal = principal
    };
}
