using Domain.Objetivos.Projecao;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public sealed record CenarioProjecaoModel(SituacaoProjecao Situacao, int? Meses, DateTime? DataPrevista)
{
    public static CenarioProjecaoModel FromDomain(CenarioProjecao c) => new(c.Situacao, c.Meses, c.DataPrevista);
}
