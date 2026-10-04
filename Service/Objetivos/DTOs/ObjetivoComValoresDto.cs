using Application.Objetivos.Queries.GetObjetivoComValoresList;

namespace Service.Objetivos.DTOs;

public class ObjetivoComValoresDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Meta { get; set; }
    public string FontePagadora { get; set; } = string.Empty;
    public decimal AporteMensal { get; set; }
    public string OndeAplicar { get; set; } = string.Empty;
    public decimal ValorTotalInvestido { get; set; }
    public decimal ValorTotalLiquido { get; set; }

    public static ObjetivoComValoresDto FromApplication(ObjetivoComValoresModel objetivo)
    {
        return new ObjetivoComValoresDto
        {
            Id = objetivo.Id,
            Nome = objetivo.Nome,
            Meta = objetivo.Meta,
            FontePagadora = objetivo.FontePagadora,
            AporteMensal = objetivo.AporteMensal,
            OndeAplicar = objetivo.OndeAplicar,
            ValorTotalInvestido = objetivo.ValorTotalInvestido,
            ValorTotalLiquido = objetivo.ValorTotalLiquido
        };
    }

    public static List<ObjetivoComValoresDto> FromApplication(List<ObjetivoComValoresModel> objetivos)
    {
        return [.. objetivos.Select(FromApplication)];
    }
}
