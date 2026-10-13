using Application.Movimentos.Queries.GetMovimentoList;

namespace Service.Movimentos.DTOs;

public record MovimentoFiltroDto
{
    public Guid? ObjetivoId { get; init; }
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }
    public bool? EstaAtivo { get; init; }

    public MovimentoFiltroParameters ToParameters() => new()
    {
        ObjetivoId = ObjetivoId,
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId,
        EstaAtivo = EstaAtivo
    };
}
