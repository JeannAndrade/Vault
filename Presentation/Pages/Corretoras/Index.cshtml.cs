using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Corretoras;

namespace Presentation.Pages.Corretoras;

public class IndexModel(ICorretoraApi corretoraApi) : PageModel
{
    public List<CorretoraDto> Corretoras { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Corretoras = await corretoraApi.GetAllAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await corretoraApi.DeleteAsync(id, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível excluir a corretora agora. Tente novamente em instantes.";
        }

        Corretoras = await corretoraApi.GetAllAsync(cancellationToken);
        return Page();
    }
}
