namespace Application.TiposRenda.Commands.DeleteTipoRenda;

public interface IDeleteTipoRendaCommand
{
    Task ExecuteAsync(Guid ownerId, Guid tipoRendaId);
}
