namespace Application.Produtos.Commands.DeleteProduto;

public interface IDeleteProdutoCommand
{
    Task ExecuteAsync(Guid ownerId, Guid produtoId);
}
