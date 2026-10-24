using Application.Dashboards.FluxoMensal;
using System.Globalization;

namespace Service.Dashboards.DTOs;

public sealed record RetornoMensalDto(string Mes, decimal Total, int Quantidade)
{
    public static RetornoMensalDto FromApplication(RetornoMensalModel model) =>
        new(FluxoMensalDtoHelper.MontarMes(model.Ano, model.Mes), model.Total, model.Quantidade);

    public static List<RetornoMensalDto> FromApplication(List<RetornoMensalModel> models) =>
        [.. models.Select(FromApplication)];

}

public sealed record InvestidoMensalDto(string Mes, decimal Novo, decimal Reinvestimento, decimal Total)
{
    public static InvestidoMensalDto FromApplication(InvestidoMensalModel model) =>
        new(FluxoMensalDtoHelper.MontarMes(model.Ano, model.Mes), model.Novo, model.Reinvestimento, model.Total);

    public static List<InvestidoMensalDto> FromApplication(List<InvestidoMensalModel> models) =>
        [.. models.Select(FromApplication)];

}

internal static class FluxoMensalDtoHelper
{
    public static string MontarMes(int ano, int mes) =>
        string.Create(CultureInfo.InvariantCulture, $"{ano:D4}-{mes:D2}");
}
