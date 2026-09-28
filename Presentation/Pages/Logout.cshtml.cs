using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Authentication;

namespace Presentation.Pages;

public class LogoutModel(ISignInService signInService) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await signInService.SignOutAsync(HttpContext, cancellationToken);

        return RedirectToPage("/Login");
    }
}
