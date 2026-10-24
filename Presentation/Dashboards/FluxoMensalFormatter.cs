namespace Presentation.Dashboards;

public static class FluxoMensalFormatter
{
    public static string FormatarMes(string? mes)
    {
        if (mes is null) return string.Empty;
        if (mes.Length != 7 || mes[4] != '-' || !int.TryParse(mes[..4], out var ano) || !int.TryParse(mes[5..], out var numeroMes) || ano < 0 || numeroMes is < 1 or > 12)
            return mes;
        return $"{mes[5..]}/{mes[..4]}";
    }
}
