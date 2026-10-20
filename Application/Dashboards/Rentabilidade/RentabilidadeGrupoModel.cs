namespace Application.Dashboards.Rentabilidade;

public record RentabilidadeGrupoModel(
    Guid TipoRendaId,
    string? TipoRendaNome,
    List<RentabilidadeItemModel> Itens);
