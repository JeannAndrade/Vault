using Domain.Objetivos.Projecao;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public sealed record ProjecaoObjetivoModel(
    decimal SaldoPendente,
    decimal? AporteRealizadoMensal,
    int MesesObservados,
    decimal AportePlanejadoMensal,
    double TaxaAnualEstimada,
    double TaxaMensalEstimada,
    bool BaixaConfianca,
    CenarioProjecaoModel Conservador,
    CenarioProjecaoModel Realista,
    CenarioProjecaoModel Planejado)
{
    public static ProjecaoObjetivoModel FromDomain(ResultadoProjecao r) => new(r.SaldoPendente, r.AporteRealizadoMensal, r.MesesObservados,
        r.AportePlanejadoMensal, r.TaxaAnualEstimada, r.TaxaMensalEstimada, r.BaixaConfianca,
        CenarioProjecaoModel.FromDomain(r.Conservador), CenarioProjecaoModel.FromDomain(r.Realista), CenarioProjecaoModel.FromDomain(r.Planejado));

    public static ProjecaoObjetivoModel Vazia => new(0, null, 0, 0, 0, 0, true,
        CenarioProjecaoModel.FromDomain(CenarioProjecao.SemPrevisao()),
        CenarioProjecaoModel.FromDomain(CenarioProjecao.SemPrevisao()),
        CenarioProjecaoModel.FromDomain(CenarioProjecao.SemPrevisao()));
}
