using LumiaFoundation.Core.Domain;

namespace Domain.ValueObjects
{
    public sealed class ValorAporte : GenericSingleValueObject<decimal>
    {
        public ValorAporte(decimal value) : base(value)
        {
            if (value <= 0M) throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(ValorAporte)} must be greater than 0");
        }
    }
}