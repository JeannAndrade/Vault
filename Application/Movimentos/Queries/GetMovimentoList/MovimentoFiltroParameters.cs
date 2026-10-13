using Persistence.Movimentos;

namespace Application.Movimentos.Queries.GetMovimentoList;

public record MovimentoFiltroParameters
{
    public Guid? ObjetivoId { get; init; }
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }
    public bool? EstaAtivo { get; init; }

    public MovimentoFiltro ToMovimentoFiltro() => new()
    {
        ObjetivoId = ObjetivoId,
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId,
        EstaAtivo = EstaAtivo
    };
}
