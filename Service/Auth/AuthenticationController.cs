using LumiaFoundation.AspNetCore.ActionFilters;
using LumiaFoundation.AspNetCore.Commons.BaseControllers;
using LumiaFoundation.Auth.DTO;
using LumiaFoundation.Auth.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Service.Auth;

[Route("api/authentication")]
[ApiController]
public class AuthenticationController(IServiceManager service) : BaseApiController
{
    private readonly IServiceManager _service = service;

    [HttpPost]
    [ServiceFilter(typeof(DtoNotEmptyValidationAttribute))]
    public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
    {
        var result = await _service.AuthenticationService.RegisterUser(userForRegistration);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.TryAddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [ServiceFilter(typeof(DtoNotEmptyValidationAttribute))]
    public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDto user)
    {
        if (!await _service.AuthenticationService.ValidateUser(user))
            return Unauthorized();

        return Ok(new { Token = await _service.AuthenticationService.CreateToken(populateExp: true) });
    }
}

