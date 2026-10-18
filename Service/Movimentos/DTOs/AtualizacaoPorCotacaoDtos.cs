using Application.Movimentos.AtualizacaoPorCotacao;

namespace Service.Movimentos.DTOs;

public record PapelParaCotacaoDto(string Papel, decimal? UltimoValor, DateTime? DataUltimaCotacao)
{
    public static List<PapelParaCotacaoDto> FromApplication(IEnumerable<PapelParaCotacaoModel> papeis)
    {
        return papeis
            .Select(papel => new PapelParaCotacaoDto(
                papel.Papel,
                papel.UltimoValor,
                papel.DataUltimaCotacao))
            .ToList();
    }
}

public record AtualizacaoPorCotacaoSolicitacaoDto(string? Papel, decimal Valor, DateTime Data)
{
    public AtualizacaoPorCotacaoParameters ToParameters() => new()
    {
        Papel = Papel,
        Valor = Valor,
        Data = Data
    };
}

public record AtualizacaoPorCotacaoResultadoDto(bool MovimentosAtualizados, int QuantidadeMovimentos)
{
    public static AtualizacaoPorCotacaoResultadoDto FromApplication(AtualizacaoPorCotacaoResultadoModel resultado)
    {
        return new AtualizacaoPorCotacaoResultadoDto(
            resultado.MovimentosAtualizados,
            resultado.QuantidadeMovimentos);
    }
}
