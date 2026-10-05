using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Objetivos;

namespace Presentation.Pages;

public class IndexModel(IObjetivoApi objetivoApi) : PageModel
{
    public List<ResumoObjetivoDto> Objetivos { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Objetivos = await objetivoApi.GetAllComValoresAsync(cancellationToken);
    }
}
