using System.ComponentModel.DataAnnotations;
using Domain.Objetivos;

namespace Application.Objetivos.Commands.UpdateObjetivo;

public record ObjetivoModelForUpdate
{
  [Required(ErrorMessage = "Nome do Objetivo é obrigatório")]
  [MaxLength(100, ErrorMessage = "Nome do Objetivo não deve exceder 100 caracteres")]
  public string Nome { get; set; } = string.Empty;

  [MaxLength(500, ErrorMessage = "Descrição do Objetivo não deve exceder 500 caracteres")]
  public string? Descricao { get; set; }

  [Required(ErrorMessage = "Meta é obrigatória")]
  [Range(1, double.MaxValue, ErrorMessage = "O valor deve ser positivo")]
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

  public Objetivo UpdateDomain(Objetivo objetivo)
  {
    objetivo.Nome = Nome;
    objetivo.Descricao = Descricao;
    objetivo.Meta = Meta;
    objetivo.FontePagadora = FontePagadora;
    objetivo.AporteMensal = AporteMensal;
    objetivo.OndeAplicar = OndeAplicar;
    return objetivo;
  }
}
