using System.ComponentModel.DataAnnotations;
using Domain.Emissores;

namespace Application.Emissores.Commands.CreateEmissor;

public record EmissorModelForCreation(
    [property: Required(ErrorMessage = "Nome do Emissor é obrigatório")]
    [property: MaxLength(60, ErrorMessage = "Nome do Emissor não deve exceder 60 caracteres")]
    string Nome)
{
  public Emissor ToDomain(Guid ownerId) => new()
  {
    Nome = Nome,
    OwnerId = ownerId
  };
}
