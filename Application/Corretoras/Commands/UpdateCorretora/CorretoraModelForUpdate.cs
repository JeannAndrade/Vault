using System.ComponentModel.DataAnnotations;
using Domain.Corretoras;

namespace Application.Corretoras.Commands.UpdateCorretora;

public record CorretoraModelForUpdate
{
    [Required(ErrorMessage = "Nome da Corretora é obrigatório")]
    [MaxLength(60, ErrorMessage = "Nome da Corretora não deve exceder 60 caracteres")]
    public string Nome { get; set; } = string.Empty;

    public Corretora UpdateDomain(Corretora corretora)
    {
        corretora.Nome = Nome;
        return corretora;
    }
}
