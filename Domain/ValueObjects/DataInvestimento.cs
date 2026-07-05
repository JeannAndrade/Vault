using LumiaFoundation.Core.Domain;

namespace Domain.ValueObjects
{
    public sealed class DataInvestimento : GenericSingleValueObject<DateTime>
    {
        public DataInvestimento(DateTime value) : base(value)
        {
            if (value > DateTime.Today) throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(DataInvestimento)} must be in the past");
        }
    }
}