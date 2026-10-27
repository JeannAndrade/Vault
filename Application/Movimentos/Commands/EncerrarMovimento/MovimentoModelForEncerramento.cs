using System.ComponentModel.DataAnnotations;
using Domain.Movimentos;

namespace Application.Movimentos.Commands.EncerrarMovimento;

public record MovimentoModelForEncerramento
{
    [Required(ErrorMessage = "Motivo do encerramento é obrigatório")]
    [EnumDataType(typeof(MotivoEncerramento), ErrorMessage = "Motivo do encerramento inválido")]
    public MotivoEncerramento? Motivo { get; init; }

    [Required(ErrorMessage = "Data do encerramento é obrigatória")]
    public DateTime? Data { get; init; }
}
