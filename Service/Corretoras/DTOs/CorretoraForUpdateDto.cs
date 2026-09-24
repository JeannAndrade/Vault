using Application.Corretoras.Commands.UpdateCorretora;

namespace Service.Corretoras.DTOs;

public record CorretoraForUpdateDto(string Nome)
{
    public CorretoraModelForUpdate ToUpdateCorretoraCommand() => new() { Nome = Nome };
}
