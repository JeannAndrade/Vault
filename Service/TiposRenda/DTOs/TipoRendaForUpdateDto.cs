using Application.TiposRenda.Commands.UpdateTipoRenda;

namespace Service.TiposRenda.DTOs;

public record TipoRendaForUpdateDto(string Nome)
{
  public TipoRendaModelForUpdate ToUpdateTipoRendaCommand() => new() { Nome = Nome };
}
