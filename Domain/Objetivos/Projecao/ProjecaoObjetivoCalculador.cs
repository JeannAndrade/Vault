namespace Domain.Objetivos.Projecao;

public static class ProjecaoObjetivoCalculador
{
    public const int MesesJanelaAporte = 12;
    public const int DiasMinimosParaTaxa = 90;
    public const double TaxaAnualMaxima = 0.30;
    public const int MesesMaximos = 1200;
    public const int DiasPorAno = 365;

    public static ResultadoProjecao Calcular(IReadOnlyList<MovimentoParaProjecao> movimentos, decimal meta, decimal aporteMensalPlanejado, DateTime hoje)
    {
        ArgumentNullException.ThrowIfNull(movimentos);
        var data = hoje.Date;
        var saldo = Arredondar(movimentos.Where(m => m.CompoeSaldo).Sum(m => Positivo(m.ValorLiquidoAtual)));
        var pendente = Arredondar(movimentos.Where(m => m.CompoeSaldo && !m.EstaAtivo).Sum(m => Positivo(m.ValorLiquidoAtual)));
        var (aporteRealizado, meses) = CalcularAporte(movimentos, data);
        var (anual, mensal, baixa) = CalcularTaxa(movimentos);
        var planejado = Math.Max(aporteMensalPlanejado, 0);
        return new(saldo, pendente, aporteRealizado, meses, planejado, anual, mensal, baixa,
            CalcularCenario(saldo, meta, aporteRealizado, 0, data),
            CalcularCenario(saldo, meta, aporteRealizado, mensal, data),
            CalcularCenario(saldo, meta, planejado, mensal, data));
    }

    private static (decimal?, int) CalcularAporte(IReadOnlyList<MovimentoParaProjecao> ms, DateTime hoje)
    {
        var elegiveis = ms.Where(m => !m.EhReinvestimento && m.ValorAporte > 0 && m.DataInvestimento.Date <= hoje)
            .ToList();
        if (elegiveis.Count == 0) return (null, 0);
        var atual = IndiceMes(hoje);
        var inicio = Math.Max(atual - MesesJanelaAporte, elegiveis.Min(m => IndiceMes(m.DataInvestimento)));
        var meses = atual - inicio;
        if (meses == 0) return (null, 0);
        var soma = elegiveis.Where(m => IndiceMes(m.DataInvestimento) >= inicio && IndiceMes(m.DataInvestimento) < atual).Sum(m => m.ValorAporte);
        return (Arredondar(soma / meses), meses);
    }

    private static (double, double, bool) CalcularTaxa(IReadOnlyList<MovimentoParaProjecao> ms)
    {
        var ativos = ms.Where(m => m.EstaAtivo && m.ValorAporte > 0).ToList();
        if (ativos.Count == 0) return (0, 0, true);
        var aportes = ativos.Sum(m => m.ValorAporte);
        var tempo = ativos.Sum(m => (double)m.ValorAporte * Math.Max(0, (m.DataAtualizacaoValorLiquido.Date - m.DataInvestimento.Date).Days)) / (double)aportes;
        if (tempo < DiasMinimosParaTaxa) return (0, 0, true);
        var fator = (double)ativos.Sum(m => Positivo(m.ValorLiquidoAtual)) / (double)aportes;
        var anual = Math.Pow(fator, DiasPorAno / tempo) - 1;
        if (!double.IsFinite(anual)) return (0, 0, true);
        anual = Math.Clamp(anual, 0, TaxaAnualMaxima);
        var mensal = Math.Pow(1 + anual, 1.0 / 12) - 1;
        return double.IsFinite(mensal) ? (anual, mensal, false) : (0, 0, true);
    }

    private static CenarioProjecao CalcularCenario(decimal saldo, decimal meta, decimal? aporte, double taxa, DateTime hoje)
    {
        if (meta <= 0) return CenarioProjecao.SemPrevisao();
        if (saldo >= meta) return CenarioProjecao.Atingida();
        if (!aporte.HasValue) return CenarioProjecao.SemPrevisao();
        var s = (double)saldo; var m = (double)meta; var a = (double)aporte.Value;
        var n = taxa == 0 ? (a == 0 ? double.NaN : (m - s) / a) : Math.Log((m * taxa + a) / (s * taxa + a)) / Math.Log(1 + taxa);
        if (!double.IsFinite(n) || n <= 0) return CenarioProjecao.SemPrevisao();
        var meses = (int)Math.Ceiling(Math.Round(n, 9));
        return meses > MesesMaximos ? CenarioProjecao.MaisDeCemAnos() : CenarioProjecao.Prevista(meses, new DateTime(hoje.Year, hoje.Month, 1).AddMonths(meses));
    }

    private static int IndiceMes(DateTime data) => data.Year * 12 + data.Month;
    private static decimal Positivo(decimal valor) => Math.Max(valor, 0);
    private static decimal Arredondar(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
