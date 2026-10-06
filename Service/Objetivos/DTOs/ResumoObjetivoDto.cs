using Application.Objetivos.Queries.GetResumoObjetivosList;

namespace Service.Objetivos.DTOs;

public class ResumoObjetivoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Meta { get; set; }
    public decimal PercentualMeta { get; set; }
    public int QtdeMovimentos { get; set; }
    public decimal TotalAportado { get; set; }
    public decimal TotalLiquido { get; set; }

    public static ResumoObjetivoDto FromApplication(ResumoObjetivoModel objetivo)
    {
        return new ResumoObjetivoDto
        {
            Id = objetivo.Id,
            Nome = objetivo.Nome,
            Meta = objetivo.Meta,
            PercentualMeta = objetivo.PercentualMeta,
            QtdeMovimentos = objetivo.QtdeMovimentos,
            TotalAportado = objetivo.TotalAportado,
            TotalLiquido = objetivo.TotalLiquido
        };
    }

    public static List<ResumoObjetivoDto> FromApplication(List<ResumoObjetivoModel> objetivos)
    {
        return [.. objetivos.Select(FromApplication)];
    }
}
