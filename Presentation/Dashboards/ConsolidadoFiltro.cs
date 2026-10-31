namespace Presentation.Dashboards;

public sealed record ConsolidadoFiltro
{
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }
}
