using Presentation.Dashboards;

namespace Presentation.Pages.Dashboards;

public sealed class ConsolidadoFiltroInput
{
    public Guid? TipoRendaId { get; set; }
    public Guid? CorretoraId { get; set; }
    public Guid? ProdutoId { get; set; }
    public Guid? EmissorId { get; set; }

    public bool HasValue => TipoRendaId.HasValue || CorretoraId.HasValue || ProdutoId.HasValue || EmissorId.HasValue;

    public ConsolidadoFiltro ToFiltro() => new()
    {
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId
    };
}
