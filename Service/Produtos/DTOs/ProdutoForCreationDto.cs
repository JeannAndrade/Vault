using Application.Produtos.Commands.CreateProduto;

namespace Service.Produtos.DTOs;

public record ProdutoForCreationDto(string Nome)
{
  public ProdutoModelForCreation ToCreateProdutoCommand(Guid ownerId) => new(Nome, ownerId);
}
