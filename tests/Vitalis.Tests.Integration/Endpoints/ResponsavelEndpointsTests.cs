using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Vitalis.Tests.Integration.Fixtures;
using Xunit;

namespace Vitalis.Tests.Integration.Endpoints;

[Collection(VitalisApiCollection.Name)]
public class ResponsavelEndpointsTests : IDisposable
{
    private readonly VitalisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ResponsavelEndpointsTests(VitalisWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.LimparBanco();
        _client = factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PostCadastro_DadosValidos_DeveRetornar201ComLocationDoNovoRecurso()
    {
        // Arrange
        var payload = DadosDeIntegracao.NovoCadastro();

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payload);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        resposta.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task PostCadastro_DadosValidos_NaoDeveRetornarASenhaNoCorpoDaResposta()
    {
        // Arrange
        var payload = DadosDeIntegracao.NovoCadastro();

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payload);
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        corpo.Should().NotContain(DadosDeIntegracao.SenhaPadrao);
        corpo.Should().NotContainEquivalentOf("senha");
    }

    [Fact]
    public async Task PostCadastro_EmailInvalido_DeveRetornar400()
    {
        // Arrange
        var payload = DadosDeIntegracao.NovoCadastro(email: "email-sem-arroba");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payload);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCadastro_PayloadIncompleto_DeveRetornar400()
    {
        // Arrange
        var payloadIncompleto = new { nome = "Somente o nome" };

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payloadIncompleto);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCadastro_JsonMalformado_DeveRetornar400()
    {
        // Arrange
        var conteudo = new StringContent("{ isso não é json }", Encoding.UTF8, "application/json");

        // Act
        var resposta = await _client.PostAsync("/api/responsavel/cadastro", conteudo);

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCadastro_CpfJaCadastrado_DeveRetornar409Conflict()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "99988877766", email: "primeiro@pethub.com");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro",
            DadosDeIntegracao.NovoCadastro(cpf: "99988877766", email: "segundo@pethub.com"));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_ResponsavelCadastrado_DeveRetornar200ComOsDados()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{id}");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("id").GetInt64().Should().Be(id);
        corpo.GetProperty("nome").GetString().Should().Be("Pedro Chasci");
    }

    [Fact]
    public async Task GetById_IdInexistente_DeveRetornar404ComMensagemDeErro()
    {
        // Arrange
        const long idInexistente = 999_999;

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{idInexistente}");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        corpo.GetProperty("erro").GetString().Should().Be("Responsavel não encontrado");
    }

    [Fact]
    public async Task GetAll_VariosResponsaveisCadastrados_DeveRetornar200ComTodosOsRegistros()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "11111111111", email: "a@pethub.com");
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "22222222222", email: "b@pethub.com");

        // Act
        var resposta = await _client.GetAsync("/api/responsavel");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetBuscarPorCpf_SemServiceToken_DeveRetornar401()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "33344455566", email: "c@pethub.com");

        // Act
        var resposta = await _client.GetAsync("/api/responsavel/buscar?cpf=33344455566");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBuscarPorCpf_ServiceTokenInvalido_DeveRetornar401()
    {
        // Arrange
        using var clientComTokenErrado = _factory.CreateClientComServiceToken("token-invalido");

        // Act
        var resposta = await clientComTokenErrado.GetAsync("/api/responsavel/buscar?cpf=33344455566");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBuscarPorCpf_ServiceTokenValidoECpfCadastrado_DeveRetornar200ComOResponsavel()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "44455566677", email: "d@pethub.com");
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.GetAsync("/api/responsavel/buscar?cpf=44455566677");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("cpf").GetString().Should().Be("44455566677");
    }

    [Fact]
    public async Task GetBuscarPorCpf_CpfInexistente_DeveRetornar404()
    {
        // Arrange
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.GetAsync("/api/responsavel/buscar?cpf=00000000000");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostLogin_CredenciaisValidas_DeveRetornar200ComOsDadosDoResponsavel()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "55566677788", email: "login@pethub.com");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/login",
            DadosDeIntegracao.NovoLogin(email: "login@pethub.com"));
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("id").GetInt64().Should().Be(id);
    }

    [Fact]
    public async Task PostLogin_SenhaIncorreta_DeveRetornar401()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "66677788899", email: "senha@pethub.com");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/login",
            DadosDeIntegracao.NovoLogin(email: "senha@pethub.com", senha: "senha-errada"));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutResponsavel_ResponsavelCadastrado_DeveRetornar204EPersistirAsAlteracoes()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "77788899900", email: "put@pethub.com");
        var alteracao = DadosDeIntegracao.NovoCadastro(
            nome: "Nome Atualizado", cpf: "77788899900", email: "atualizado@pethub.com");

        // Act
        var resposta = await _client.PutAsJsonAsync($"/api/responsavel/{id}", alteracao);
        var consulta = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{id}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        consulta.GetProperty("nome").GetString().Should().Be("Nome Atualizado");
    }

    [Fact]
    public async Task DeleteResponsavel_ResponsavelCadastrado_DeveRetornar204ERemoverORegistro()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "88899900011", email: "del@pethub.com");

        // Act
        var resposta = await _client.DeleteAsync($"/api/responsavel/{id}");
        var consulta = await _client.GetAsync($"/api/responsavel/{id}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        consulta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteResponsavel_IdInexistente_DeveRetornar404()
    {
        // Arrange & Act
        var resposta = await _client.DeleteAsync("/api/responsavel/999999");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
