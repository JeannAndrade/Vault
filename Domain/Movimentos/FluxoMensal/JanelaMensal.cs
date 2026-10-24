namespace Domain.Movimentos.FluxoMensal;

public static class JanelaMensal
{
    public const int QuantidadeMeses = 12;

    public static IReadOnlyList<IntervaloMensal> Proximos(DateTime hoje) => Criar(hoje, 0);

    public static IReadOnlyList<IntervaloMensal> Anteriores(DateTime hoje) => Criar(hoje, -QuantidadeMeses + 1);

    private static IReadOnlyList<IntervaloMensal> Criar(DateTime hoje, int primeiroDeslocamento)
    {
        var primeiroDoMes = new DateTime(hoje.Year, hoje.Month, 1);
        return Enumerable.Range(primeiroDeslocamento, QuantidadeMeses)
            .Select(deslocamento =>
            {
                var inicio = primeiroDoMes.AddMonths(deslocamento);
                return new IntervaloMensal(inicio.Year, inicio.Month, inicio, primeiroDoMes.AddMonths(deslocamento + 1));
            })
            .ToList();
    }
}
