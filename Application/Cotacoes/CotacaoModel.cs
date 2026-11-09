using Domain.Cotacoes;

namespace Application.Cotacoes;

public record CotacaoModel(Guid Id, Guid OwnerId, string Papel, decimal Valor, DateTime Data)
{
    public static CotacaoModel FromDomain(Cotacao c) => new(c.Id, c.OwnerId, c.Papel, c.Valor, c.Data);
}

public record CotacaoPontoModel(DateTime Data, decimal Valor);
