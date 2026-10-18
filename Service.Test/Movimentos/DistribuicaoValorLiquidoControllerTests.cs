using LumiaFoundation.AspNetCore.ActionFilters;
using Service.Movimentos;

namespace Service.Test.Movimentos;

public class DistribuicaoValorLiquidoControllerTests
{
    [Fact]
    public void PossuiFiltroDeValidacaoDeRequisicao()
    {
        Assert.Contains(typeof(RequestValidationAttribute), typeof(DistribuicaoValorLiquidoController).GetCustomAttributes(true).Select(a => a.GetType()));
    }
}
