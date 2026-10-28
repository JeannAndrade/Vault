using System.Text.Json.Serialization;
using Application.Objetivos.Queries.GetResumoObjetivosList;
using Domain.Objetivos.Projecao;

namespace Service.Objetivos.DTOs;

public class CenarioProjecaoDto
{
    [JsonConverter(typeof(JsonStringEnumConverter<SituacaoProjecao>))]
    public SituacaoProjecao Situacao { get; set; }
    public int? Meses { get; set; }
    public DateTime? DataPrevista { get; set; }

    public static CenarioProjecaoDto FromApplication(CenarioProjecaoModel c) => new()
    {
        Situacao = c.Situacao, Meses = c.Meses, DataPrevista = c.DataPrevista
    };
}
