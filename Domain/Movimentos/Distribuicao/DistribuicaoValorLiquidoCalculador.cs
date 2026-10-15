namespace Domain.Movimentos.Distribuicao;

public static class DistribuicaoValorLiquidoCalculador
{
    public static ResultadoDistribuicao Calcular(
        IReadOnlyList<AporteParaDistribuicao> aportes,
        decimal valorLiquidoTotal,
        DateTime dataReferencia)
    {
        ValidarParametros(aportes, valorLiquidoTotal);

        if (aportes.Count == 0)
        {
            return ResultadoDistribuicao.Falha(ErroDistribuicao.SemAportes);
        }

        var aportesOrdenados = OrdenarAportes(aportes);
        var temposEmAnos = CalcularTemposEmAnos(aportesOrdenados, dataReferencia);
        var totalAportado = aportesOrdenados.Sum(aporte => aporte.ValorAporte);
        var ganhoTotal = valorLiquidoTotal - totalAportado;

        if (valorLiquidoTotal == 0 || ganhoTotal == 0)
        {
            return CriarResultadoSemCalculo(aportesOrdenados, totalAportado, valorLiquidoTotal, ganhoTotal);
        }

        var tempoMedio = CalcularTempoMedioPonderado(aportesOrdenados, temposEmAnos, totalAportado);
        if (tempoMedio == 0)
        {
            return ResultadoDistribuicao.Falha(ErroDistribuicao.SemBaseDeTempo);
        }

        var taxa = CalcularTaxaRetorno(valorLiquidoTotal, totalAportado, tempoMedio);
        var (ganhosProjetados, somaGanhosProjetados) = CalcularGanhosProjetados(aportesOrdenados, temposEmAnos, taxa);

        if (!ValidarValoresFinitos(taxa, ganhosProjetados, somaGanhosProjetados))
        {
            return ResultadoDistribuicao.Falha(ErroDistribuicao.CalculoInvalido);
        }

        var ganhos = DistribuirGanhosProporcionais(ganhoTotal, aportesOrdenados, ganhosProjetados, somaGanhosProjetados);
        var resultadoAportes = MontarAportesDistribuidos(aportesOrdenados, ganhos);

        if (resultadoAportes.Any(aporte => aporte.ValorLiquido < 0))
        {
            return ResultadoDistribuicao.Falha(ErroDistribuicao.ValorNegativo);
        }

        return ResultadoDistribuicao.Ok(resultadoAportes, totalAportado, valorLiquidoTotal, ganhoTotal, taxa);
    }

    private static void ValidarParametros(IReadOnlyList<AporteParaDistribuicao> aportes, decimal valorLiquidoTotal)
    {
        ArgumentNullException.ThrowIfNull(aportes);
        ArgumentOutOfRangeException.ThrowIfNegative(valorLiquidoTotal, nameof(valorLiquidoTotal));

        if (aportes.Any(aporte => aporte.ValorAporte <= 0))
        {
            throw new ArgumentException("Todos os aportes devem ter valor maior que zero.", nameof(aportes));
        }
    }

    private static AporteParaDistribuicao[] OrdenarAportes(IReadOnlyList<AporteParaDistribuicao> aportes) =>
        aportes.OrderBy(aporte => aporte.DataInvestimento)
               .ThenBy(aporte => aporte.Id)
               .ToArray();

    private static double[] CalcularTemposEmAnos(AporteParaDistribuicao[] aportes, DateTime dataReferencia) =>
        aportes.Select(aporte => Math.Max(0, (dataReferencia.Date - aporte.DataInvestimento.Date).Days / 365.0))
               .ToArray();

    private static double CalcularTempoMedioPonderado(
        AporteParaDistribuicao[] aportes,
        double[] tempos,
        decimal totalAportado) =>
        aportes.Select((aporte, indice) => (double)aporte.ValorAporte * tempos[indice]).Sum() / (double)totalAportado;

    private static double CalcularTaxaRetorno(decimal valorLiquidoTotal, decimal totalAportado, double tempoMedio) =>
        Math.Pow((double)(valorLiquidoTotal / totalAportado), 1 / tempoMedio) - 1;

    private static (double[] GanhosProjetados, double Soma) CalcularGanhosProjetados(
        AporteParaDistribuicao[] aportes,
        double[] tempos,
        double taxa)
    {
        var ganhosProjetados = aportes.Select((aporte, indice) =>
            (double)aporte.ValorAporte * (Math.Pow(1 + taxa, tempos[indice]) - 1)).ToArray();

        return (ganhosProjetados, ganhosProjetados.Sum());
    }

    private static bool ValidarValoresFinitos(double taxa, double[] ganhosProjetados, double somaGanhosProjetados) =>
        double.IsFinite(taxa) &&
        double.IsFinite(somaGanhosProjetados) &&
        somaGanhosProjetados != 0 &&
        ganhosProjetados.All(double.IsFinite);

    private static decimal[] DistribuirGanhosProporcionais(
        decimal ganhoTotal,
        AporteParaDistribuicao[] aportes,
        double[] ganhosProjetados,
        double somaGanhosProjetados)
    {
        var ganhos = new decimal[aportes.Length];
        var ganhosCalculados = 0m;

        for (var indice = 0; indice < ganhos.Length - 1; indice++)
        {
            ganhos[indice] = decimal.Round(
                ganhoTotal * (decimal)(ganhosProjetados[indice] / somaGanhosProjetados),
                2,
                MidpointRounding.AwayFromZero);
            ganhosCalculados += ganhos[indice];
        }

        ganhos[^1] = ganhoTotal - ganhosCalculados;
        return ganhos;
    }

    private static AporteDistribuido[] MontarAportesDistribuidos(
        AporteParaDistribuicao[] aportes,
        decimal[] ganhos) =>
        aportes.Select((aporte, indice) => new AporteDistribuido(
            aporte.Id,
            aporte.ValorAporte,
            ganhos[indice],
            aporte.ValorAporte + ganhos[indice])).ToArray();

    private static ResultadoDistribuicao CriarResultadoSemCalculo(
        AporteParaDistribuicao[] aportes,
        decimal totalAportado,
        decimal valorLiquidoTotal,
        decimal ganhoTotal)
    {
        var taxa = valorLiquidoTotal == 0 ? -1 : 0;

        var aportesDistribuidos = aportes.Select(aporte => new AporteDistribuido(
            aporte.Id,
            aporte.ValorAporte,
            valorLiquidoTotal == 0 ? -aporte.ValorAporte : 0,
            valorLiquidoTotal == 0 ? 0 : aporte.ValorAporte)).ToArray();

        return ResultadoDistribuicao.Ok(aportesDistribuidos, totalAportado, valorLiquidoTotal, ganhoTotal, taxa);
    }
}
