namespace Persistence.Movimentos;

public sealed record MovimentoFiltro
{
    public Guid? ObjetivoId { get; init; }
    public Guid? TipoRendaId { get; init; }
    public Guid? CorretoraId { get; init; }
    public Guid? ProdutoId { get; init; }
    public Guid? EmissorId { get; init; }
    public bool? EstaAtivo { get; init; }

    public static MovimentoFiltro Vazio { get; } = new();
}
