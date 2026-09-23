using Application.Corretoras.Commands.CreateCorretora;

namespace Service.Corretoras.DTOs;

public record CorretoraForCreationDto(string Nome, Guid UserId);

public static class CorretoraForCreationDtoExtensions
{
    public static CorretoraModelForCreation ToCreateCorretoraCommand(this CorretoraForCreationDto dto) => new() { Nome = dto.Nome };
}
