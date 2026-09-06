using LumiaFoundation.EFRepository.Domain;

namespace Domain.Bancos
{
    public class Banco : Entity
    {
        public string Nome { get; set; } = string.Empty;
    }
}