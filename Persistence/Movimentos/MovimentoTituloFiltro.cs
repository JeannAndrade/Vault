namespace Persistence.Movimentos;

public sealed record MovimentoTituloFiltro
{
    public required Guid TipoRendaId { get; init; }
    public required Guid CorretoraId { get; init; }
    public required Guid ProdutoId { get; init; }
    public required Guid EmissorId { get; init; }
}
