using Domain.Bancos;

namespace Application.Bancos;

public record BancoModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public static BancoModel FromDomain(Banco banco)
    {
        return new BancoModel
        {
            Id = banco.Id,
            Nome = banco.Nome,
            UserId = banco.UserId
        };
    }
}
