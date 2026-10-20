using System.Globalization;

namespace Presentation.Dashboards;

public static class RentabilidadeFormatter
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Percentual(double fracao, int casas = 2)
    {
        var arredondada = Math.Round(fracao, casas + 2, MidpointRounding.AwayFromZero);
        if (arredondada == 0)
            arredondada = 0;

        return (arredondada * 100).ToString($"N{casas}", Cultura) + "%";
    }

    public static string ClasseDaRentabilidade(double fracao)
    {
        var arredondada = Math.Round(fracao, 4, MidpointRounding.AwayFromZero);
        return arredondada switch
        {
            > 0 => "text-success",
            < 0 => "text-danger",
            _ => ""
        };
    }
}
