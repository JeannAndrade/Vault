namespace Presentation.Dashboards;

public sealed record RentabilidadeItemDto
{
    public Guid Id { get; init; }
    public string? ProdutoNome { get; init; }
    public string? EmissorNome { get; init; }
    public string? ObjetivoNome { get; init; }
    public string? CorretoraNome { get; init; }
    public decimal ValorAporte { get; init; }
    public decimal ValorLiquidoAtual { get; init; }
    public int DiasInvestido { get; init; }
    public double Acumulada { get; init; }
    public double Diaria { get; init; }
    public double Anualizada { get; init; }
}

public sealed record RentabilidadeGrupoDto
{
    public Guid TipoRendaId { get; init; }
    public string? TipoRendaNome { get; init; }
    public List<RentabilidadeItemDto> Itens { get; init; } = [];
}
