namespace Presentation.Movimentos;

public sealed record ProximoVencimentoDto
{
    public Guid Id { get; set; }
    public string? ObjetivoNome { get; set; }
    public string? CorretoraNome { get; set; }
    public string? ProdutoNome { get; set; }
    public string? EmissorNome { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorLiquidoAtual { get; set; }
}
