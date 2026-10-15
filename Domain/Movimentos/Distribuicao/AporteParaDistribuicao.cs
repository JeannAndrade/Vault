namespace Domain.Movimentos.Distribuicao;

public sealed record AporteParaDistribuicao(
    Guid Id,
    decimal ValorAporte,
    DateTime DataInvestimento);
