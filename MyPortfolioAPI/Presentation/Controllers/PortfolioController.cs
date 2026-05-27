using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Extensions;
using MyPortfolioAPI.Services;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public sealed class PortfolioController : ControllerBase
{
    private readonly IPortfolioApplicationService _portfolioApplicationService;

    public PortfolioController(IPortfolioApplicationService portfolioApplicationService)
    {
        _portfolioApplicationService = portfolioApplicationService;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetAsync(CancellationToken cancellationToken)
    {
        var profile = await _portfolioApplicationService.GetAsync(User.GetUserId(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> PutAsync(UserProfileDto request, CancellationToken cancellationToken)
    {
        var profile = await _portfolioApplicationService.SaveAsync(User.GetUserId(), request, cancellationToken);
        return Ok(profile);
    }
}
