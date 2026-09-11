using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Vitalis.Observabilidade;

public static class AplicacaoMetricas
{
    public const string NomeServico = "Vitalis.API";

    public static readonly Meter MeterAplicacao = new(NomeServico, "1.0.0");

    public static readonly Counter<long> ResponsaveisCadastradosContador =
        MeterAplicacao.CreateCounter<long>(
            name: "responsaveis_cadastrados_total",
            unit: "{responsaveis}",
            description: "Contagem total de responsáveis cadastrados na API");

    public static readonly Counter<long> LembretesCriadosContador =
        MeterAplicacao.CreateCounter<long>(
            name: "lembretes_criados_total",
            unit: "{lembretes}",
            description: "Contagem total de lembretes criados na API");

    public static readonly ActivitySource ActivitySourceAplicacao = new(NomeServico);
}
