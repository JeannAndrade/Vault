using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.TiposRenda;

namespace Presentation.Pages.TiposRenda;

public class IndexModel(ITipoRendaApi tipoRendaApi) : PageModel
{
  public List<TipoRendaDto> TiposRenda { get; private set; } = [];

  public string? ErrorMessage { get; private set; }

  public async Task OnGetAsync(CancellationToken cancellationToken)
  {
    TiposRenda = await tipoRendaApi.GetAllAsync(cancellationToken);
  }

  public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
  {
    try
    {
      await tipoRendaApi.DeleteAsync(id, cancellationToken);
    }
    catch (ApiException)
    {
      ErrorMessage = "Não foi possível excluir o tipo de renda agora. Tente novamente em instantes.";
    }

    TiposRenda = await tipoRendaApi.GetAllAsync(cancellationToken);
    return Page();
  }
}
