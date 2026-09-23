using Application.Corretoras;

namespace Service.Corretoras.DTOs;

public class CorretoraDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    public static CorretoraDto FromApplication(CorretoraModel corretora)
    {
        return new CorretoraDto
        {
            Id = corretora.Id,
            Nome = corretora.Nome
        };
    }

    public static List<CorretoraDto> FromApplication(List<CorretoraModel> corretoras)
    {
        return [.. corretoras.Select(FromApplication)];
    }
}
