namespace Persistence.Movimentos;

public sealed record TituloFiltro(
    Guid TipoRendaId,
    Guid CorretoraId,
    Guid ProdutoId,
    Guid EmissorId);
