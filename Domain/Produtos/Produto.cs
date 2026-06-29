using LumiaFoundation.EFRepository.Domain;

namespace Domain.Produtos
{
    public class Produto(string nome) : Entity
    {
        public string Nome { get; private set; } = nome ?? throw new ArgumentNullException(nameof(nome));
    }
}