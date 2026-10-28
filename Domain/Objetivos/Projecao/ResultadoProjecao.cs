namespace Domain.Objetivos.Projecao;

public sealed record ResultadoProjecao(
    decimal Saldo,
    decimal SaldoPendente,
    decimal? AporteRealizadoMensal,
    int MesesObservados,
    decimal AportePlanejadoMensal,
    double TaxaAnualEstimada,
    double TaxaMensalEstimada,
    bool BaixaConfianca,
    CenarioProjecao Conservador,
    CenarioProjecao Realista,
    CenarioProjecao Planejado);
