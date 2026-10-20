using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Dashboards;

namespace Presentation.Pages.Dashboards;

public class RentabilidadeModel(IRentabilidadeApi api) : PageModel
{
    // Espelha a Api e o Domain (ADR 0018).
    public const int TopPadrao = 25;
    public const int TopMaximo = 100;
    public const int DiasMinimosParaRanking = 30;

    [BindProperty(SupportsGet = true, Name = "top")]
    public int Top { get; set; } = TopPadrao;

    [BindProperty(SupportsGet = true, Name = "ordem")]
    public OrdemRentabilidade Ordem { get; set; } = OrdemRentabilidade.Maiores;

    public List<RentabilidadeGrupoDto> Grupos { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public bool EhModoMenores => Ordem == OrdemRentabilidade.Menores;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Top = Math.Clamp(Top, 1, TopMaximo);

        try
        {
            Grupos = await api.GetRentabilidadesAsync(Top, Ordem, cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível carregar as rentabilidades agora. Tente novamente em instantes.";
        }
    }

    public string ObterTitulo(RentabilidadeGrupoDto grupo) =>
        EhModoMenores
            ? $"Menores rentabilidades da renda {grupo.TipoRendaNome ?? "(sem nome)"}"
            : $"Campeões da renda {grupo.TipoRendaNome ?? "(sem nome)"}";
}
