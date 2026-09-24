using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetObjetivo;

public class GetObjetivoQuery(IRepositoryManager repositoryManager) : IGetObjetivoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<ObjetivoModel> ExecuteAsync(Guid ownerId, Guid objetivoId)
    {
        var objetivo = await _repository.Objetivo.GetAsync(ownerId, objetivoId, trackChanges: false)
            ?? throw new EntityNotFoundException("Objetivo not found");

        return ObjetivoModel.FromDomain(objetivo);
    }
}
