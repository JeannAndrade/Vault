using System.Text.Json.Serialization;

namespace Presentation.Movimentos;

public sealed record MovimentoForEncerramentoDto
{
    [JsonConverter(typeof(JsonStringEnumConverter<MotivoEncerramento>))]
    public MotivoEncerramento Motivo { get; init; }

    public DateTime Data { get; init; }
}
