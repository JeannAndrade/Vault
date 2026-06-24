using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LumiaFoundation.EFRepository.Domain;

namespace Domain.Bancos
{
    public class Banco(string nome) : Entity
    {
        public string Nome { get; private set; } = nome ?? throw new ArgumentNullException(nameof(nome));
    }
}