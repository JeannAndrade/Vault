namespace Domain.Movimentos.Distribuicao;

public sealed record ResultadoDistribuicao
{
    private ResultadoDistribuicao(
        IReadOnlyList<AporteDistribuido>? aportes,
        decimal totalAportado,
        decimal valorLiquidoTotal,
        decimal ganhoTotal,
        double? taxaEfetivaAnual,
        ErroDistribuicao? erro)
    {
        Aportes = aportes;
        TotalAportado = totalAportado;
        ValorLiquidoTotal = valorLiquidoTotal;
        GanhoTotal = ganhoTotal;
        TaxaEfetivaAnual = taxaEfetivaAnual;
        Erro = erro;
    }

    public IReadOnlyList<AporteDistribuido>? Aportes { get; }
    public decimal TotalAportado { get; }
    public decimal ValorLiquidoTotal { get; }
    public decimal GanhoTotal { get; }
    public double? TaxaEfetivaAnual { get; }
    public ErroDistribuicao? Erro { get; }
    public bool Sucesso => Erro is null;

    public static ResultadoDistribuicao Ok(
        IReadOnlyList<AporteDistribuido> aportes,
        decimal totalAportado,
        decimal valorLiquidoTotal,
        decimal ganhoTotal,
        double? taxaEfetivaAnual) =>
        new(aportes, totalAportado, valorLiquidoTotal, ganhoTotal, taxaEfetivaAnual, null);

    public static ResultadoDistribuicao Falha(ErroDistribuicao erro) =>
        new(null, 0, 0, 0, null, erro);
}
