using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;
using Presentation.Objetivos;

namespace Presentation.Pages;

public class IndexModel(IObjetivoApi objetivoApi, IMovimentoApi movimentoApi) : PageModel
{
    public List<ResumoObjetivoDto> Objetivos { get; private set; } = [];

    public List<ProximoVencimentoDto> ProximosVencimentos { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public string? ProximosVencimentosErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Objetivos = await objetivoApi.GetAllComValoresAsync(cancellationToken);

        try
        {
            ProximosVencimentos = await movimentoApi.GetProximosVencimentosAsync(cancellationToken);
        }
        catch (ApiException)
        {
            ProximosVencimentosErrorMessage = "Não foi possível carregar os próximos vencimentos agora.";
        }
    }

    public static string ObterClasseLinha(ProximoVencimentoDto movimento)
    {
        var hojeUtc = new UtcDateTimeProvider().GetDateTime().Date;

        if (movimento.DataVencimento is DateTime dataVencimento && (dataVencimento.Date - hojeUtc).TotalDays < 10)
            return "bg-warning-subtle";

        return movimento.ValorLiquidoAtual > 5000m ? "bg-success-subtle" : string.Empty;
    }
}
