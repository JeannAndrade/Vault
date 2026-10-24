namespace Application.Dashboards.FluxoMensal;

public sealed record RetornoMensalModel(int Ano, int Mes, decimal Total, int Quantidade);

public sealed record InvestidoMensalModel(int Ano, int Mes, decimal Novo, decimal Reinvestimento)
{
    public decimal Total => Novo + Reinvestimento;
}
