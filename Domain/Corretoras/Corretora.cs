using LumiaFoundation.EFRepository.Domain;

namespace Domain.Corretoras
{
    public class Corretora(string nome) : Entity
    {
        public string Nome { get; private set; } = nome ?? throw new ArgumentNullException(nameof(nome));
    }
}