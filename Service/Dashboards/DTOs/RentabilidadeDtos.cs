using Application.Dashboards.Rentabilidade;

namespace Service.Dashboards.DTOs;

public record RentabilidadeFiltroDto
{
    public int Top { get; init; } = RentabilidadeParameters.TopPadrao;
    public OrdemRentabilidade Ordem { get; init; } = OrdemRentabilidade.Maiores;

    public RentabilidadeParameters ToParameters() => new()
    {
        Top = Top,
        Ordem = Ordem
    };
}

public record RentabilidadeItemDto(
    Guid Id,
    string? ProdutoNome,
    string? EmissorNome,
    string? ObjetivoNome,
    string? CorretoraNome,
    decimal ValorAporte,
    decimal ValorLiquidoAtual,
    int DiasInvestido,
    double Acumulada,
    double Diaria,
    double Anualizada)
{
    public static RentabilidadeItemDto FromApplication(RentabilidadeItemModel item) => new(
        item.Id,
        item.ProdutoNome,
        item.EmissorNome,
        item.ObjetivoNome,
        item.CorretoraNome,
        item.ValorAporte,
        item.ValorLiquidoAtual,
        item.DiasInvestido,
        item.Acumulada,
        item.Diaria,
        item.Anualizada);
}

public record RentabilidadeGrupoDto(
    Guid TipoRendaId,
    string? TipoRendaNome,
    List<RentabilidadeItemDto> Itens)
{
    public static RentabilidadeGrupoDto FromApplication(RentabilidadeGrupoModel grupo) => new(
        grupo.TipoRendaId,
        grupo.TipoRendaNome,
        [.. grupo.Itens.Select(RentabilidadeItemDto.FromApplication)]);

    public static List<RentabilidadeGrupoDto> FromApplication(List<RentabilidadeGrupoModel> grupos) =>
        [.. grupos.Select(FromApplication)];
}
