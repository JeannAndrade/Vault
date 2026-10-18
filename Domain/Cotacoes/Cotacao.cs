using LumiaFoundation.Core.Domain;

namespace Domain.Cotacoes;

public class Cotacao : Entity
{
    public Guid UserId { get; set; }
    public string Papel { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
}
