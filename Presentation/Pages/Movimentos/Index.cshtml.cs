using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class IndexModel(IMovimentoApi movimentoApi) : PageModel
{
    public List<MovimentoDto> Movimentos { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Movimentos = await movimentoApi.GetAllAsync(cancellationToken);
    }

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

        Movimentos = await movimentoApi.GetAllAsync(cancellationToken);
        return Page();
    }
}
