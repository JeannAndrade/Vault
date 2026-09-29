using System.ComponentModel.DataAnnotations;
using LumiaFoundation.AspNetCore.ClientAuthentication;
using LumiaFoundation.Http.Client.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages;

public class RegisterModel(IApiRegistrationService registrationService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var registration = new UserRegistration(
            Input.FirstName.Trim(),
            Input.LastName.Trim(),
            Input.UserName.Trim(),
            Input.Password,
            Input.Email.Trim());

        var result = await registrationService.RegisterAsync(registration, cancellationToken);

        if (result.Outcome == RegistrationOutcome.Succeeded)
            return RedirectToPage("/Login", new { registered = true });

        ErrorMessage = result.Outcome == RegistrationOutcome.Rejected
            ? result.ErrorMessage
            : "Não foi possível criar a conta agora. Tente novamente em instantes.";

        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(256, ErrorMessage = "O nome não deve exceder 256 caracteres.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o sobrenome.")]
        [StringLength(256, ErrorMessage = "O sobrenome não deve exceder 256 caracteres.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o usuário.")]
        [StringLength(256, ErrorMessage = "O usuário não deve exceder 256 caracteres.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{10,}$",
            ErrorMessage = "A senha deve ter ao menos 10 caracteres, com maiúscula, minúscula, dígito e símbolo.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
