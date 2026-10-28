using System.Text.Json.Serialization;

namespace Presentation.Objetivos;

public sealed record CenarioProjecaoDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter<SituacaoProjecao>))] SituacaoProjecao Situacao,
    int? Meses,
    DateTime? DataPrevista);
