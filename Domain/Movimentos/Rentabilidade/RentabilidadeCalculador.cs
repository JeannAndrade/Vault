namespace Domain.Movimentos.Rentabilidade;

public static class RentabilidadeCalculador
{
    public const int DiasMinimosParaRanking = 30;
    public const int DiasPorAno = 365;

    public static ResultadoRentabilidade? Calcular(
        decimal valorAporte,
        decimal valorLiquidoAtual,
        DateTime dataInvestimento,
        DateTime dataAtualizacaoValorLiquido)
    {
        if (!EntradasValidas(valorAporte, dataInvestimento, dataAtualizacaoValorLiquido))
        {
            return null;
        }

        var diasDecorridos = CalcularDiasDecorridos(dataInvestimento, dataAtualizacaoValorLiquido);
        if (diasDecorridos <= 0)
        {
            return null;
        }

        var fatorRentabilidade = CalcularFatorRentabilidade(valorLiquidoAtual, valorAporte);
        if (fatorRentabilidade is null)
        {
            return null;
        }

        var rentabilidadeAcumulada = CalcularRentabilidadeAcumulada(fatorRentabilidade.Value);
        var rentabilidadeDiaria = CalcularRentabilidadeDiaria(fatorRentabilidade.Value, diasDecorridos);
        var rentabilidadeAnualizada = CalcularRentabilidadeAnualizada(fatorRentabilidade.Value, diasDecorridos);

        return SaoValoresFinitos(rentabilidadeAcumulada, rentabilidadeDiaria, rentabilidadeAnualizada)
            ? new ResultadoRentabilidade(rentabilidadeAcumulada, rentabilidadeDiaria, rentabilidadeAnualizada, diasDecorridos)
            : null;
    }

    private static bool EntradasValidas(
        decimal valorAporte,
        DateTime dataInvestimento,
        DateTime dataAtualizacaoValorLiquido)
        => valorAporte > 0 && dataInvestimento <= dataAtualizacaoValorLiquido;

    private static int CalcularDiasDecorridos(DateTime dataInvestimento, DateTime dataAtualizacao)
        => (dataAtualizacao.Date - dataInvestimento.Date).Days;

    private static double? CalcularFatorRentabilidade(decimal valorLiquidoAtual, decimal valorAporte)
    {
        var fator = (double)valorLiquidoAtual / (double)valorAporte;
        return fator >= 0 ? fator : null;
    }

    private static double CalcularRentabilidadeAcumulada(double fatorRentabilidade)
        => fatorRentabilidade - 1;

    private static double CalcularRentabilidadeDiaria(double fatorRentabilidade, int diasDecorridos)
        => Math.Pow(fatorRentabilidade, 1.0 / diasDecorridos) - 1;

    private static double CalcularRentabilidadeAnualizada(double fatorRentabilidade, int diasDecorridos)
        => Math.Pow(fatorRentabilidade, DiasPorAno / (double)diasDecorridos) - 1;

    private static bool SaoValoresFinitos(params double[] valores)
        => valores.All(double.IsFinite);
}
