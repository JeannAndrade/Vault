using Domain.Movimentos;
using Domain.Movimentos.Rentabilidade;

namespace Application.Dashboards.Rentabilidade;

public record RentabilidadeItemModel(
    Guid Id,
    string? ProdutoNome,
    string? EmissorNome,
    string? ObjetivoNome,
    string? CorretoraNome,
    decimal ValorAporte,
    decimal ValorLiquidoAtual,
    int DiasInvestido,
    double Acumulada,
    double Diaria,
    double Anualizada)
{
    public static RentabilidadeItemModel FromDomain(Movimento movimento, ResultadoRentabilidade rentabilidade) => new(
        movimento.Id,
        movimento.Produto?.Nome,
        movimento.Emissor?.Nome,
        movimento.Objetivo?.Nome,
        movimento.Corretora?.Nome,
        movimento.ValorAporte,
        movimento.ValorLiquidoAtual,
        rentabilidade.Dias,
        rentabilidade.Acumulada,
        rentabilidade.Diaria,
        rentabilidade.Anualizada);
}
