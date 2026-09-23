using System.ComponentModel.DataAnnotations;
using Domain.Corretoras;

namespace Application.Corretoras.Commands.CreateCorretora;

public record CorretoraModelForCreation
{
    [Required(ErrorMessage = "Nome da Corretora é obrigatório")]
    [MaxLength(60, ErrorMessage = "Nome da Corretora não deve exceder 60 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O usuário é obrigatório")]
    public Guid UserId { get; set; }

    public Corretora ToDomain() => new()
    {
        Nome = Nome,
        UserId = UserId
    };
}
