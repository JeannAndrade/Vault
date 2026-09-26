using Application.Corretoras;
using Application.Corretoras.Queries.GetCorretora;
using Application.Emissores;
using Application.Emissores.Queries.GetEmissor;
using Application.Movimentos.Validation;
using Application.Objetivos;
using Application.Objetivos.Queries.GetObjetivo;
using Application.Produtos;
using Application.Produtos.Queries.GetProduto;
using Application.TiposRenda;
using Application.TiposRenda.Queries.GetTipoRenda;
using LumiaFoundation.Core.Domain.Exceptions;

namespace Application.Test.Movimentos.Validation;

public class MovimentoReferenciasValidatorTests
{
    [Fact]
    public async Task ValidarAsync_WhenTodasAsReferenciasExistem_ChamaTodasAsQueriesNaOrdem()
    {
        // Arrange
        var chamadas = new List<string>();
        var validator = CriarValidator(chamadas);

        // Act
        var exception = await Record.ExceptionAsync(() => validator.ValidarAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        Assert.Null(exception);
        Assert.Equal(["Objetivo", "TipoRenda", "Corretora", "Produto", "Emissor"], chamadas);
    }

    [Fact]
    public async Task ValidarAsync_WhenObjetivoNaoExiste_LancaEntityNotFoundExceptionSemChamarAsDemais()
    {
        // Arrange
        var chamadas = new List<string>();
        var validator = CriarValidator(chamadas, objetivoDeveFalhar: true);

        // Act
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => validator.ValidarAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        Assert.Equal("Objetivo not found", exception.Message);
        Assert.Equal(["Objetivo"], chamadas);
    }

    [Fact]
    public async Task ValidarAsync_WhenEmissorNaoExiste_ValidaTodasAsAnterioresAntesDeLancar()
    {
        // Arrange
        var chamadas = new List<string>();
        var validator = CriarValidator(chamadas, emissorDeveFalhar: true);

        // Act
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => validator.ValidarAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        Assert.Equal("Emissor not found", exception.Message);
        Assert.Equal(["Objetivo", "TipoRenda", "Corretora", "Produto", "Emissor"], chamadas);
    }

    private static MovimentoReferenciasValidator CriarValidator(
        List<string> chamadas,
        bool objetivoDeveFalhar = false,
        bool tipoRendaDeveFalhar = false,
        bool corretoraDeveFalhar = false,
        bool produtoDeveFalhar = false,
        bool emissorDeveFalhar = false) =>
        new(
            new FakeGetObjetivoQuery(chamadas, objetivoDeveFalhar),
            new FakeGetTipoRendaQuery(chamadas, tipoRendaDeveFalhar),
            new FakeGetCorretoraQuery(chamadas, corretoraDeveFalhar),
            new FakeGetProdutoQuery(chamadas, produtoDeveFalhar),
            new FakeGetEmissorQuery(chamadas, emissorDeveFalhar));

    private sealed class FakeGetObjetivoQuery(List<string> chamadas, bool deveFalhar) : IGetObjetivoQuery
    {
        public Task<ObjetivoModel> ExecuteAsync(Guid ownerId, Guid objetivoId)
        {
            chamadas.Add("Objetivo");
            return deveFalhar
                ? throw new EntityNotFoundException("Objetivo not found")
                : Task.FromResult(new ObjetivoModel { Id = objetivoId, UserId = ownerId });
        }
    }

    private sealed class FakeGetTipoRendaQuery(List<string> chamadas, bool deveFalhar) : IGetTipoRendaQuery
    {
        public Task<TipoRendaModel> ExecuteAsync(Guid ownerId, Guid tipoRendaId)
        {
            chamadas.Add("TipoRenda");
            return deveFalhar
                ? throw new EntityNotFoundException("Tipo de renda not found")
                : Task.FromResult(new TipoRendaModel { Id = tipoRendaId, UserId = ownerId });
        }
    }

    private sealed class FakeGetCorretoraQuery(List<string> chamadas, bool deveFalhar) : IGetCorretoraQuery
    {
        public Task<CorretoraModel> ExecuteAsync(Guid ownerId, Guid corretoraId)
        {
            chamadas.Add("Corretora");
            return deveFalhar
                ? throw new EntityNotFoundException("Corretora not found")
                : Task.FromResult(new CorretoraModel { Id = corretoraId, UserId = ownerId });
        }
    }

    private sealed class FakeGetProdutoQuery(List<string> chamadas, bool deveFalhar) : IGetProdutoQuery
    {
        public Task<ProdutoModel> ExecuteAsync(Guid ownerId, Guid produtoId)
        {
            chamadas.Add("Produto");
            return deveFalhar
                ? throw new EntityNotFoundException("Produto not found")
                : Task.FromResult(new ProdutoModel { Id = produtoId, UserId = ownerId });
        }
    }

    private sealed class FakeGetEmissorQuery(List<string> chamadas, bool deveFalhar) : IGetEmissorQuery
    {
        public Task<EmissorModel> ExecuteAsync(Guid ownerId, Guid emissorId)
        {
            chamadas.Add("Emissor");
            return deveFalhar
                ? throw new EntityNotFoundException("Emissor not found")
                : Task.FromResult(new EmissorModel { Id = emissorId, UserId = ownerId });
        }
    }
}
