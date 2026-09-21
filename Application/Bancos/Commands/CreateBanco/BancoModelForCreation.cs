using System.ComponentModel.DataAnnotations;
using Domain.Bancos;

namespace Application.Bancos.Commands.CreateBanco;

public record BancoModelForCreation(
    [property: Required(ErrorMessage = "Nome do Banco é obrigatório")]
    [property: MaxLength(60, ErrorMessage = "Nome do Banco não deve exceder 60 caracteres")]
    string Nome,

    [property: Required(ErrorMessage = "O usuário é obrigatório")]
    Guid UserId)
{
    public Banco ToDomain() => new()
    {
        Nome = Nome,
        UserId = UserId
    };
}
