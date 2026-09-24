namespace Application.TiposRenda.Commands.UpdateTipoRenda;

public interface IUpdateTipoRendaCommand
{
  Task<TipoRendaModel> ExecuteAsync(TipoRendaModelForUpdate tipoRendaModel, Guid ownerId, Guid tipoRendaId);
}
