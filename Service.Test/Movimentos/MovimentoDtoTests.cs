using Application.Movimentos;
using LumiaFoundation.Core.Pagination;
using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class MovimentoDtoTests
{
    [Fact]
    public void FromApplication_MapsDataVencimento()
    {
        var vencimento = new DateTime(2030, 1, 15);
        var model = new MovimentoModel { Id = Guid.NewGuid(), DataVencimento = vencimento };

        var dto = MovimentoDto.FromApplication(model);

        Assert.Equal(vencimento, dto.DataVencimento);
    }

    [Fact]
    public void FromApplication_WhenDataVencimentoIsNull_MapsAsNull()
    {
        // Ações, por exemplo, não têm vencimento.
        var model = new MovimentoModel { Id = Guid.NewGuid(), DataVencimento = null };

        var dto = MovimentoDto.FromApplication(model);

        Assert.Null(dto.DataVencimento);
    }

    [Fact]
    public void FromApplication_ComPagedList_PreservaItensEMetadados()
    {
        var model = new MovimentoModel { Id = Guid.NewGuid() };
        var pagina = new PagedList<MovimentoModel>([model], page: 3, pageSize: 5, totalCount: 11);

        var resposta = MovimentoDto.FromApplication(pagina);

        Assert.Equal(model.Id, Assert.Single(resposta.Items).Id);
        Assert.Equal(3, resposta.Page);
        Assert.Equal(5, resposta.PageSize);
        Assert.Equal(11, resposta.TotalCount);
        Assert.Equal(3, resposta.TotalPages);
    }
}
