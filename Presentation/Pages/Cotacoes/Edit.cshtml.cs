using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Cotacoes;

namespace Presentation.Pages.Cotacoes;

public class EditModel(ICotacaoApi api) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? PapelFiltro { get; set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var cotacao = await api.GetByIdAsync(Id, cancellationToken);
        Input = new InputModel
        {
            Papel = cotacao.Papel,
            Valor = cotacao.Valor,
            Data = cotacao.Data
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await api.UpdateAsync(
                Id,
                new CotacaoForUpdateDto(Input.Papel.Trim(), Input.Valor, Input.Data),
                cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Já existe cotação para este papel nesta data, ou os dados são inválidos.";
            return Page();
        }

        return RedirectToPage("Index", new { pagina = Pagina, papel = PapelFiltro });
    }

    public class InputModel
    {
        [Required]
        [StringLength(60)]
        public string Papel { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.0001", "79228162514264337593543950335", ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Valor { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }
    }
}

