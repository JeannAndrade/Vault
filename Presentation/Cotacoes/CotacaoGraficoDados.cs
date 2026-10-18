using System.Globalization;
using System.Text.Json;

namespace Presentation.Cotacoes;

public static class CotacaoGraficoDados
{
    public static string ToJson(string papel, IEnumerable<CotacaoPontoDto> pontos) => JsonSerializer.Serialize(new
    {
        papel,
        rotulos = pontos.Select(p => p.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).ToArray(),
        valores = pontos.Select(p => p.Valor).ToArray()
    });
}
