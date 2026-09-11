using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vitalis.Tests.Integration.Fixtures;
using Xunit;

namespace Vitalis.Tests.Integration.Endpoints;

[Collection(VitalisApiCollection.Name)]
public class EnderecoContatoEndpointsTests : IDisposable
{
    private readonly HttpClient _client;

    public EnderecoContatoEndpointsTests(VitalisWebApplicationFactory factory)
    {
        factory.LimparBanco();
        _client = factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PostEndereco_DadosValidos_DeveRetornar201EMarcarOPrimeiroComoPrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/enderecos", DadosDeIntegracao.NovoEndereco());
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        criado.GetProperty("principal").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PostEndereco_PayloadIncompleto_DeveRetornar400()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var enderecoIncompleto = new { logradouro = "Av. Paulista" };

        // Act
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/enderecos", enderecoIncompleto);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEnderecos_AposCadastrarDois_DeveRetornar200ComOsDoisEnderecos()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        await CriarEnderecoAsync(responsavelId);
        await CriarEnderecoAsync(responsavelId);

        // Act
        var enderecos = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}/enderecos");

        // Assert
        enderecos.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetEnderecoById_EnderecoDeOutroResponsavel_DeveRetornar404()
    {
        // Arrange
        var dono = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "11111111111", email: "dono@pethub.com");
        var estranho = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "22222222222", email: "outro@pethub.com");
        var enderecoId = await CriarEnderecoAsync(dono);

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{estranho}/enderecos/{enderecoId}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchEnderecoPrincipal_ComDoisEnderecos_DeveTransferirAMarcacaoDePrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var primeiro = await CriarEnderecoAsync(responsavelId);
        var segundo = await CriarEnderecoAsync(responsavelId);

        // Act
        var resposta = await _client.PatchAsync(
            $"/api/responsavel/{responsavelId}/enderecos/{segundo}/principal", content: null);
        var enderecos = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}/enderecos");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        BuscarPorId(enderecos, primeiro).GetProperty("principal").GetBoolean().Should().BeFalse();
        BuscarPorId(enderecos, segundo).GetProperty("principal").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEndereco_EnderecoExistente_DeveRetornar204ERemoverORegistro()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var enderecoId = await CriarEnderecoAsync(responsavelId);

        // Act
        var resposta = await _client.DeleteAsync($"/api/responsavel/{responsavelId}/enderecos/{enderecoId}");
        var consulta = await _client.GetAsync($"/api/responsavel/{responsavelId}/enderecos/{enderecoId}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        consulta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostContato_DadosValidos_DeveRetornar201EMarcarOPrimeiroComoPrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/contatos", DadosDeIntegracao.NovoContato());
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        criado.GetProperty("principal").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PatchContatoPrincipal_ComDoisContatos_DeveTransferirAMarcacaoDePrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var primeiro = await CriarContatoAsync(responsavelId, "11999998888");
        var segundo = await CriarContatoAsync(responsavelId, "11955554444");

        // Act
        var resposta = await _client.PatchAsync(
            $"/api/responsavel/{responsavelId}/contatos/{segundo}/principal", content: null);
        var contatos = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}/contatos");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        BuscarPorId(contatos, primeiro).GetProperty("principal").GetBoolean().Should().BeFalse();
        BuscarPorId(contatos, segundo).GetProperty("principal").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetContatoById_IdInexistente_DeveRetornar404()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{responsavelId}/contatos/999999");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetResponsavelById_ComEnderecoEContato_DeveRetornarOsRelacionamentosAninhados()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        await CriarEnderecoAsync(responsavelId);
        await CriarContatoAsync(responsavelId, "11999998888");

        // Act
        var responsavel = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}");

        // Assert
        responsavel.GetProperty("enderecos").GetArrayLength().Should().Be(1);
        responsavel.GetProperty("contatos").GetArrayLength().Should().Be(1);
    }

    private async Task<long> CriarEnderecoAsync(long responsavelId)
    {
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/enderecos", DadosDeIntegracao.NovoEndereco());
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }

    private async Task<long> CriarContatoAsync(long responsavelId, string telefone)
    {
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/contatos", DadosDeIntegracao.NovoContato(telefone));
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }

    private static JsonElement BuscarPorId(JsonElement colecao, long id)
        => colecao.EnumerateArray().Single(item => item.GetProperty("id").GetInt64() == id);
}
