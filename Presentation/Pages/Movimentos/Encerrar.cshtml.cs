using System.Net;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class EncerrarModel(IMovimentoApi movimentoApi, IDateTimeProvider clock) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true, Name = "pagina")]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public MovimentoFiltroInput Filtro { get; set; } = new();

    [BindProperty]
    public EncerramentoInput Input { get; set; } = new();

    public MovimentoDto Movimento { get; private set; } = null!;

    public string? ErrorMessage { get; private set; }

    public bool EstaEncerrado => !Movimento.EstaAtivo;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await CarregarMovimentoAsync(cancellationToken);

        if (Movimento.EstaAtivo)
        {
            Input.Motivo = null;
            Input.Data = clock.GetDateTime().Date;
        }
        else
        {
            Input.Motivo = Movimento.MotivoEncerramento;
            Input.Data = Movimento.DataEncerramento;
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await CarregarMovimentoAsync(cancellationToken);
        AdicionarErrosDeValidacao();

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await movimentoApi.EncerrarAsync(
                Id,
                new MovimentoForEncerramentoDto
                {
                    Motivo = Input.Motivo!.Value,
                    Data = Input.Data!.Value.Date
                },
                cancellationToken);
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.StatusCode == HttpStatusCode.UnprocessableEntity
                ? exception.Message
                : "Não foi possível salvar o encerramento agora. Tente novamente em instantes.";
            return Page();
        }

        return RedirectToPage("Index", Filtro.ToRouteValues(Pagina));
    }

    public async Task<IActionResult> OnPostReativarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await movimentoApi.ReativarAsync(Id, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível reativar o movimento agora. Tente novamente em instantes.";
            await CarregarMovimentoAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("Index", Filtro.ToRouteValues(Pagina));
    }

    private async Task CarregarMovimentoAsync(CancellationToken cancellationToken) =>
        Movimento = await movimentoApi.GetByIdAsync(Id, cancellationToken);

    private void AdicionarErrosDeValidacao()
    {
        foreach (var erro in EncerramentoInputValidador.Validar(Input, clock.GetDateTime().Date, obrigatorio: true))
            ModelState.AddModelError($"Input.{erro.Campo}", erro.Mensagem);
    }
}
