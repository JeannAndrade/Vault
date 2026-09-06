using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Bancos;

namespace Persistence.Bancos
{
    public interface IBancoRepository
    {
        Task<IEnumerable<Banco>> GetAllBancosAsync(bool trackChanges);
        Task<Banco?> GetBancoAsync(Guid bancoId, bool trackChanges);
        void CreateBanco(Banco banco);
    }
}