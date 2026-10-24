namespace Presentation.Dashboards;

public interface IFluxoMensalApi
{
    Task<List<RetornoMensalDto>> GetRetornoMensalAsync(CancellationToken cancellationToken);
    Task<List<InvestidoMensalDto>> GetInvestidoMensalAsync(CancellationToken cancellationToken);
}
