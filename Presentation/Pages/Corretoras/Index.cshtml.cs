using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Corretoras;

namespace Presentation.Pages.Corretoras;

public class IndexModel(ICorretoraApi corretoraApi) : PageModel
{
    public List<CorretoraDto> Corretoras { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Corretoras = await corretoraApi.GetAllAsync(cancellationToken);
    }
}
