using Application.Objetivos.Queries.GetResumoObjetivosList;

namespace Service.Objetivos.DTOs;

public class ResumoObjetivoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Meta { get; set; }
    // Desde o ADR 0021, os totais incluem Movimentos encerrados como Pendente.
    public decimal PercentualMeta { get; set; }
    public int QtdeMovimentos { get; set; }
    public decimal TotalAportado { get; set; }
    public decimal TotalLiquido { get; set; }
    public ProjecaoObjetivoDto Projecao { get; set; } = ProjecaoObjetivoDto.FromApplication(ProjecaoObjetivoModel.Vazia);

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
            TotalLiquido = objetivo.TotalLiquido,
            Projecao = ProjecaoObjetivoDto.FromApplication(objetivo.Projecao)
        };
    }

    public static List<ResumoObjetivoDto> FromApplication(List<ResumoObjetivoModel> objetivos)
    {
        return [.. objetivos.Select(FromApplication)];
    }
}
