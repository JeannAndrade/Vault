using Application.Emissores.Commands.UpdateEmissor;

namespace Service.Emissores.DTOs;

public record EmissorForUpdateDto(string Nome)
{
  public EmissorModelForUpdate ToUpdateEmissorCommand() => new() { Nome = Nome };
}
