using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Admin;

namespace SharingSystem.Web.Areas.Admin.Controllers;

[Route("admin")]
public sealed class DashboardController(IDashboardService dashboardService) : AdminController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboardService.GetStatsAsync(cancellationToken));
}
