using Application.Objetivos.Queries.GetResumoObjetivosList;

namespace Service.Objetivos.DTOs;

public class ProjecaoObjetivoDto
{
    public decimal SaldoPendente { get; set; }
    public decimal? AporteRealizadoMensal { get; set; }
    public int MesesObservados { get; set; }
    public decimal AportePlanejadoMensal { get; set; }
    public double TaxaAnualEstimada { get; set; }
    public double TaxaMensalEstimada { get; set; }
    public bool BaixaConfianca { get; set; }
    public CenarioProjecaoDto Conservador { get; set; } = null!;
    public CenarioProjecaoDto Realista { get; set; } = null!;
    public CenarioProjecaoDto Planejado { get; set; } = null!;

    public static ProjecaoObjetivoDto FromApplication(ProjecaoObjetivoModel p) => new()
    {
        SaldoPendente = p.SaldoPendente, AporteRealizadoMensal = p.AporteRealizadoMensal,
        MesesObservados = p.MesesObservados, AportePlanejadoMensal = p.AportePlanejadoMensal,
        TaxaAnualEstimada = p.TaxaAnualEstimada, TaxaMensalEstimada = p.TaxaMensalEstimada,
        BaixaConfianca = p.BaixaConfianca,
        Conservador = CenarioProjecaoDto.FromApplication(p.Conservador),
        Realista = CenarioProjecaoDto.FromApplication(p.Realista),
        Planejado = CenarioProjecaoDto.FromApplication(p.Planejado)
    };
}
