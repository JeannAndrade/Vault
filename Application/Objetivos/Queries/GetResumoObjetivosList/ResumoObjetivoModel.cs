using Domain.Objetivos;
using Domain.Objetivos.Projecao;

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
    public ProjecaoObjetivoModel Projecao { get; set; } = ProjecaoObjetivoModel.Vazia;

    public static ResumoObjetivoModel FromDomain(Objetivo objetivo, DateTime hoje)
    {
        var doSaldo = objetivo.Movimentos.Where(m => m.CompoeSaldoDoObjetivo).ToList();
        var totalLiquido = doSaldo.Sum(m => m.ValorLiquidoAtual);
        var resultado = ProjecaoObjetivoCalculador.Calcular(objetivo.Movimentos.Select(m => new MovimentoParaProjecao(
            m.ValorAporte, m.ValorLiquidoAtual, m.DataInvestimento, m.DataAtualizacaoValorLiquido,
            m.EhReinvestimento, m.EstaAtivo, m.CompoeSaldoDoObjetivo)).ToList(), objetivo.Meta, objetivo.AporteMensal, hoje);

        return new ResumoObjetivoModel
        {
            Id = objetivo.Id,
            Nome = objetivo.Nome,
            Meta = objetivo.Meta,
            PercentualMeta = objetivo.Meta > 0 ? totalLiquido / objetivo.Meta * 100 : 0,
            QtdeMovimentos = doSaldo.Count,
            TotalAportado = doSaldo.Sum(m => m.ValorAporte),
            TotalLiquido = totalLiquido,
            Projecao = ProjecaoObjetivoModel.FromDomain(resultado)
        };
    }
}
