using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Core.ValidationAttributes;
using Persistence.Movimentos;

namespace Application.Movimentos.DistribuicaoValorLiquido;

public record DistribuicaoValorLiquidoParameters
{
    [NotEmptyGuid] public Guid TipoRendaId { get; init; }
    [NotEmptyGuid] public Guid CorretoraId { get; init; }
    [NotEmptyGuid] public Guid ProdutoId { get; init; }
    [NotEmptyGuid] public Guid EmissorId { get; init; }
    [Required, Range(0, double.MaxValue, ErrorMessage = "O valor líquido total não pode ser negativo")]
    public decimal ValorLiquidoTotal { get; init; }
    public MovimentoTituloFiltro ToTituloFiltro() => new() { TipoRendaId = TipoRendaId, CorretoraId = CorretoraId, ProdutoId = ProdutoId, EmissorId = EmissorId };
}
