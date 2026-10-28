namespace Presentation.Objetivos;

public sealed record ProjecaoObjetivoDto(
    decimal SaldoPendente,
    decimal? AporteRealizadoMensal,
    int MesesObservados,
    decimal AportePlanejadoMensal,
    double TaxaAnualEstimada,
    double TaxaMensalEstimada,
    bool BaixaConfianca,
    CenarioProjecaoDto Conservador,
    CenarioProjecaoDto Realista,
    CenarioProjecaoDto Planejado);
