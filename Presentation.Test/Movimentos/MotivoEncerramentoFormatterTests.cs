using Presentation.Movimentos;

namespace Presentation.Test.Movimentos;

public class MotivoEncerramentoFormatterTests
{
    [Theory]
    [InlineData(MotivoEncerramento.Gasto, "Gasto")]
    [InlineData(MotivoEncerramento.Reinvestido, "Reinvestido")]
    [InlineData(MotivoEncerramento.Pendente, "Pendente (sem destino definido)")]
    public void Descricao_FormataMotivo(MotivoEncerramento motivo, string esperado) =>
        Assert.Equal(esperado, MotivoEncerramentoFormatter.Descricao(motivo));

    [Fact]
    public void Descricao_NuloRetornaTextoVazio() =>
        Assert.Equal(string.Empty, MotivoEncerramentoFormatter.Descricao(null));

    [Fact]
    public void Descricao_CobreTodosOsValoresDoEnum()
    {
        foreach (var motivo in Enum.GetValues<MotivoEncerramento>())
            Assert.False(string.IsNullOrWhiteSpace(MotivoEncerramentoFormatter.Descricao(motivo)));
    }

    [Fact]
    public void Enum_ContemExatamenteOsNomesDoContratoDaApi()
    {
        Assert.Equal(["Gasto", "Reinvestido", "Pendente"], Enum.GetNames<MotivoEncerramento>());
    }
}
