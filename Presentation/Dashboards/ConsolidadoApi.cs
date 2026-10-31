using LumiaFoundation.Http.Client.Services;

namespace Presentation.Dashboards;

public sealed class ConsolidadoApi(IApiConnection connection) : IConsolidadoApi
{
    private const string BasePath = "api/dashboards/consolidado";

    public async Task<ConsolidadoDto> GetConsolidadoAsync(
        ConsolidadoFiltro filtro, DimensaoConsolidado? agruparPor, CancellationToken cancellationToken)
    {
        var parametros = new List<string>();
        Adicionar(parametros, "tipoRendaId", filtro.TipoRendaId);
        Adicionar(parametros, "corretoraId", filtro.CorretoraId);
        Adicionar(parametros, "produtoId", filtro.ProdutoId);
        Adicionar(parametros, "emissorId", filtro.EmissorId);
        if (agruparPor.HasValue)
            parametros.Add($"agruparPor={agruparPor.Value.ToString().ToLowerInvariant()}");

        var path = parametros.Count == 0 ? BasePath : $"{BasePath}?{string.Join('&', parametros)}";
        return await connection.SendAsync<ConsolidadoDto>(HttpMethod.Get, path, null, cancellationToken)
            ?? new ConsolidadoDto();
    }

    private static void Adicionar(List<string> parametros, string nome, Guid? valor)
    {
        if (valor.HasValue)
            parametros.Add(FormattableString.Invariant($"{nome}={valor.Value}"));
    }
}
