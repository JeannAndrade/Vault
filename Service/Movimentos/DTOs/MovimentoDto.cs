using Application.Movimentos;

namespace Service.Movimentos.DTOs;

public class MovimentoDto
{
    public Guid Id { get; set; }
    public Guid ObjetivoId { get; set; }
    public string? ObjetivoNome { get; set; }
    public Guid TipoRendaId { get; set; }
    public string? TipoRendaNome { get; set; }
    public Guid CorretoraId { get; set; }
    public string? CorretoraNome { get; set; }
    public Guid ProdutoId { get; set; }
    public string? ProdutoNome { get; set; }
    public Guid EmissorId { get; set; }
    public string? EmissorNome { get; set; }
    public string? RentabilidadeContratada { get; set; }
    public string? CotacaoNaCompra { get; set; }
    public string? Observacao { get; set; }
    public string? Protocolo { get; set; }
    public decimal? Quantidade { get; set; }
    public DateTime DataInvestimento { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorAporte { get; set; }
    public bool EhReinvestimento { get; set; }
    public bool EstaAtivo { get; set; }
    public decimal ValorLiquidoAtual { get; set; }

    public static MovimentoDto FromApplication(MovimentoModel movimento) => new()
    {
        Id = movimento.Id,
        ObjetivoId = movimento.ObjetivoId,
        ObjetivoNome = movimento.ObjetivoNome,
        TipoRendaId = movimento.TipoRendaId,
        TipoRendaNome = movimento.TipoRendaNome,
        CorretoraId = movimento.CorretoraId,
        CorretoraNome = movimento.CorretoraNome,
        ProdutoId = movimento.ProdutoId,
        ProdutoNome = movimento.ProdutoNome,
        EmissorId = movimento.EmissorId,
        EmissorNome = movimento.EmissorNome,
        RentabilidadeContratada = movimento.RentabilidadeContratada,
        CotacaoNaCompra = movimento.CotacaoNaCompra,
        Observacao = movimento.Observacao,
        Protocolo = movimento.Protocolo,
        Quantidade = movimento.Quantidade,
        DataInvestimento = movimento.DataInvestimento,
        DataVencimento = movimento.DataVencimento,
        ValorAporte = movimento.ValorAporte,
        EhReinvestimento = movimento.EhReinvestimento,
        EstaAtivo = movimento.EstaAtivo,
        ValorLiquidoAtual = movimento.ValorLiquidoAtual
    };

    public static List<MovimentoDto> FromApplication(List<MovimentoModel> movimentos) => [.. movimentos.Select(FromApplication)];
}
