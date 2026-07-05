using LumiaFoundation.Core.Domain;

namespace Domain.ValueObjects
{
    public sealed class ValorLiquidoAtual : GenericSingleValueObject<decimal>
    {
        public ValorLiquidoAtual(decimal value) : base(value)
        {
            if (value < 0M) throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(ValorLiquidoAtual)} must be equal or greater than 0");
        }
    }
}