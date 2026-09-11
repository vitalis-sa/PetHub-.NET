using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;
using Xunit;

namespace Vitalis.Tests.Unit.Aplicacao;

public class ResponsavelContatoControllerTests
{
    private readonly Mock<IResponsavelContatoRepository> _repositorioMock;

    public ResponsavelContatoControllerTests()
    {
        _repositorioMock = new Mock<IResponsavelContatoRepository>();
    }

    private ResponsavelContatoController CriarController()
        => ConfiguracaoFixture.ComHttpContext(
            new ResponsavelContatoController(
                _repositorioMock.Object,
                new Mock<ILogger<ResponsavelContatoController>>().Object));

    [Fact]
    public void GetAll_ResponsavelComContatos_DeveRetornarOkComALista()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetByResponsavelId(1))
            .Returns([NovoContato(id: 1, responsavelId: 1, principal: true)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll(1);

        // Assert
        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<ResponsavelContato>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public void GetById_ContatoInexistente_DeveRetornarNotFound()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(999)).Returns((ResponsavelContato?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1, 999);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void Add_ContatoValido_DeveAssociarAoResponsavelERetornarCreated()
    {
        // Arrange
        var contato = NovoContato(id: 0, responsavelId: 0);
        _repositorioMock.Setup(r => r.Add(contato)).Callback<ResponsavelContato>(c => c.Id = 3);
        var controller = CriarController();

        // Act
        var resultado = controller.Add(responsavelId: 8, contato);

        // Assert
        resultado.Should().BeOfType<CreatedAtActionResult>();
        contato.ResponsavelId.Should().Be(8);
        _repositorioMock.Verify(r => r.Add(contato), Times.Once);
    }

    [Fact]
    public void Update_ContatoExistente_DevePreservarOsIdentificadoresERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(4)).Returns(NovoContato(id: 4, responsavelId: 2));
        var controller = CriarController();
        var atualizado = NovoContato(id: 0, responsavelId: 0, telefone: "11912345678");

        // Act
        var resultado = controller.Update(responsavelid: 2, id: 4, atualizado);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        atualizado.Id.Should().Be(4);
        atualizado.ResponsavelId.Should().Be(2);
    }

    [Fact]
    public void Delete_ContatoDoProprioResponsavel_DeveRemoverERetornarNoContent()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(4)).Returns(NovoContato(id: 4, responsavelId: 2));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(2, 4);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.Delete(4), Times.Once);
    }

    [Fact]
    public void SetPrincipal_ContatoDoProprioResponsavel_DeveDelegarParaORepositorio()
    {
        // Arrange
        _repositorioMock.Setup(r => r.GetById(4)).Returns(NovoContato(id: 4, responsavelId: 2));
        var controller = CriarController();

        // Act
        var resultado = controller.SetPrincipal(2, 4);

        // Assert
        resultado.Should().BeOfType<NoContentResult>();
        _repositorioMock.Verify(r => r.SetPrincipal(2, 4), Times.Once);
    }

    private static ResponsavelContato NovoContato(
        long id = 1, long responsavelId = 1, bool principal = false, string telefone = "11999998888") => new()
    {
        Id = id, ResponsavelId = responsavelId, Tipo = "CELULAR",
        Telefone = telefone, Principal = principal
    };
}
