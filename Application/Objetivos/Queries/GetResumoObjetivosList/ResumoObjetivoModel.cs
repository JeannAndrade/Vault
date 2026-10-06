using Domain.Objetivos;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public record ResumoObjetivoModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Meta { get; set; }
    public decimal PercentualMeta { get; set; }
    public int QtdeMovimentos { get; set; }
    public decimal TotalAportado { get; set; }
    public decimal TotalLiquido { get; set; }

    public static ResumoObjetivoModel FromDomain(Objetivo objetivo)
    {
        var totalLiquido = objetivo.Movimentos.Where(m => m.EstaAtivo).Sum(m => m.ValorLiquidoAtual);

        return new ResumoObjetivoModel
        {
            Id = objetivo.Id,
            Nome = objetivo.Nome,
            Meta = objetivo.Meta,
            PercentualMeta = totalLiquido / objetivo.Meta * 100,
            QtdeMovimentos = objetivo.Movimentos.Count(m => m.EstaAtivo),
            TotalAportado = objetivo.Movimentos.Where(m => m.EstaAtivo).Sum(m => m.ValorAporte),
            TotalLiquido = totalLiquido
        };
    }
}
