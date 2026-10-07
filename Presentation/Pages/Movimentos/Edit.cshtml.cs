using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class EditModel(IMovimentoApi movimentoApi, IMovimentoFormLookupData lookupData) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateModel.InputModel Input { get; set; } = new();

    public MovimentoFormLookupData Lookup { get; private set; } = null!;

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Lookup = await lookupData.LoadAsync(cancellationToken);
        var movimento = await movimentoApi.GetByIdAsync(Id, cancellationToken);
        Input = MapToInput(movimento);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Lookup = await lookupData.LoadAsync(cancellationToken);

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await movimentoApi.UpdateAsync(Id, Input.ToUpdateDto(), cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível salvar o movimento agora. Tente novamente em instantes.";
            return Page();
        }

        return RedirectToPage("Index");
    }

    private static CreateModel.InputModel MapToInput(MovimentoDto movimento) => new()
    {
        ObjetivoId = movimento.ObjetivoId,
        TipoRendaId = movimento.TipoRendaId,
        CorretoraId = movimento.CorretoraId,
        ProdutoId = movimento.ProdutoId,
        EmissorId = movimento.EmissorId,
        RentabilidadeContratada = movimento.RentabilidadeContratada,
        CotacaoNaCompra = movimento.CotacaoNaCompra,
        Observacao = movimento.Observacao,
        Protocolo = movimento.Protocolo,
        Quantidade = movimento.Quantidade,
        DataInvestimento = movimento.DataInvestimento,
        DataVencimento = movimento.DataVencimento,
        ValorAporte = movimento.ValorAporte,
        EhReinvestimento = movimento.EhReinvestimento,
        EstaAtivo = movimento.EstaAtivo,
        ValorLiquidoAtual = movimento.ValorLiquidoAtual
    };
}
