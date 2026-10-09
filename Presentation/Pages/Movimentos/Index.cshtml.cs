using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class IndexModel(IMovimentoApi movimentoApi) : PageModel
{
    public const int TamanhoPagina = 12;

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    public PagedResponse<MovimentoDto> Resultado { get; private set; } =
        new PagedResponse<MovimentoDto>([], 1, TamanhoPagina, 0, 0);

    public IReadOnlyList<MovimentoDto> Movimentos => Resultado.Items;

    public string? ErrorMessage { get; private set; }

    public Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        CarregarPaginaAsync(cancellationToken);

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await movimentoApi.DeleteAsync(id, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível excluir o movimento agora. Tente novamente em instantes.";
        }

        return await CarregarPaginaAsync(cancellationToken);
    }

    private async Task<IActionResult> CarregarPaginaAsync(CancellationToken cancellationToken)
    {
        // A Api rejeita página < 1; no Web, uma URL digitada à mão com valor inválido cai na primeira.
        var pagina = Math.Max(1, Pagina);
        var resultado = await movimentoApi.GetPagedAsync(pagina, TamanhoPagina, cancellationToken);

        // Página que deixou de existir (ex.: a última foi esvaziada por exclusões): vai para a última real.
        if (resultado.Items.Count == 0 && resultado.TotalPages > 0 && pagina > resultado.TotalPages)
            return RedirectToPage(new { pagina = resultado.TotalPages });

        Resultado = resultado;
        return Page();
    }
}
