using Microsoft.AspNetCore.Mvc;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Services;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthApplicationService _authApplicationService;

    public AuthController(IAuthApplicationService authApplicationService)
    {
        _authApplicationService = authApplicationService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> RegisterAsync(RegisterDto request, CancellationToken cancellationToken)
    {
        var response = await _authApplicationService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto request, CancellationToken cancellationToken)
    {
        var response = await _authApplicationService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
}
