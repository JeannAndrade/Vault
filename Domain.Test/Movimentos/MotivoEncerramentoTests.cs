using Domain.Movimentos;

namespace Domain.Test.Movimentos;

public class MotivoEncerramentoTests
{
    [Fact]
    public void EnumExibeOsValoresEsperadosEmOrdem()
    {
        var nomes = Enum.GetNames<MotivoEncerramento>();

        Assert.Equal(new[] { "Gasto", "Reinvestido", "Pendente" }, nomes);
    }
}
