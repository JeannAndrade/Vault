namespace Presentation.Objetivos;

/// <summary>
/// Agregação de apresentação sobre dados já entregues pela Api. Se a lista for paginada ou filtrada, reavaliar conforme o ADR 0014.
/// </summary>
public sealed record TotaisObjetivos(
    int QtdeMovimentos,
    decimal TotalAportado,
    decimal TotalLiquido)
{
    public static TotaisObjetivos Vazio { get; } = new(0, 0m, 0m);

    public static TotaisObjetivos FromResumo(IEnumerable<ResumoObjetivoDto> objetivos)
    {
        ArgumentNullException.ThrowIfNull(objetivos);

        return new TotaisObjetivos(
            objetivos.Sum(objetivo => objetivo.QtdeMovimentos),
            objetivos.Sum(objetivo => objetivo.TotalAportado),
            objetivos.Sum(objetivo => objetivo.TotalLiquido));
    }
}
