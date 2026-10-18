using Application.Cotacoes;

namespace Service.Cotacoes.DTOs;

public record CotacaoDto(Guid Id, string Papel, decimal Valor, DateTime Data)
{
    public static CotacaoDto FromApplication(CotacaoModel cotacao) => new(
        cotacao.Id,
        cotacao.Papel,
        cotacao.Valor,
        cotacao.Data);

    public static List<CotacaoDto> FromApplication(IEnumerable<CotacaoModel> cotacoes)
    {
        return cotacoes.Select(FromApplication).ToList();
    }
}

public record CotacaoPontoDto(DateTime Data, decimal Valor)
{
    public static List<CotacaoPontoDto> FromApplication(IEnumerable<CotacaoPontoModel> cotacoes)
    {
        return cotacoes.Select(cotacao => new CotacaoPontoDto(cotacao.Data, cotacao.Valor)).ToList();
    }
}
