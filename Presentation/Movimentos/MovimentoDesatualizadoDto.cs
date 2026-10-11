namespace Presentation.Movimentos;

public sealed record MovimentoDesatualizadoDto
{
    public Guid Id { get; init; }
    public string? ObjetivoNome { get; init; }
    public string? CorretoraNome { get; init; }
    public string? ProdutoNome { get; init; }
    public string? EmissorNome { get; init; }
    public DateTime? DataVencimento { get; init; }
    public decimal ValorLiquidoAtual { get; init; }
}
