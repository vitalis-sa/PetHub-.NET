using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitalis.Health;

public class BancoDadosHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public BancoDadosHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();

        try
        {
            bool conexaoOk = await _context.Database.CanConnectAsync(cancellationToken);

            cronometro.Stop();

            if (conexaoOk)
            {
                return HealthCheckResult.Healthy(
                    "Conexão com o Banco de Dados estabelecida com sucesso.",
                    data: new Dictionary<string, object> { { "LatenciaMs", cronometro.ElapsedMilliseconds } });
            }

            return HealthCheckResult.Unhealthy(
                "Falha ao conectar no Banco de Dados (Oracle).",
                data: new Dictionary<string, object> { { "LatenciaMs", cronometro.ElapsedMilliseconds } });
        }
        catch (Exception ex)
        {
            cronometro.Stop();

            return HealthCheckResult.Unhealthy(
                "Falha ao conectar no Banco de Dados (Oracle).",
                ex,
                new Dictionary<string, object> { { "LatenciaMs", cronometro.ElapsedMilliseconds } });
        }
    }
}
