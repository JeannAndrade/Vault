using System.ComponentModel.DataAnnotations;
using Domain.TiposRenda;

namespace Application.TiposRenda.Commands.UpdateTipoRenda;

public record TipoRendaModelForUpdate
{
  [Required(ErrorMessage = "Nome do Tipo de Renda é obrigatório")]
  [MaxLength(60, ErrorMessage = "Nome do Tipo de Renda não deve exceder 60 caracteres")]
  public string Nome { get; set; } = string.Empty;

  public TipoRenda UpdateDomain(TipoRenda tipoRenda)
  {
    tipoRenda.Nome = Nome;
    return tipoRenda;
  }
}
