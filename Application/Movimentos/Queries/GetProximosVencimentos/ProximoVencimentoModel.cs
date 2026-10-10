using Domain.Movimentos;

namespace Application.Movimentos.Queries.GetProximosVencimentos;

public record ProximoVencimentoModel
{
    public Guid Id { get; set; }
    public string? ObjetivoNome { get; set; }
    public string? CorretoraNome { get; set; }
    public string? ProdutoNome { get; set; }
    public string? EmissorNome { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorLiquidoAtual { get; set; }

    public static ProximoVencimentoModel FromDomain(Movimento movimento) => new()
    {
        Id = movimento.Id,
        ObjetivoNome = movimento.Objetivo?.Nome,
        CorretoraNome = movimento.Corretora?.Nome,
        ProdutoNome = movimento.Produto?.Nome,
        EmissorNome = movimento.Emissor?.Nome,
        DataVencimento = movimento.DataVencimento,
        ValorLiquidoAtual = movimento.ValorLiquidoAtual
    };
}
