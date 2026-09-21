using System.ComponentModel.DataAnnotations;
using Domain.Corretoras;

namespace Application.Corretoras.Commands.CreateCorretora;

public record CorretoraModelForCreation(
    [property: Required(ErrorMessage = "Nome da Corretora é obrigatório")]
    [property: MaxLength(60, ErrorMessage = "Nome da Corretora não deve exceder 60 caracteres")]
    string Nome,

    [property: Required(ErrorMessage = "O usuário é obrigatório")]
    Guid UserId)
{
  public Corretora ToDomain() => new()
  {
    Nome = Nome,
    UserId = UserId
  };
}
