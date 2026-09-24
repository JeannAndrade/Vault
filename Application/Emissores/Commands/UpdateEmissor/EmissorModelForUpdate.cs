using System.ComponentModel.DataAnnotations;
using Domain.Emissores;

namespace Application.Emissores.Commands.UpdateEmissor;

public record EmissorModelForUpdate
{
  [Required(ErrorMessage = "Nome do Emissor é obrigatório")]
  [MaxLength(60, ErrorMessage = "Nome do Emissor não deve exceder 60 caracteres")]
  public string Nome { get; set; } = string.Empty;

  public Emissor UpdateDomain(Emissor emissor)
  {
    emissor.Nome = Nome;
    return emissor;
  }
}
