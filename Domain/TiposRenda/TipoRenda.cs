using LumiaFoundation.EFRepository.Domain;

namespace Domain.TiposRenda
{
    public class TipoRenda(string nome) : Entity
    {
        public string Nome { get; private set; } = nome ?? throw new ArgumentNullException(nameof(nome));
    }
}