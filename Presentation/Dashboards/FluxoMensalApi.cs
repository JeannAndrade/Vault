using LumiaFoundation.Http.Client.Services;

namespace Presentation.Dashboards;

public sealed class FluxoMensalApi(IApiConnection connection) : IFluxoMensalApi
{
    private const string RetornoPath = "api/dashboards/retorno-mensal";
    private const string InvestidoPath = "api/dashboards/investido-mensal";

    public async Task<List<RetornoMensalDto>> GetRetornoMensalAsync(CancellationToken cancellationToken) =>
        await connection.SendAsync<List<RetornoMensalDto>>(HttpMethod.Get, RetornoPath, ct: cancellationToken) ?? [];

    public async Task<List<InvestidoMensalDto>> GetInvestidoMensalAsync(CancellationToken cancellationToken) =>
        await connection.SendAsync<List<InvestidoMensalDto>>(HttpMethod.Get, InvestidoPath, ct: cancellationToken) ?? [];
}
