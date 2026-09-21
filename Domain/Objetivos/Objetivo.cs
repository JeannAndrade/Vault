using Domain.ValueObjects;
using LumiaFoundation.Core.Domain;

namespace Domain.Objetivos;

public class Objetivo : Entity
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public Meta Meta { get; set; }
    public string FontePagadora { get; set; } = string.Empty;
    public AporteMensal AporteMensal { get; set; }
    public string OndeAplicar { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Objetivo()
    {
        Meta = new Meta(0);
        AporteMensal = new AporteMensal(0);
    }

    public Objetivo(string nome, string? descricao, Meta meta, string fontePagadora, AporteMensal aporteMensal, string ondeAplicar)
    {
        Nome = nome;
        Descricao = descricao;
        Meta = meta;
        FontePagadora = fontePagadora;
        AporteMensal = aporteMensal;
        OndeAplicar = ondeAplicar;
    }

    public Objetivo(string nome, Meta meta, string fontePagadora, AporteMensal aporteMensal, string ondeAplicar)
        : this(nome, null, meta, fontePagadora, aporteMensal, ondeAplicar)
    {
    }
}
