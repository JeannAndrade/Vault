using Application.Dashboards.Consolidado;
using Persistence.Movimentos;

namespace Service.Dashboards.DTOs;

public record ConsolidadoFiltroDto
{
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }
    public DimensaoConsolidado? AgruparPor { get; init; }

    public ConsolidadoParameters ToParameters() => new()
    {
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId,
        AgruparPor = AgruparPor
    };
}

public record ConsolidadoLinhaDto(
    Guid? Id,
    string? Nome,
    int Quantidade,
    decimal TotalAportado,
    decimal TotalLiquido,
    decimal PercentualLiquido)
{
    public static ConsolidadoLinhaDto FromApplication(ConsolidadoLinhaModel modelo) => new(
        modelo.Id, modelo.Nome, modelo.Quantidade, modelo.TotalAportado, modelo.TotalLiquido, modelo.PercentualLiquido);

    public static List<ConsolidadoLinhaDto> FromApplication(List<ConsolidadoLinhaModel> modelos) =>
        [.. modelos.Select(FromApplication)];
}

public record ConsolidadoDto(
    ConsolidadoLinhaDto Total,
    List<ConsolidadoLinhaDto> Grupos)
{
    public static ConsolidadoDto FromApplication(ConsolidadoModel modelo) => new(
        ConsolidadoLinhaDto.FromApplication(modelo.Total),
        ConsolidadoLinhaDto.FromApplication(modelo.Grupos ?? []));
}
