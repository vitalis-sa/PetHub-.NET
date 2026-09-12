using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitalis.Health;

public static class RespostaHealthCheck
{
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public const string ContentType = "application/json";

    public static Task EscreverAsync(HttpContext contexto, HealthReport relatorio)
    {
        contexto.Response.ContentType = $"{ContentType}; charset=utf-8";

        var corpo = new
        {
            status = relatorio.Status.ToString(),
            duracaoTotalMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds, 2),
            verificadoEm = DateTimeOffset.UtcNow,

            verificacoes = relatorio.Entries.Select(entrada => new
            {
                nome = entrada.Key,
                status = entrada.Value.Status.ToString(),
                descricao = entrada.Value.Description,
                duracaoMs = Math.Round(entrada.Value.Duration.TotalMilliseconds, 2),
                dados = entrada.Value.Data,
                erro = entrada.Value.Exception?.Message
            })
        };

        return contexto.Response.WriteAsync(JsonSerializer.Serialize(corpo, OpcoesJson));
    }
}
