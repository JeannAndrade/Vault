using System.Globalization;

namespace Presentation.Objetivos;

public static class ProjecaoFormatter
{
    public const string TextoBaixaConfianca = "Poucos dados: a rentabilidade foi considerada zero ou limitada. Trate a previsão como estimativa grosseira.";
    private static readonly string[] Meses = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string FormatarCenario(CenarioProjecaoDto? cenario) => cenario?.Situacao switch
    {
        SituacaoProjecao.Prevista when cenario.Meses is int meses && cenario.DataPrevista is DateTime data =>
            $"{meses} {(meses == 1 ? "mês" : "meses")} ({FormatarMesAno(data)})",
        SituacaoProjecao.Atingida => "Meta atingida",
        SituacaoProjecao.MaisDeCemAnos => "Mais de 100 anos",
        _ => "Sem previsão"
    };

    public static string FormatarMesAno(DateTime data) => $"{Meses[data.Month - 1]}/{data.Year}";

    public static string FormatarTaxa(double taxa) => double.IsFinite(taxa) ? taxa.ToString("P2", Cultura) : "—";

    public static string FormatarAporteRealizado(decimal? aporte, int mesesObservados)
    {
        if (!aporte.HasValue) return "Sem histórico de aportes";
        return $"{aporte.Value.ToString("C", Cultura)} por mês (média de {mesesObservados} {(mesesObservados == 1 ? "mês" : "meses")})";
    }
}
