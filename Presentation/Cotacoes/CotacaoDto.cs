namespace Presentation.Cotacoes;

public sealed record CotacaoDto
{
    public Guid Id { get; init; }
    public string Papel { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public DateTime Data { get; init; }
}

public sealed record CotacaoForCreationDto(string Papel, decimal Valor, DateTime Data);
public sealed record CotacaoForUpdateDto(string Papel, decimal Valor, DateTime Data);
public sealed record CotacaoPontoDto(DateTime Data, decimal Valor);
public sealed record PapelParaCotacaoDto(string Papel, decimal? UltimoValor, DateTime? DataUltimaCotacao);
public sealed record AtualizacaoPorCotacaoSolicitacaoDto(string Papel, decimal Valor, DateTime Data);
public sealed record AtualizacaoPorCotacaoResultadoDto(bool MovimentosAtualizados, int QuantidadeMovimentos);
