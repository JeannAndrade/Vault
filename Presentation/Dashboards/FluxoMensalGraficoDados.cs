using System.Text.Json;

namespace Presentation.Dashboards;

public static class FluxoMensalGraficoDados
{
    public static string RetornoParaJson(IEnumerable<RetornoMensalDto> meses) => JsonSerializer.Serialize(new
    {
        rotulos = meses.Select(m => FluxoMensalFormatter.FormatarMes(m.Mes)).ToArray(),
        valores = meses.Select(m => m.Total).ToArray()
    });

    public static string InvestidoParaJson(IEnumerable<InvestidoMensalDto> meses) => JsonSerializer.Serialize(new
    {
        rotulos = meses.Select(m => FluxoMensalFormatter.FormatarMes(m.Mes)).ToArray(),
        novo = meses.Select(m => m.Novo).ToArray(),
        reinvestimento = meses.Select(m => m.Reinvestimento).ToArray()
    });
}
