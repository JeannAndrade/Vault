using LumiaFoundation.Core.Domain;

namespace Domain.ValueObjects
{
    public sealed class Meta : GenericSingleValueObject<decimal>
    {
        public Meta(decimal value) : base(value)
        {
            if (value <= 0M) throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(Meta)} must be greater than 0");
        }
    }
}