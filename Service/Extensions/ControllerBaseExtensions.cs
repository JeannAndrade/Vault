using LumiaFoundation.AspNetCore.Commons.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Service.Extensions;

internal static class ControllerBaseExtensions
{
    internal static void ValidarCorpo(this ControllerBase controller, object? dto)
    {
        if (dto is null || !controller.ModelState.IsValid)
            throw new HttpBaseException(StatusCodes.Status422UnprocessableEntity, "Corpo da requisição inválido.");
    }
}
