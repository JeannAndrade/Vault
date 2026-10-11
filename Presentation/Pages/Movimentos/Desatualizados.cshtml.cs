using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class DesatualizadosModel(IMovimentoApi movimentoApi) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<MovimentoDesatualizadoDto> Movimentos { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public Guid? IdComErro { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await CarregarAsync(cancellationToken);

    public async Task<IActionResult> OnPostAtualizarAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            IdComErro = id;
            await CarregarAsync(cancellationToken);
            return Page();
        }

        try
        {
            await movimentoApi.UpdateValorLiquidoAsync(id, new MovimentoForUpdateValorLiquidoDto
            {
                ValorLiquidoAtual = Input.ValorLiquidoAtual
            }, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível atualizar o valor líquido agora. Tente novamente em instantes.";
            await CarregarAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task CarregarAsync(CancellationToken cancellationToken) =>
        Movimentos = await movimentoApi.GetDesatualizadosAsync(cancellationToken);

    public class InputModel
    {
        [Required(ErrorMessage = "Informe o valor líquido atual.")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor líquido atual não pode ser negativo.")]
        public decimal ValorLiquidoAtual { get; set; }
    }
}
