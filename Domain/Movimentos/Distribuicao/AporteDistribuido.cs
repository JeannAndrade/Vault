namespace Domain.Movimentos.Distribuicao;

public sealed record AporteDistribuido(
    Guid Id,
    decimal ValorAporte,
    decimal Ganho,
    decimal ValorLiquido);
