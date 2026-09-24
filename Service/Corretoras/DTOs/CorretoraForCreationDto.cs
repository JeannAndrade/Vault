using Application.Corretoras.Commands.CreateCorretora;

namespace Service.Corretoras.DTOs;

public record CorretoraForCreationDto(string Nome)
{
    public CorretoraModelForCreation ToCreateCorretoraCommand() => new() { Nome = Nome };
}
