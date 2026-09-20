using LumiaFoundation.AspNetCore.ActionFilters;
using LumiaFoundation.Auth.DTO;
using LumiaFoundation.Auth.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Service.Auth;

[Route("api/token")]
[ApiController]
public class TokenController(IServiceManager service) : ControllerBase
{
    private readonly IServiceManager _service = service;


    [HttpPost("refresh")]
    [ServiceFilter(typeof(DtoNotEmptyValidationAttribute))]
    public async Task<IActionResult> Refresh([FromBody] TokenDto tokenDto)
    {
        var tokenDtoToReturn = await _service.AuthenticationService.RefreshToken(tokenDto);
        return Ok(tokenDtoToReturn);
    }
}
