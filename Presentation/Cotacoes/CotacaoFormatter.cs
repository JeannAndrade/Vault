using System.Globalization;

namespace Presentation.Cotacoes;

public static class CotacaoFormatter
{
    public static string Formatar(decimal valor)
    {
        var casas = Math.Clamp((int)BitConverter.GetBytes(decimal.GetBits(valor)[3])[2], 2, 4);
        return valor.ToString($"C{casas}", CultureInfo.GetCultureInfo("pt-BR"));
    }
}
