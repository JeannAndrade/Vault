using System.ComponentModel.DataAnnotations;
using Persistence.Movimentos;

namespace Application.Dashboards.Consolidado;

public record ConsolidadoParameters
{
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }

    [EnumDataType(typeof(DimensaoConsolidado))]
    public DimensaoConsolidado? AgruparPor { get; init; }

    public ConsolidadoFiltro ToFiltro() => new()
    {
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId
    };
}

public record ConsolidadoLinhaModel(
    Guid? Id,
    string? Nome,
    int Quantidade,
    decimal TotalAportado,
    decimal TotalLiquido,
    decimal PercentualLiquido);

public record ConsolidadoModel(
    ConsolidadoLinhaModel Total,
    List<ConsolidadoLinhaModel> Grupos);
