using LumiaFoundation.EFRepository.Domain;

namespace Domain.Emissores
{
    public class Emissor(string nome) : Entity
    {
        public string Nome { get; private set; } = nome ?? throw new ArgumentNullException(nameof(nome));
    }
}