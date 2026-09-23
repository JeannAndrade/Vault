using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetObjetivo;

public class GetObjetivoQuery(IRepositoryManager repositoryManager) : IGetObjetivoQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;

  public async Task<ObjetivoModel> ExecuteAsync(Guid ownerId, Guid objetivoId)
  {
    var objetivo = await _repository.Objetivo.GetObjetivoAsync(ownerId, objetivoId, trackChanges: false)
        ?? throw new EntityNotFoundException("Objetivo not found");

    return new ObjetivoModel
    {
      Id = objetivo.Id,
      Nome = objetivo.Nome,
      Descricao = objetivo.Descricao,
      Meta = objetivo.Meta,
      FontePagadora = objetivo.FontePagadora,
      AporteMensal = objetivo.AporteMensal,
      OndeAplicar = objetivo.OndeAplicar,
      UserId = objetivo.UserId
    };
  }
}
