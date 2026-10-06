namespace Presentation.Objetivos;

public sealed record ResumoObjetivoDto(
    Guid Id,
    string Nome,
    decimal Meta,
    decimal PercentualMeta,
    int QtdeMovimentos,
    decimal TotalAportado,
    decimal TotalLiquido);
