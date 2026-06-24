using Domain.Bancos;
using Domain.ValueObjects;
using LumiaFoundation.EFRepository.Domain;

namespace Domain.Objetivos
{
    public class Objetivo : Entity
    {

        public string Nome { get; private set; }
        public string? Descricao { get; private set; }
        public Meta Meta { get; private set; }
        public string FontePagadora { get; private set; }
        public AporteMensal AporteMensal { get; private set; }
        public string OndeAplicar { get; private set; }
        public Banco Banco { get; private set; }

        public Objetivo(string nome, string? descricao, Meta meta, string fontePagadora, AporteMensal aporteMensal, string ondeAplicar, Banco banco)
        {
            Nome = nome;
            Descricao = descricao;
            Meta = meta;
            FontePagadora = fontePagadora;
            AporteMensal = aporteMensal;
            OndeAplicar = ondeAplicar;
            Banco = banco;
        }

        public Objetivo(string nome, Meta meta, string fontePagadora, AporteMensal aporteMensal, string ondeAplicar, Banco banco) : this(nome, null, meta, fontePagadora, aporteMensal, ondeAplicar, banco)
        {

        }
    }
}