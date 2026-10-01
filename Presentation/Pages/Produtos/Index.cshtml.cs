using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Produtos;

namespace Presentation.Pages.Produtos;

public class IndexModel(IProdutoApi produtoApi) : PageModel
{
  public List<ProdutoDto> Produtos { get; private set; } = [];

  public string? ErrorMessage { get; private set; }

  public async Task OnGetAsync(CancellationToken cancellationToken)
  {
    Produtos = await produtoApi.GetAllAsync(cancellationToken);
  }

  public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
  {
    try
    {
      await produtoApi.DeleteAsync(id, cancellationToken);
    }
    catch (ApiException)
    {
      ErrorMessage = "Não foi possível excluir o produto agora. Tente novamente em instantes.";
    }

    Produtos = await produtoApi.GetAllAsync(cancellationToken);
    return Page();
  }
}
