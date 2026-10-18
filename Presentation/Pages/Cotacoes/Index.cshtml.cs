using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Cotacoes;

namespace Presentation.Pages.Cotacoes;

public class IndexModel(ICotacaoApi api) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Papel { get; set; }

    public PagedResponse<CotacaoDto> Resultado { get; private set; } = new([], 1, 12, 0, 0);

    public List<string> Papeis { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public object RouteValues => new { pagina = Pagina, papel = Papel };

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Papeis = await api.GetPapeisAsync(cancellationToken);
        Resultado = await api.GetPagedAsync(Papel, Pagina, 12, cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, int pagina, string? papel, CancellationToken cancellationToken)
    {
        try
        {
            await api.DeleteAsync(id, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível excluir a cotação agora. Tente novamente em instantes.";
        }

        Papeis = await api.GetPapeisAsync(cancellationToken);
        Resultado = await api.GetPagedAsync(papel, pagina, 12, cancellationToken);

        return Page();
    }
}

