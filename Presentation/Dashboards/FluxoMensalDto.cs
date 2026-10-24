namespace Presentation.Dashboards;

public sealed record RetornoMensalDto(string Mes, decimal Total, int Quantidade);
public sealed record InvestidoMensalDto(string Mes, decimal Novo, decimal Reinvestimento, decimal Total);
