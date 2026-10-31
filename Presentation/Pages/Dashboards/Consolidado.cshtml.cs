using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Dashboards;
using Presentation.Movimentos;

namespace Presentation.Pages.Dashboards;

public class ConsolidadoModel(IConsolidadoApi api, IMovimentoFormLookupData lookupData) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public ConsolidadoFiltroInput Filtro { get; set; } = new();

    [BindProperty(SupportsGet = true, Name = "agruparPor")]
    public DimensaoConsolidado? AgruparPor { get; set; }

    public MovimentoFormLookupData Lookup { get; private set; } = null!;
    public ConsolidadoDto? Resultado { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool TemCriterio => Filtro.HasValue || AgruparPor.HasValue;
    public bool TemGrupos => Resultado is { Grupos.Count: > 0 };

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Lookup = await lookupData.LoadAsync(cancellationToken);
        if (!TemCriterio)
            return;

        try
        {
            Resultado = await api.GetConsolidadoAsync(Filtro.ToFiltro(), AgruparPor, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível carregar o consolidado agora. Tente novamente em instantes.";
            Resultado = null;
        }
    }
}
