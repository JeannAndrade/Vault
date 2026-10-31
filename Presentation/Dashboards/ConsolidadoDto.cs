namespace Presentation.Dashboards;

public sealed record ConsolidadoLinhaDto
{
    public Guid? Id { get; init; }
    public string? Nome { get; init; }
    public int Quantidade { get; init; }
    public decimal TotalAportado { get; init; }
    public decimal TotalLiquido { get; init; }
    public decimal PercentualLiquido { get; init; }
}

public sealed record ConsolidadoDto
{
    public ConsolidadoLinhaDto Total { get; init; } = new();
    public List<ConsolidadoLinhaDto> Grupos { get; init; } = [];
}
