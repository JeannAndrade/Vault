namespace Presentation.Movimentos;

public sealed record MovimentoDto
{
    public Guid Id { get; set; }
    public Guid ObjetivoId { get; set; }
    public string? ObjetivoNome { get; set; }
    public Guid TipoRendaId { get; set; }
    public string? TipoRendaNome { get; set; }
    public Guid CorretoraId { get; set; }
    public string? CorretoraNome { get; set; }
    public Guid ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public Guid EmissorId { get; set; }
    public string? EmissorNome { get; set; }
    public string? RentabilidadeContratada { get; set; }
    public string? CotacaoNaCompra { get; set; }
    public DateTime DataInvestimento { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorAporte { get; set; }
    public bool EhReinvestimento { get; set; }
    public bool EstaAtivo { get; set; }
    public decimal ValorLiquidoAtual { get; set; }
}
