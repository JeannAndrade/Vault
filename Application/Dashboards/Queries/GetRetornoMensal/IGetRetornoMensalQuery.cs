using Application.Dashboards.FluxoMensal;

namespace Application.Dashboards.Queries.GetRetornoMensal;

public interface IGetRetornoMensalQuery
{
    Task<List<RetornoMensalModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken);
}
