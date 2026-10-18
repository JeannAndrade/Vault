using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Cotacoes;

namespace Presentation.Pages.Cotacoes;

public class GraficoModel(ICotacaoApi api) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Papel { get; set; }

    public List<string> Papeis { get; private set; } = [];

    public List<CotacaoPontoDto> Pontos { get; private set; } = [];

    public string DadosJson => CotacaoGraficoDados.ToJson(Papel ?? string.Empty, Pontos);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Papeis = await api.GetPapeisAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(Papel))
            Pontos = await api.GetSerieAsync(Papel, cancellationToken);
    }
}

