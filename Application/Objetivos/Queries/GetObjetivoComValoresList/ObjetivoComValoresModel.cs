using Domain.Objetivos;

namespace Application.Objetivos.Queries.GetObjetivoComValoresList;

public record ObjetivoComValoresModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Meta { get; set; }
    public string FontePagadora { get; set; } = string.Empty;
    public decimal AporteMensal { get; set; }
    public string OndeAplicar { get; set; } = string.Empty;
    public decimal ValorTotalInvestido { get; set; }
    public decimal ValorTotalLiquido { get; set; }
    public Guid UserId { get; set; }

    public static ObjetivoComValoresModel FromDomain(Objetivo objetivo)
    {
        return new ObjetivoComValoresModel
        {
            Id = objetivo.Id,
            Nome = objetivo.Nome,
            Meta = objetivo.Meta,
            FontePagadora = objetivo.FontePagadora,
            AporteMensal = objetivo.AporteMensal,
            OndeAplicar = objetivo.OndeAplicar,
            ValorTotalInvestido = objetivo.Movimentos.Sum(m => m.ValorAporte),
            ValorTotalLiquido = objetivo.Movimentos.Sum(m => m.ValorLiquidoAtual),
            UserId = objetivo.UserId
        };
    }
}
