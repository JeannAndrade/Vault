namespace Presentation.Dashboards;

public static class ConsolidadoFormatter
{
    public static string RotuloDimensao(DimensaoConsolidado? dimensao) => dimensao switch
    {
        DimensaoConsolidado.TipoRenda => "Tipo de Renda",
        DimensaoConsolidado.Corretora => "Corretora",
        DimensaoConsolidado.Produto => "Produto",
        DimensaoConsolidado.Emissor => "Emissor",
        _ => string.Empty
    };

    public static string Percentual(decimal fracao) => RentabilidadeFormatter.Percentual((double)fracao);

    public static string NomeDoGrupo(string? nome) => string.IsNullOrEmpty(nome) ? "—" : nome;
}
