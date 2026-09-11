using System.Net;
using FluentAssertions;
using Vitalis.Middlewares;
using Vitalis.Tests.Integration.Fixtures;
using Xunit;

namespace Vitalis.Tests.Integration.Monitoramento;

[Collection(VitalisApiCollection.Name)]
public class MonitoramentoEndpointsTests : IDisposable
{
    private readonly HttpClient _client;

    public MonitoramentoEndpointsTests(VitalisWebApplicationFactory factory)
        => _client = factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetHealth_ServicoExternoIndisponivel_DeveRetornar503ComStatusUnhealthy()
    {
        // Act
        var resposta = await _client.GetAsync("/health");
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        corpo.Should().Be("Unhealthy");
    }

    [Fact]
    public async Task GetHealth_EmQualquerChamada_DeveResponderEmTextoSimples()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/health");

        // Assert
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Requisicao_ComHeaderDeCorrelacao_DeveEcoarOMesmoIdentificadorNaResposta()
    {
        // Arrange
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/responsavel");
        requisicao.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeader, "correlacao-de-teste-42");

        // Act
        var resposta = await _client.SendAsync(requisicao);

        // Assert
        resposta.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeader)
            .Single().Should().Be("correlacao-de-teste-42");
    }

    [Fact]
    public async Task Requisicao_SemHeaderDeCorrelacao_DeveGerarEDevolverUmIdentificador()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/responsavel");

        // Assert
        var correlationId = resposta.Headers
            .GetValues(CorrelationIdMiddleware.CorrelationIdHeader).Single();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Requisicoes_EmSequencia_DevemReceberIdentificadoresDeCorrelacaoDistintos()
    {
        // Arrange & Act
        var primeira = await _client.GetAsync("/api/responsavel");
        var segunda = await _client.GetAsync("/api/responsavel");

        // Assert
        primeira.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeader).Single()
            .Should().NotBe(segunda.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeader).Single());
    }

    [Fact]
    public async Task GetSwagger_ComAApiNoAr_DeveRetornar200ComODocumentoOpenApi()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.Should().Contain("Vitalis API");
    }
}
