using Application.TiposRenda.Commands.CreateTipoRenda;

namespace Service.TiposRenda.DTOs;

public record TipoRendaForCreationDto(string Nome)
{
  public TipoRendaModelForCreation ToCreateTipoRendaCommand(Guid userId) => new(Nome, userId);
}
