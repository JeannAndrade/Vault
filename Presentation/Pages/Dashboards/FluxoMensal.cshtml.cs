using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Dashboards;

namespace Presentation.Pages.Dashboards;

public class FluxoMensalModel(IFluxoMensalApi api) : PageModel
{
    public List<RetornoMensalDto> Retornos { get; private set; } = [];
    public List<InvestidoMensalDto> Investidos { get; private set; } = [];
    public string? RetornoErro { get; private set; }
    public string? InvestidoErro { get; private set; }

    public decimal TotalRetorno => Retornos.Sum(item => item.Total);
    public int TotalQuantidadeRetorno => Retornos.Sum(item => item.Quantidade);
    public decimal TotalNovo => Investidos.Sum(item => item.Novo);
    public decimal TotalReinvestimento => Investidos.Sum(item => item.Reinvestimento);
    public decimal TotalInvestido => Investidos.Sum(item => item.Total);
    public string RetornoJson => FluxoMensalGraficoDados.RetornoParaJson(Retornos);
    public string InvestidoJson => FluxoMensalGraficoDados.InvestidoParaJson(Investidos);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try { Retornos = await api.GetRetornoMensalAsync(cancellationToken); }
        catch (ApiException) { RetornoErro = "Não foi possível carregar o retorno previsto agora. Tente novamente em instantes."; }

        try { Investidos = await api.GetInvestidoMensalAsync(cancellationToken); }
        catch (ApiException) { InvestidoErro = "Não foi possível carregar o valor investido agora. Tente novamente em instantes."; }
    }

    public string RotuloMes(string mes) => FluxoMensalFormatter.FormatarMes(mes);

    // A ordem cronológica crescente é garantida pelo contrato (D2/ADR 0019).
    public bool EhMesAtualDoRetorno(int indice) => indice == 0;
    public bool EhMesAtualDoInvestido(int indice) => indice == Investidos.Count - 1;
}
