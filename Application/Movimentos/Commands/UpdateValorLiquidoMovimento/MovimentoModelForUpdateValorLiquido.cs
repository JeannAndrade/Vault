using System.ComponentModel.DataAnnotations;
using Domain.Movimentos;

namespace Application.Movimentos.Commands.UpdateValorLiquidoMovimento;

public record MovimentoModelForUpdateValorLiquido
{
    [Required(ErrorMessage = "Valor líquido atual é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "O valor líquido atual não pode ser negativo")]
    public decimal ValorLiquidoAtual { get; set; }

    public void UpdateDomain(Movimento movimento, DateTime hoje)
    {
        movimento.ValorLiquidoAtual = ValorLiquidoAtual;
        movimento.DataAtualizacaoValorLiquido = hoje.Date;
    }
}
