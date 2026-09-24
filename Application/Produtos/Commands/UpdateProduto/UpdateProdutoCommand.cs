using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Produtos.Commands.UpdateProduto;

public class UpdateProdutoCommand(IRepositoryManager repositoryManager) : IUpdateProdutoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<ProdutoModel> ExecuteAsync(ProdutoModelForUpdate produtoModel, Guid ownerId, Guid produtoId)
  {
    CommandValidator.Validate(produtoModel);

    var produto = await _repositoryManager.Produto.GetAsync(ownerId, produtoId, trackChanges: true)
    ?? throw new EntityNotFoundException("Produto not found");

    _repositoryManager.Produto.Update(produtoModel.UpdateDomain(produto));
    await _repositoryManager.SaveAsync();

    return ProdutoModel.FromDomain(produto);
  }
}
