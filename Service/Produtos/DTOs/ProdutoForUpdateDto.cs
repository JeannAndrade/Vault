using Application.Produtos.Commands.UpdateProduto;

namespace Service.Produtos.DTOs;

public record ProdutoForUpdateDto(string Nome)
{
  public ProdutoModelForUpdate ToUpdateProdutoCommand() => new() { Nome = Nome };
}
