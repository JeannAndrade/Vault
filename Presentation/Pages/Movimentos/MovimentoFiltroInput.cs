using Microsoft.AspNetCore.Mvc;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public sealed class MovimentoFiltroInput
{
    public Guid? ObjetivoId { get; set; }
    public Guid? TipoRendaId { get; set; }
    public Guid? CorretoraId { get; set; }
    public Guid? ProdutoId { get; set; }
    public Guid? EmissorId { get; set; }
    public bool? EstaAtivo { get; set; }

    public bool HasValue =>
        ObjetivoId.HasValue ||
        TipoRendaId.HasValue ||
        CorretoraId.HasValue ||
        ProdutoId.HasValue ||
        EmissorId.HasValue ||
        EstaAtivo.HasValue;

    public MovimentoFiltro ToFiltro() => new()
    {
        ObjetivoId = ObjetivoId,
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId,
        EstaAtivo = EstaAtivo
    };

    public RouteValueDictionary ToRouteValues(int? pagina = null)
    {
        var routeValues = new RouteValueDictionary();

        if (pagina.HasValue)
            routeValues["pagina"] = pagina.Value;

        AddIfHasValue(routeValues, "Filtro.ObjetivoId", ObjetivoId);
        AddIfHasValue(routeValues, "Filtro.TipoRendaId", TipoRendaId);
        AddIfHasValue(routeValues, "Filtro.CorretoraId", CorretoraId);
        AddIfHasValue(routeValues, "Filtro.ProdutoId", ProdutoId);
        AddIfHasValue(routeValues, "Filtro.EmissorId", EmissorId);
        if (EstaAtivo.HasValue)
            routeValues["Filtro.EstaAtivo"] = EstaAtivo.Value.ToString().ToLowerInvariant();

        return routeValues;
    }

    private static void AddIfHasValue<T>(RouteValueDictionary routeValues, string key, T? value)
        where T : struct
    {
        if (value.HasValue)
            routeValues[key] = value.Value;
    }
}
