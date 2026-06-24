using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LumiaFoundation.Core.Domain;

namespace Domain.ValueObjects
{
    public sealed class AporteMensal : GenericSingleValueObject<decimal>
    {
        public AporteMensal(decimal value) : base(value)
        {
            if (value <= 0M) throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(AporteMensal)} must be greater than 0");
        }
    }
}