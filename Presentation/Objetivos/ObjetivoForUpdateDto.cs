namespace Presentation.Objetivos;

public sealed record ObjetivoForUpdateDto(
    string Nome,
    string? Descricao,
    decimal Meta,
    string FontePagadora,
    decimal AporteMensal,
    string OndeAplicar);
