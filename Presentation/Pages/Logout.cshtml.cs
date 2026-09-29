using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LumiaFoundation.AspNetCore.ClientAuthentication;

namespace Presentation.Pages;

public class LogoutModel(IApiSignInService signInService) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await signInService.SignOutAsync(HttpContext, cancellationToken);

        return RedirectToPage("/Login");
    }
}
