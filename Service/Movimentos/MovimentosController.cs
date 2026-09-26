using Application.Emissores.Commands.CreateEmissor;
using Application.Emissores.Commands.UpdateEmissor;
using Application.Emissores.Queries.GetEmissor;
using Application.Emissores.Queries.GetEmissorList;
using LumiaFoundation.Abstractions.ErrorModel;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Emissores.DTOs;

namespace Service.Movimentos;

[ApiController]
[Authorize]
[ServiceFilter(typeof(RetrieveUserIdFromTokenAttribute))]
[Route("api/movimentos")]
public class MovimentosController : BaseApiController
{

}
