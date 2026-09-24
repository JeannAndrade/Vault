namespace Application.Produtos.Commands.UpdateProduto;

public interface IUpdateProdutoCommand
{
  Task<ProdutoModel> ExecuteAsync(ProdutoModelForUpdate produtoModel, Guid ownerId, Guid produtoId);
}
