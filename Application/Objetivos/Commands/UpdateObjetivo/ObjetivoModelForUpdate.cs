using System.ComponentModel.DataAnnotations;
using Domain.Objetivos;

namespace Application.Objetivos.Commands.UpdateObjetivo;

public record ObjetivoModelForUpdate : IValidatableObject
{
  [Required(ErrorMessage = "Nome do Objetivo é obrigatório")]
  [MaxLength(100, ErrorMessage = "Nome do Objetivo não deve exceder 100 caracteres")]
  public string Nome { get; set; } = string.Empty;

  [MaxLength(500, ErrorMessage = "Descrição do Objetivo não deve exceder 500 caracteres")]
  public string? Descricao { get; set; }

  [Required(ErrorMessage = "Meta é obrigatória")]
  public decimal Meta { get; set; }

  [Required(ErrorMessage = "Fonte pagadora é obrigatória")]
  [MaxLength(100, ErrorMessage = "Fonte pagadora não deve exceder 100 caracteres")]
  public string FontePagadora { get; set; } = string.Empty;

  [Required(ErrorMessage = "Aporte mensal é obrigatório")]
  [Range(1, double.MaxValue, ErrorMessage = "O valor deve ser positivo")]
  public decimal AporteMensal { get; set; }

  [Required(ErrorMessage = "Onde aplicar é obrigatório")]
  [MaxLength(100, ErrorMessage = "Onde aplicar não deve exceder 100 caracteres")]
  public string OndeAplicar { get; set; } = string.Empty;

  public bool EstaAtivo { get; set; }

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (Meta < 1m || Meta > 9999999999999999.99m)
      yield return new ValidationResult(
          "A meta deve ser maior ou igual a 1 e ter no máximo 16 dígitos inteiros",
          new[] { nameof(Meta) });

    if (decimal.Round(Meta, 2) != Meta)
      yield return new ValidationResult(
          "A meta deve ter no máximo 2 casas decimais",
          new[] { nameof(Meta) });

    if (AporteMensal > 9999999999999999.99m)
      yield return new ValidationResult(
          "O aporte mensal deve ter no máximo 16 dígitos inteiros",
          new[] { nameof(AporteMensal) });

    if (decimal.Round(AporteMensal, 2) != AporteMensal)
      yield return new ValidationResult(
          "O aporte mensal deve ter no máximo 2 casas decimais",
          new[] { nameof(AporteMensal) });
  }

  public Objetivo UpdateDomain(Objetivo objetivo)
  {
    objetivo.Nome = Nome;
    objetivo.Descricao = Descricao;
    objetivo.Meta = Meta;
    objetivo.FontePagadora = FontePagadora;
    objetivo.AporteMensal = AporteMensal;
    objetivo.OndeAplicar = OndeAplicar;
    objetivo.EstaAtivo = EstaAtivo;
    return objetivo;
  }
}
