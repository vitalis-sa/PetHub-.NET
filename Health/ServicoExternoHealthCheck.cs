using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitalis.Health;

public class ServicoExternoHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ServicoExternoHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var url = _configuration["ServicosExternos:PethubJava"];

        if (string.IsNullOrWhiteSpace(url))
            return HealthCheckResult.Unhealthy("URL do serviço externo não configurada.");

        var cronometro = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient();
            var resposta = await client.GetAsync(url, cancellationToken);

            cronometro.Stop();

            if (resposta.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy(
                    "Serviço externo (pethub-java) disponível.",
                    data: new Dictionary<string, object>
                    {
                        { "LatenciaMs", cronometro.ElapsedMilliseconds },
                        { "StatusCode", (int)resposta.StatusCode }
                    });
            }

            return HealthCheckResult.Unhealthy(
                $"Serviço externo (pethub-java) respondeu {(int)resposta.StatusCode}.",
                data: new Dictionary<string, object>
                {
                    { "LatenciaMs", cronometro.ElapsedMilliseconds },
                    { "StatusCode", (int)resposta.StatusCode }
                });
        }
        catch (Exception ex)
        {
            cronometro.Stop();

            return HealthCheckResult.Unhealthy(
                "Serviço externo (pethub-java) inacessível.",
                ex,
                new Dictionary<string, object> { { "LatenciaMs", cronometro.ElapsedMilliseconds } });
        }
    }
}
