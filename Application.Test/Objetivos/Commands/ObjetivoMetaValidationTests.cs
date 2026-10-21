using System.ComponentModel.DataAnnotations;
using Application.Objetivos.Commands.CreateObjetivo;
using Application.Objetivos.Commands.UpdateObjetivo;

namespace Application.Test.Objetivos.Commands;

public class ObjetivoMetaValidationTests
{
    [Fact]
    public void CreationModel_AcceptsMetaWithMaximumPrecisionAndScale()
    {
        var model = new ObjetivoModelForCreation(
            "Objetivo",
            null,
            9999999999999999.99m,
            "Salário",
            100m,
            "Poupança",
            true,
            Guid.NewGuid());

        Assert.Empty(Validate(model));
    }

    [Theory]
    [InlineData("10000000000000000")]
    [InlineData("100.001")]
    public void CreationModel_RejectsMetaOutsidePrecisionOrScale(string meta)
    {
        var model = new ObjetivoModelForCreation(
            "Objetivo",
            null,
            decimal.Parse(meta, System.Globalization.CultureInfo.InvariantCulture),
            "Salário",
            100m,
            "Poupança",
            true,
            Guid.NewGuid());

        Assert.NotEmpty(Validate(model));
    }

    [Fact]
    public void UpdateModel_RejectsMetaWithMoreThanTwoDecimalPlaces()
    {
        var model = new ObjetivoModelForUpdate
        {
            Nome = "Objetivo",
            Meta = 100.001m,
            FontePagadora = "Salário",
            AporteMensal = 100m,
            OndeAplicar = "Poupança"
        };

        Assert.Contains(Validate(model), error => error.MemberNames.Contains(nameof(model.Meta)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
