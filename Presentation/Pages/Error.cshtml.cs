using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages;

[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    // O UseExceptionHandler reexecuta a requisição neste caminho, mantendo o método HTTP original
    // (por exemplo, o POST do login); por isso a página responde a GET e a POST.
    public void OnGet()
    {
    }

    public void OnPost()
    {
    }
}
