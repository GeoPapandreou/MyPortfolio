using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPortfolioAPI.DTOs;
using MyPortfolioAPI.Extensions;
using MyPortfolioAPI.Services;

namespace MyPortfolioAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public sealed class GenerateController : ControllerBase
{
    private readonly IPortfolioPackageApplicationService _portfolioPackageApplicationService;

    public GenerateController(IPortfolioPackageApplicationService portfolioPackageApplicationService)
    {
        _portfolioPackageApplicationService = portfolioPackageApplicationService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateAsync(GenerateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _portfolioPackageApplicationService.GenerateAsync(User.GetUserId(), request, cancellationToken);
        return File(result.FileBytes, "application/zip", result.FileName);
    }

    [HttpGet("versions/{versionId:guid}/download")]
    public async Task<IActionResult> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var result = await _portfolioPackageApplicationService.DownloadVersionAsync(User.GetUserId(), versionId, cancellationToken);
        return File(result.FileBytes, "application/zip", result.FileName);
    }
}
