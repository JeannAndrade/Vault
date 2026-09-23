namespace Application.TiposRenda.Commands.CreateTipoRenda;

public interface ICreateTipoRendaCommand
{
  Task<TipoRendaModel> ExecuteAsync(TipoRendaModelForCreation tipoRendaModel);
}
