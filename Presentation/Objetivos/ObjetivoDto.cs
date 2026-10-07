namespace Presentation.Objetivos;

public sealed record ObjetivoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Meta,
    string FontePagadora,
    decimal AporteMensal,
    string OndeAplicar,
    bool EstaAtivo);
