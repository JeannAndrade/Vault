namespace Domain.Movimentos;

/// <summary>
/// Valores persistidos em texto na coluna MotivoEncerramento.
/// Os nomes são contrato de persistência e devem permanecer estáveis.
/// </summary>
public enum MotivoEncerramento
{
    Gasto,
    Reinvestido,
    Pendente
}
