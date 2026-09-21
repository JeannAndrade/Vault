namespace Application.Produtos.Queries.GetProduto;

public interface IGetProdutoQuery
{
  Task<ProdutoModel> ExecuteAsync(Guid ownerId, Guid produtoId);
}
