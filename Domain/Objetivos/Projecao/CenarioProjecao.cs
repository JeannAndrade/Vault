namespace Domain.Objetivos.Projecao;

public sealed record CenarioProjecao(SituacaoProjecao Situacao, int? Meses, DateTime? DataPrevista)
{
    public static CenarioProjecao Prevista(int meses, DateTime dataPrevista) => new(SituacaoProjecao.Prevista, meses, dataPrevista);
    public static CenarioProjecao Atingida() => new(SituacaoProjecao.Atingida, null, null);
    public static CenarioProjecao MaisDeCemAnos() => new(SituacaoProjecao.MaisDeCemAnos, null, null);
    public static CenarioProjecao SemPrevisao() => new(SituacaoProjecao.SemPrevisao, null, null);
}
