using System.ComponentModel.DataAnnotations;
using Domain.Movimentos;
using LumiaFoundation.Core.ValidationAttributes;

namespace Application.Movimentos.Commands.UpdateMovimento;

public record MovimentoModelForUpdate
{
  [NotEmptyGuid]
  public Guid ObjetivoId { get; set; }

  [NotEmptyGuid]
  public Guid TipoRendaId { get; set; }

  [NotEmptyGuid]
  public Guid CorretoraId { get; set; }

  [NotEmptyGuid]
  public Guid ProdutoId { get; set; }

  [NotEmptyGuid]
  public Guid EmissorId { get; set; }

  [MaxLength(60, ErrorMessage = "Rentabilidade contratada não deve exceder 60 caracteres")]
  public string? RentabilidadeContratada { get; set; }

  [MaxLength(60, ErrorMessage = "Cotação na compra não deve exceder 60 caracteres")]
  public string? CotacaoNaCompra { get; set; }

  [Required(ErrorMessage = "Data do investimento é obrigatória")]
  public DateTime DataInvestimento { get; set; }

  [Required(ErrorMessage = "Valor do aporte é obrigatório")]
  [Range(0.01, double.MaxValue, ErrorMessage = "O valor do aporte deve ser maior que zero")]
  public decimal ValorAporte { get; set; }

  public bool EhReinvestimento { get; set; }

  public bool EstaAtivo { get; set; }

  [Required(ErrorMessage = "Valor líquido atual é obrigatório")]
  [Range(0, double.MaxValue, ErrorMessage = "O valor líquido atual não pode ser negativo")]
  public decimal ValorLiquidoAtual { get; set; }

  public Movimento UpdateDomain(Movimento movimento)
  {
    movimento.ObjetivoId = ObjetivoId;
    movimento.TipoRendaId = TipoRendaId;
    movimento.CorretoraId = CorretoraId;
    movimento.ProdutoId = ProdutoId;
    movimento.EmissorId = EmissorId;
    movimento.RentabilidadeContratada = RentabilidadeContratada;
    movimento.CotacaoNaCompra = CotacaoNaCompra;
    movimento.DataInvestimento = DataInvestimento;
    movimento.ValorAporte = ValorAporte;
    movimento.EhReinvestimento = EhReinvestimento;
    movimento.EstaAtivo = EstaAtivo;
    movimento.ValorLiquidoAtual = ValorLiquidoAtual;
    return movimento;
  }
}
