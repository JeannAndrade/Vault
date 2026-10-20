using LumiaFoundation.Http.Client.Services;

namespace Presentation.Dashboards;

public sealed class RentabilidadeApi(IApiConnection connection) : IRentabilidadeApi
{
    private const string BasePath = "api/dashboards/rentabilidade";

    public async Task<List<RentabilidadeGrupoDto>> GetRentabilidadesAsync(
        int top,
        OrdemRentabilidade ordem,
        CancellationToken cancellationToken)
    {
        var ordemTexto = ordem.ToString().ToLowerInvariant();
        var path = FormattableString.Invariant($"{BasePath}?top={top}&ordem={ordemTexto}");
        return await connection.SendAsync<List<RentabilidadeGrupoDto>>(
            HttpMethod.Get, path, null, cancellationToken) ?? [];
    }
}
