using Application.Movimentos.Commands.UpdateValorLiquidoMovimento;

namespace Service.Movimentos.DTOs;

public record MovimentoForUpdateValorLiquidoDto
{
    public decimal ValorLiquidoAtual { get; init; }

    public MovimentoModelForUpdateValorLiquido ToUpdateValorLiquidoCommand() => new()
    {
        ValorLiquidoAtual = ValorLiquidoAtual
    };
}
