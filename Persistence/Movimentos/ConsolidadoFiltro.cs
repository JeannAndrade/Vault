namespace Persistence.Movimentos;

public sealed record ConsolidadoFiltro
{
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }

    public bool TemFiltro => TipoRendaId.HasValue || CorretoraId.HasValue || ProdutoId.HasValue || EmissorId.HasValue;
}

public enum DimensaoConsolidado
{
    TipoRenda,
    Corretora,
    Produto,
    Emissor
}

public sealed record ConsolidadoAgregado(
    int Quantidade,
    decimal TotalAportado,
    decimal TotalLiquido);

public sealed record ConsolidadoGrupoItem(
    Guid Id,
    string Nome,
    int Quantidade,
    decimal TotalAportado,
    decimal TotalLiquido);
