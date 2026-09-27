using Application.Movimentos.Commands.CreateMovimento;

namespace Service.Movimentos.DTOs;

public record MovimentoForCreationDto
{
    public Guid ObjetivoId { get; init; }
    public Guid TipoRendaId { get; init; }
    public Guid CorretoraId { get; init; }
    public Guid ProdutoId { get; init; }
    public Guid EmissorId { get; init; }
    public string? RentabilidadeContratada { get; init; }
    public string? CotacaoNaCompra { get; init; }
    public DateTime DataInvestimento { get; init; }
    public decimal ValorAporte { get; init; }
    public bool EhReinvestimento { get; init; }
    public bool EstaAtivo { get; init; }
    public decimal ValorLiquidoAtual { get; init; }

    public MovimentoModelForCreation ToCreateMovimentoCommand() => new()
    {
        ObjetivoId = ObjetivoId,
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId,
        RentabilidadeContratada = RentabilidadeContratada,
        CotacaoNaCompra = CotacaoNaCompra,
        DataInvestimento = DataInvestimento,
        ValorAporte = ValorAporte,
        EhReinvestimento = EhReinvestimento,
        EstaAtivo = EstaAtivo,
        ValorLiquidoAtual = ValorLiquidoAtual
    };
}
