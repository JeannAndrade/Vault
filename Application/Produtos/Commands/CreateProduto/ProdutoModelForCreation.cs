using System.ComponentModel.DataAnnotations;
using Domain.Produtos;

namespace Application.Produtos.Commands.CreateProduto;

public record ProdutoModelForCreation(
    [property: Required(ErrorMessage = "Nome do Produto é obrigatório")]
    [property: MaxLength(60, ErrorMessage = "Nome do Produto não deve exceder 60 caracteres")]
    string Nome,

    [property: Required(ErrorMessage = "O usuário é obrigatório")]
    Guid UserId)
{
  public Produto ToDomain() => new()
  {
    Nome = Nome,
    UserId = UserId
  };
}
