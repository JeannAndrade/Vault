namespace Presentation.Objetivos;

public sealed record ObjetivoForCreationDto(
    string Nome,
    string? Descricao,
    decimal Meta,
    string FontePagadora,
    decimal AporteMensal,
    string OndeAplicar,
    bool EstaAtivo);
