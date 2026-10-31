using Application.Dashboards.Consolidado;

namespace Application.Dashboards.Queries.GetConsolidado;

public interface IGetConsolidadoQuery
{
    Task<ConsolidadoModel> ExecuteAsync(Guid ownerId, ConsolidadoParameters parametros, CancellationToken cancellationToken);
}
