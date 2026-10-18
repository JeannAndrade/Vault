using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Cotacoes;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class AtualizacaoPorCotacaoModel : PageModel
{
    private readonly IAtualizacaoPorCotacaoApi _api;
    private readonly IDateTimeProvider _clock;

    public AtualizacaoPorCotacaoModel(IAtualizacaoPorCotacaoApi api, IDateTimeProvider clock)
    {
        _api = api;
        _clock = clock;
    }

    public List<PapelParaCotacaoDto> Papeis { get; private set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? PapelComErro { get; private set; }

    public string? ErrorMessage { get; private set; }

    [TempData]
    public string? MensagemSucesso { get; set; }

    public DateTime Hoje => _clock.GetDateTime().Date;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Input.Data = Hoje;
        await CarregarPapeisAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAtualizarAsync(string papel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || Input.Data > Hoje)
        {
            PapelComErro = papel;
            await CarregarPapeisAsync(cancellationToken);
            return Page();
        }

        try
        {
            var resultado = await _api.AtualizarAsync(
                new AtualizacaoPorCotacaoSolicitacaoDto(papel, Input.Valor, Input.Data),
                cancellationToken);

            MensagemSucesso = resultado.MovimentosAtualizados
                ? $"{papel}: {resultado.QuantidadeMovimentos} movimento(s) atualizado(s)."
                : $"{papel}: cotação registrada somente no histórico (data anterior à última cotação).";

            return RedirectToPage();
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível atualizar por cotação agora. Tente novamente.";
            PapelComErro = papel;
            await CarregarPapeisAsync(cancellationToken);
            return Page();
        }
    }

    private async Task CarregarPapeisAsync(CancellationToken cancellationToken)
    {
        Papeis = await _api.GetAsync(cancellationToken);
    }

    public sealed class InputModel
    {
        [Required]
        [Range(typeof(decimal), "0.0001", "79228162514264337593543950335", ErrorMessage = "O valor deve ser maior que zero.")]
        public decimal Valor { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }
    }
}
