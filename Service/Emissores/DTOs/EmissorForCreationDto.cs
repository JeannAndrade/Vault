using Application.Emissores.Commands.CreateEmissor;

namespace Service.Emissores.DTOs;

public record EmissorForCreationDto(string Nome)
{
  public EmissorModelForCreation ToCreateEmissorCommand() => new(Nome);
}
