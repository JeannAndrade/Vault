using Domain.Movimentos;

namespace Application.Movimentos;

public record MovimentoModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ObjetivoId { get; set; }
    public Guid TipoRendaId { get; set; }
    public Guid CorretoraId { get; set; }
    public Guid ProdutoId { get; set; }
    public Guid EmissorId { get; set; }

    // Preenchidos apenas quando o Movimento é lido via GetWithRelatedEntitiesAsync
    // (GetMovimentoQuery / GetMovimentosListQuery). Em Create/Update ficam null,
    // pois a entidade recém-persistida não carrega as navigation properties.
    public string? ObjetivoNome { get; set; }
    public string? TipoRendaNome { get; set; }
    public string? CorretoraNome { get; set; }
    public string? ProdutoNome { get; set; }
    public string? EmissorNome { get; set; }

    public string? RentabilidadeContratada { get; set; }
    public string? CotacaoNaCompra { get; set; }
    public DateTime DataInvestimento { get; set; }
    public decimal ValorAporte { get; set; }
    public bool EhReinvestimento { get; set; }
    public bool EstaAtivo { get; set; }
    public decimal ValorLiquidoAtual { get; set; }

    public static MovimentoModel FromDomain(Movimento movimento)
    {
        return new MovimentoModel
        {
            Id = movimento.Id,
            UserId = movimento.UserId,
            ObjetivoId = movimento.ObjetivoId,
            TipoRendaId = movimento.TipoRendaId,
            CorretoraId = movimento.CorretoraId,
            ProdutoId = movimento.ProdutoId,
            EmissorId = movimento.EmissorId,
            ObjetivoNome = movimento.Objetivo?.Nome,
            TipoRendaNome = movimento.TipoRenda?.Nome,
            CorretoraNome = movimento.Corretora?.Nome,
            ProdutoNome = movimento.Produto?.Nome,
            EmissorNome = movimento.Emissor?.Nome,
            RentabilidadeContratada = movimento.RentabilidadeContratada,
            CotacaoNaCompra = movimento.CotacaoNaCompra,
            DataInvestimento = movimento.DataInvestimento,
            ValorAporte = movimento.ValorAporte,
            EhReinvestimento = movimento.EhReinvestimento,
            EstaAtivo = movimento.EstaAtivo,
            ValorLiquidoAtual = movimento.ValorLiquidoAtual
        };
    }
}
