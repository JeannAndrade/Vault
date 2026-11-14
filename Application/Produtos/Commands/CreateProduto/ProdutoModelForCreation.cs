using System.ComponentModel.DataAnnotations;
using Domain.Produtos;

namespace Application.Produtos.Commands.CreateProduto;

public record ProdutoModelForCreation(
    [property: Required(ErrorMessage = "Nome do Produto é obrigatório")]
    [property: MaxLength(60, ErrorMessage = "Nome do Produto não deve exceder 60 caracteres")]
    string Nome)
{
  public Produto ToDomain(Guid ownerId) => new()
  {
    Nome = Nome,
    OwnerId = ownerId
  };
}
