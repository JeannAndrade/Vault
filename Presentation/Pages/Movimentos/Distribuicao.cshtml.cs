using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Core.ValidationAttributes;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class DistribuicaoModel : PageModel
{
    private readonly IDistribuicaoValorLiquidoApi _api;
    private readonly IMovimentoFormLookupData _lookupData;

    public DistribuicaoModel(
        IDistribuicaoValorLiquidoApi api,
        IMovimentoFormLookupData lookupData)
    {
        _api = api;
        _lookupData = lookupData;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public MovimentoFormLookupData Lookup { get; private set; } = null!;

    public DistribuicaoValorLiquidoDto? Resultado { get; private set; }

    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? MensagemSucesso { get; set; }

    public decimal TotalLiquidoAtual => Resultado?.Aportes.Sum(a => a.ValorLiquidoAtual) ?? 0;

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => await CarregarAsync(cancellationToken);

    public async Task<IActionResult> OnPostSimularAsync(CancellationToken cancellationToken)
    {
        await CarregarAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            Resultado = await _api.SimularAsync(Input.ToSolicitacaoDto(), cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível simular agora. Tente novamente em instantes.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmarAsync(CancellationToken cancellationToken)
    {
        await CarregarAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Dados inválidos. Refaça a simulação.";
            return Page();
        }

        try
        {
            await _api.DistribuirAsync(Input.ToSolicitacaoDto(), cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível confirmar agora. Refaça a simulação e tente novamente.";
            return Page();
        }

        MensagemSucesso = "Valor líquido distribuído com sucesso.";
        return RedirectToPage();
    }

    private async Task CarregarAsync(CancellationToken cancellationToken)
        => Lookup = await _lookupData.LoadAsync(cancellationToken);

    public class InputModel
    {
        [NotEmptyGuid]
        [Display(Name = "Tipo de Renda")]
        public Guid TipoRendaId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Corretora")]
        public Guid CorretoraId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Produto")]
        public Guid ProdutoId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Emissor")]
        public Guid EmissorId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "O valor líquido total não pode ser negativo.")]
        public decimal ValorLiquidoTotal { get; set; }

        public DistribuicaoValorLiquidoSolicitacaoDto ToSolicitacaoDto()
            => new()
            {
                TipoRendaId = TipoRendaId,
                CorretoraId = CorretoraId,
                ProdutoId = ProdutoId,
                EmissorId = EmissorId,
                ValorLiquidoTotal = ValorLiquidoTotal,
            };
    }
}
