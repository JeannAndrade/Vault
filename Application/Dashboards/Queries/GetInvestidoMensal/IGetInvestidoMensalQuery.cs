using Application.Dashboards.FluxoMensal;

namespace Application.Dashboards.Queries.GetInvestidoMensal;

public interface IGetInvestidoMensalQuery
{
    Task<List<InvestidoMensalModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken);
}
