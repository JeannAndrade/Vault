namespace Domain.Movimentos.Rentabilidade;

public sealed record ResultadoRentabilidade(
    double Acumulada,
    double Diaria,
    double Anualizada,
    int Dias)
{
    public bool ElegivelParaRanking => Dias >= RentabilidadeCalculador.DiasMinimosParaRanking;
}
