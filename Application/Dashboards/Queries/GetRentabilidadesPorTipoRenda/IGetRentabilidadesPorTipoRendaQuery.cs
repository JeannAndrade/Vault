using Application.Dashboards.Rentabilidade;

namespace Application.Dashboards.Queries.GetRentabilidadesPorTipoRenda;

public interface IGetRentabilidadesPorTipoRendaQuery
{
    Task<List<RentabilidadeGrupoModel>> ExecuteAsync(
        Guid ownerId,
        RentabilidadeParameters parametros,
        CancellationToken cancellationToken);
}
