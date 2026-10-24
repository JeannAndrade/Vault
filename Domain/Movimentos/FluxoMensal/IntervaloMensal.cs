namespace Domain.Movimentos.FluxoMensal;

public sealed record IntervaloMensal(int Ano, int Mes, DateTime Inicio, DateTime Fim)
{
    public bool Contem(DateTime data) => Inicio <= data && data < Fim;
}
