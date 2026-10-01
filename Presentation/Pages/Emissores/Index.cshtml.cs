using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Emissores;

namespace Presentation.Pages.Emissores;

public class IndexModel(IEmissorApi emissorApi) : PageModel
{
  public List<EmissorDto> Emissores { get; private set; } = [];

  public string? ErrorMessage { get; private set; }

  public async Task OnGetAsync(CancellationToken cancellationToken)
  {
    Emissores = await emissorApi.GetAllAsync(cancellationToken);
  }

  public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
  {
    try
    {
      await emissorApi.DeleteAsync(id, cancellationToken);
    }
    catch (ApiException)
    {
      ErrorMessage = "Não foi possível excluir o emissor agora. Tente novamente em instantes.";
    }

    Emissores = await emissorApi.GetAllAsync(cancellationToken);
    return Page();
  }
}
