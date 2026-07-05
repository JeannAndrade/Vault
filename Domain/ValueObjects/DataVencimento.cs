using LumiaFoundation.Core.Domain;

namespace Domain.ValueObjects
{
    public sealed class DataVencimento : GenericSingleValueObject<DateTime>
    {
        public DataVencimento(DateTime value) : base(value)
        {
            if (value <= DateTime.Today) throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(DataVencimento)} must be in the future");
        }
    }
}