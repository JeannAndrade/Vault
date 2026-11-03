using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Objetivos;

namespace Presentation.Pages.Objetivos;

public class EditModel(IObjetivoApi objetivoApi) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateModel.InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var objetivo = await objetivoApi.GetByIdAsync(Id, cancellationToken);
        Input = new CreateModel.InputModel
        {
            Nome = objetivo.Nome,
            Descricao = objetivo.Descricao,
            Meta = objetivo.Meta,
            FontePagadora = objetivo.FontePagadora,
            AporteMensal = objetivo.AporteMensal,
            OndeAplicar = objetivo.OndeAplicar,
            EstaAtivo = objetivo.EstaAtivo
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await objetivoApi.UpdateAsync(Id, new ObjetivoForUpdateDto(
                Input.Nome.Trim(),
                string.IsNullOrWhiteSpace(Input.Descricao) ? null : Input.Descricao.Trim(),
                Input.Meta,
                Input.FontePagadora.Trim(),
                Input.AporteMensal,
                Input.OndeAplicar.Trim(),
                Input.EstaAtivo), cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível salvar o objetivo agora. Tente novamente em instantes.";
            return Page();
        }

        return RedirectToPage("Index");
    }
}
