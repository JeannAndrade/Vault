using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Objetivos;

namespace Presentation.Pages.Objetivos;

public class IndexModel(IObjetivoApi objetivoApi) : PageModel
{
  public List<ObjetivoDto> Objetivos { get; private set; } = [];

  public string? ErrorMessage { get; private set; }

  public async Task OnGetAsync(CancellationToken cancellationToken)
  {
    Objetivos = await objetivoApi.GetAllAsync(cancellationToken);
  }

  public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
  {
    try
    {
      await objetivoApi.DeleteAsync(id, cancellationToken);
    }
    catch (ApiException)
    {
      ErrorMessage = "Não foi possível excluir o objetivo agora. Tente novamente em instantes.";
    }

    Objetivos = await objetivoApi.GetAllAsync(cancellationToken);
    return Page();
  }
}
