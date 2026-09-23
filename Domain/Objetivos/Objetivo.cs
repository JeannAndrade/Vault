using Domain.Movimentos;
using LumiaFoundation.Core.Domain;

namespace Domain.Objetivos;

public class Objetivo : Entity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Meta { get; set; } = 0M;
    public string FontePagadora { get; set; } = string.Empty;
    public decimal AporteMensal { get; set; } = 0M;
    public string OndeAplicar { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public ICollection<Movimento> Movimentos { get; set; } = [];
}
