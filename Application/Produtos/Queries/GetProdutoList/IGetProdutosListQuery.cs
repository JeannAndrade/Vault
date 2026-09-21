namespace Application.Produtos.Queries.GetProdutoList;

public interface IGetProdutosListQuery
{
  Task<List<ProdutoModel>> ExecuteAsync(Guid ownerId);
}
