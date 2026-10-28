using Application.Movimentos.Commands.EncerrarMovimento;
using Domain.Movimentos;
using System.Text.Json.Serialization;

namespace Service.Movimentos.DTOs;

public record MovimentoForEncerramentoDto
{
    [JsonConverter(typeof(JsonStringEnumConverter<MotivoEncerramento>))]
    public MotivoEncerramento? Motivo { get; init; }

    public DateTime? Data { get; init; }

    public MovimentoModelForEncerramento ToEncerrarMovimentoCommand() => new()
    {
        Motivo = Motivo,
        Data = Data
    };
}
