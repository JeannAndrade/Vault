using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LumiaFoundation.AspNetCore.ClientAuthentication;

namespace Presentation.Pages;

public class LoginModel(IApiSignInService signInService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Expired { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Registered { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var outcome = await signInService.SignInAsync(
            HttpContext,
            Input.UserName.Trim(),
            Input.Password,
            cancellationToken);

        if (outcome == SignInOutcome.Succeeded)
        {
            // Só aceita destino local: evita open redirect (OWASP).
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");
        }

        ErrorMessage = outcome == SignInOutcome.InvalidCredentials
            ? "Usuário ou senha inválidos."
            : "Não foi possível entrar agora. Tente novamente em instantes.";

        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Informe o usuário.")]
        [StringLength(256, ErrorMessage = "O usuário não deve exceder 256 caracteres.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
