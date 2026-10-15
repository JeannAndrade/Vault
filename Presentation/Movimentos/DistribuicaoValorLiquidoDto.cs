namespace Presentation.Movimentos;

public sealed record DistribuicaoValorLiquidoSolicitacaoDto
{
    public Guid TipoRendaId { get; init; }
    public Guid CorretoraId { get; init; }
    public Guid ProdutoId { get; init; }
    public Guid EmissorId { get; init; }
    public decimal ValorLiquidoTotal { get; init; }
}

public sealed record AporteDistribuidoDto(Guid Id, string? ObjetivoNome, DateTime DataInvestimento,
    decimal ValorAporte, decimal ValorLiquidoAtual, decimal NovoValorLiquido, decimal Ganho);

public sealed record DistribuicaoValorLiquidoDto
{
    public List<AporteDistribuidoDto> Aportes { get; init; } = [];
    public decimal TotalAportado { get; init; }
    public decimal GanhoTotal { get; init; }
    public decimal ValorLiquidoTotal { get; init; }
    public double? TaxaEfetivaAnual { get; init; }
    public string? Aviso { get; init; }
    public bool PodeConfirmar { get; init; }
}
