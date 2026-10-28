using Application.Objetivos.Queries.GetResumoObjetivosList;
using Domain.Objetivos.Projecao;
using Service.Objetivos.DTOs;

namespace Service.Test.Objetivos;

public class ProjecaoObjetivoDtoTests
{
    [Fact]
    public void FromApplication_CopiaTodosOsCamposEUnificaOsCenarios()
    {
        var data = new DateTime(2028, 5, 1);
        var modelo = new ProjecaoObjetivoModel(50m, null, 9, 600m, .3, .02, false,
            new(SituacaoProjecao.Prevista, 19, data),
            new(SituacaoProjecao.Atingida, null, null),
            new(SituacaoProjecao.MaisDeCemAnos, null, null));

        var dto = ProjecaoObjetivoDto.FromApplication(modelo);

        Assert.Equal(modelo.SaldoPendente, dto.SaldoPendente);
        Assert.Null(dto.AporteRealizadoMensal);
        Assert.Equal(modelo.MesesObservados, dto.MesesObservados);
        Assert.Equal(modelo.AportePlanejadoMensal, dto.AportePlanejadoMensal);
        Assert.Equal(modelo.TaxaAnualEstimada, dto.TaxaAnualEstimada);
        Assert.Equal(modelo.TaxaMensalEstimada, dto.TaxaMensalEstimada);
        Assert.False(dto.BaixaConfianca);
        Assert.Equal(SituacaoProjecao.Prevista, dto.Conservador.Situacao);
        Assert.Equal(19, dto.Conservador.Meses);
        Assert.Equal(data, dto.Conservador.DataPrevista);
        Assert.Null(dto.Realista.Meses);
        Assert.Null(dto.Planejado.DataPrevista);
    }

    [Fact]
    public void ResumoSemProjecao_UsaValorSeguro()
    {
        var dto = ResumoObjetivoDto.FromApplication(new ResumoObjetivoModel { Nome = "Reserva" });

        Assert.NotNull(dto.Projecao);
        Assert.All(new[] { dto.Projecao.Conservador, dto.Projecao.Realista, dto.Projecao.Planejado },
            c => Assert.Equal(SituacaoProjecao.SemPrevisao, c.Situacao));
    }
}
