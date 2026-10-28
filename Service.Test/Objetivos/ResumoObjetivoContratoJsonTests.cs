using System.Text.Json;
using Application.Objetivos.Queries.GetResumoObjetivosList;
using Domain.Objetivos.Projecao;
using Service.Objetivos.DTOs;

namespace Service.Test.Objetivos;

public class ResumoObjetivoContratoJsonTests
{
    [Fact]
    public void Serializacao_ExponeProjecaoComEnumComoTextoEAnulaveis()
    {
        var modelo = new ResumoObjetivoModel
        {
            Id = Guid.NewGuid(), Nome = "Reserva", Meta = 12000m, PercentualMeta = 8.3m,
            QtdeMovimentos = 1, TotalAportado = 1000m, TotalLiquido = 1000m,
            Projecao = new ProjecaoObjetivoModel(0, null, 0, 600m, 0, 0, true,
                new(SituacaoProjecao.SemPrevisao, null, null),
                new(SituacaoProjecao.SemPrevisao, null, null),
                new(SituacaoProjecao.Prevista, 19, new DateTime(2028, 5, 1)))
        };

        var json = JsonSerializer.Serialize(ResumoObjetivoDto.FromApplication(modelo), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        var projecao = raiz.GetProperty("projecao");

        Assert.Equal("Reserva", raiz.GetProperty("nome").GetString());
        Assert.True(raiz.TryGetProperty("totalLiquido", out _));
        Assert.Equal("SemPrevisao", projecao.GetProperty("conservador").GetProperty("situacao").GetString());
        Assert.Equal("Prevista", projecao.GetProperty("planejado").GetProperty("situacao").GetString());
        Assert.True(projecao.GetProperty("aporteRealizadoMensal").ValueKind == JsonValueKind.Null);
        Assert.Equal("2028-05-01T00:00:00", projecao.GetProperty("planejado").GetProperty("dataPrevista").GetString());
    }

    [Fact]
    public void Situacoes_ConservamOsQuatroNomesDoContrato()
    {
        Assert.Equal(["Prevista", "Atingida", "MaisDeCemAnos", "SemPrevisao"],
            Enum.GetNames<SituacaoProjecao>());
    }
}
