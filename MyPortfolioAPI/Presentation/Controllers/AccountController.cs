using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Extensions;
using MyPortfolioAPI.Services;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountApplicationService _accountApplicationService;

    public AccountController(IAccountApplicationService accountApplicationService)
    {
        _accountApplicationService = accountApplicationService;
    }

    [HttpGet]
    public async Task<ActionResult<AccountSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var account = await _accountApplicationService.GetAsync(User.GetUserId(), cancellationToken);
        return Ok(account);
    }

    [HttpPut]
    public async Task<ActionResult<AccountSettingsDto>> PutAsync(AccountSettingsDto request, CancellationToken cancellationToken)
    {
        var account = await _accountApplicationService.UpdateAsync(User.GetUserId(), request, cancellationToken);
        return Ok(account);
    }

    [HttpDelete("versions/{versionId:guid}")]
    public async Task<ActionResult<AccountSettingsDto>> DeletePortfolioVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var account = await _accountApplicationService.DeletePortfolioVersionAsync(User.GetUserId(), versionId, cancellationToken);
        return Ok(account);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(CancellationToken cancellationToken)
    {
        await _accountApplicationService.DeleteAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
