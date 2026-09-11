using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Vitalis.Models;
using Vitalis.Tests.Integration.Fixtures;
using Xunit;

namespace Vitalis.Tests.Integration.Endpoints;

[Collection(VitalisApiCollection.Name)]
public class LembreteEndpointsTests : IDisposable
{
    private readonly VitalisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LembreteEndpointsTests(VitalisWebApplicationFactory factory)
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
    public async Task PostLembrete_ServiceTokenValido_DeveRetornar201EPersistirOLembrete()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId));
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        criado.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostLembrete_SemServiceToken_DeveRetornar401()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLembrete_ServiceTokenInvalido_DeveRetornar401()
    {
        // Arrange
        using var clientComTokenErrado = _factory.CreateClientComServiceToken("token-invalido");

        // Act
        var resposta = await clientComTokenErrado.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId: 1));

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLembrete_QuandoCriado_DeveNascerComStatusPendente()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var lembreteId = await CriarLembreteAsync(clientAutenticado, responsavelId);
        var lembretes = await _client.GetFromJsonAsync<JsonElement>("/api/lembretes");

        // Assert
        var lembrete = lembretes.EnumerateArray().Single(l => l.GetProperty("id").GetInt64() == lembreteId);
        lembrete.GetProperty("status").GetInt32().Should().Be((int)StatusLembrete.PENDENTE);
    }

    [Fact]
    public async Task GetLembretes_SemLembretesCadastrados_DeveRetornar200ComListaVazia()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/lembretes");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetLembreteById_IdInexistente_DeveRetornar404()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/lembretes/999999");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLembretesPorResponsavel_LembretesDeVariosResponsaveis_DeveRetornarApenasOsDoResponsavel()
    {
        // Arrange
        var primeiro = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "11111111111", email: "a@pethub.com");
        var segundo = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "22222222222", email: "b@pethub.com");
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        await CriarLembreteAsync(clientAutenticado, primeiro);
        await CriarLembreteAsync(clientAutenticado, segundo);

        // Act
        var lembretes = await _client.GetFromJsonAsync<JsonElement>($"/api/lembretes/responsavel/{primeiro}");

        // Assert
        lembretes.GetArrayLength().Should().Be(1);
        lembretes[0].GetProperty("responsavelId").GetInt64().Should().Be(primeiro);
    }

    [Fact]
    public async Task GetLembretesPorResponsavelETipo_TipoInformado_DeveRetornarApenasOsDaqueleTipo()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        await CriarLembreteAsync(clientAutenticado, responsavelId, TipoLembrete.VACINA);
        await CriarLembreteAsync(clientAutenticado, responsavelId, TipoLembrete.EXAME);

        // Act
        var lembretes = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/lembretes/responsavel/{responsavelId}/tipo/VACINA");

        // Assert
        lembretes.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetLembretesPorResponsavelETipo_TipoInexistente_DeveRetornar400()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.GetAsync($"/api/lembretes/responsavel/{responsavelId}/tipo/BANHO");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchStatus_LembreteExistente_DeveRetornar204EAtualizarOStatus()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        var lembreteId = await CriarLembreteAsync(clientAutenticado, responsavelId);

        // Act
        var resposta = await _client.PatchAsJsonAsync($"/api/lembretes/{lembreteId}/status",
            new { status = (int)StatusLembrete.ENVIADO });
        var lembrete = await _client.GetFromJsonAsync<JsonElement>($"/api/lembretes/{lembreteId}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        lembrete.GetProperty("status").GetInt32().Should().Be((int)StatusLembrete.ENVIADO);
    }

    [Fact]
    public async Task PatchStatus_LembreteInexistente_DeveRetornar404()
    {
        // Arrange & Act
        var resposta = await _client.PatchAsJsonAsync("/api/lembretes/999999/status",
            new { status = (int)StatusLembrete.FALHOU });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLembrete_LembreteExistente_DeveRetornar204ERemoverORegistro()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        var lembreteId = await CriarLembreteAsync(clientAutenticado, responsavelId);

        // Act
        var resposta = await _client.DeleteAsync($"/api/lembretes/{lembreteId}");
        var consulta = await _client.GetAsync($"/api/lembretes/{lembreteId}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        consulta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<long> CriarLembreteAsync(
        HttpClient clientAutenticado, long responsavelId, TipoLembrete tipo = TipoLembrete.VACINA)
    {
        var resposta = await clientAutenticado.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId, tipo));
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }
}
