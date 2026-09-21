namespace Application.Produtos.Commands.CreateProduto;

public interface ICreateProdutoCommand
{
  Task<ProdutoModel> ExecuteAsync(ProdutoModelForCreation produtoModel);
}
