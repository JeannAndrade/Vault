using System.ComponentModel.DataAnnotations;
using Domain.TiposRenda;

namespace Application.TiposRenda.Commands.CreateTipoRenda;

public record TipoRendaModelForCreation(
    [property: Required(ErrorMessage = "Nome do Tipo de Renda é obrigatório")]
    [property: MaxLength(60, ErrorMessage = "Nome do Tipo de Renda não deve exceder 60 caracteres")]
    string Nome,

    [property: Required(ErrorMessage = "O usuário é obrigatório")]
    Guid UserId)
{
  public TipoRenda ToDomain() => new()
  {
    Nome = Nome,
    UserId = UserId
  };
}
