using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Corretoras;

namespace Presentation.Pages.Corretoras;

public class EditModel(ICorretoraApi corretoraApi) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var corretora = await corretoraApi.GetByIdAsync(Id, cancellationToken);
        Input = new InputModel { Nome = corretora.Nome };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await corretoraApi.UpdateAsync(Id, new CorretoraForUpdateDto(Input.Nome.Trim()), cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível salvar a corretora agora. Tente novamente em instantes.";
            return Page();
        }

        return RedirectToPage("Index");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(60, ErrorMessage = "O nome não deve exceder 60 caracteres.")]
        public string Nome { get; set; } = string.Empty;
    }
}
