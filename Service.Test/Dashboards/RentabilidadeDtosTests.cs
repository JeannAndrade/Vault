using Application.Dashboards.Rentabilidade;
using Service.Dashboards.DTOs;

namespace Service.Test.Dashboards;

public class RentabilidadeDtosTests
{
    [Fact]
    public void FiltroSemArgumentosUsaPadroesECopiaParametros()
    {
        var filtro = new RentabilidadeFiltroDto();
        var parametros = filtro.ToParameters();

        Assert.Equal(25, filtro.Top);
        Assert.Equal(OrdemRentabilidade.Maiores, filtro.Ordem);
        Assert.Equal(filtro.Top, parametros.Top);
        Assert.Equal(filtro.Ordem, parametros.Ordem);
    }

    [Fact]
    public void FromApplicationMapeiaTodosOsCamposEPreservaOrdem()
    {
        var primeiro = new RentabilidadeItemModel(
            Guid.NewGuid(), "Produto 1", "Emissor 1", "Objetivo 1", "Corretora 1",
            1000, 1100, 365, 0.1, 0.0002, 0.1);
        var segundo = primeiro with { Id = Guid.NewGuid(), ProdutoNome = "Produto 2" };
        var grupos = new List<RentabilidadeGrupoModel>
        {
            new(Guid.NewGuid(), "CDB", [primeiro, segundo]),
            new(Guid.NewGuid(), "LCI", [])
        };

        var resposta = RentabilidadeGrupoDto.FromApplication(grupos);

        Assert.Equal(grupos.Count, resposta.Count);
        Assert.Equal("CDB", resposta[0].TipoRendaNome);
        Assert.Equal(primeiro.Id, resposta[0].Itens[0].Id);
        Assert.Equal(segundo.Id, resposta[0].Itens[1].Id);
        Assert.Equal(primeiro.ValorAporte, resposta[0].Itens[0].ValorAporte);
        Assert.Equal(primeiro.ValorLiquidoAtual, resposta[0].Itens[0].ValorLiquidoAtual);
        Assert.Equal(primeiro.DiasInvestido, resposta[0].Itens[0].DiasInvestido);
        Assert.Equal(primeiro.Acumulada, resposta[0].Itens[0].Acumulada);
        Assert.Equal(primeiro.Diaria, resposta[0].Itens[0].Diaria);
        Assert.Equal(primeiro.Anualizada, resposta[0].Itens[0].Anualizada);
        Assert.Equal("LCI", resposta[1].TipoRendaNome);
    }
}
