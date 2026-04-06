using AdminService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AdminAppService _svc;
    public AdminController(AdminAppService svc) => _svc = svc;

    private void ForwardToken() =>
        _svc.SetAuthToken(Request.Headers["Authorization"].ToString());

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        ForwardToken();
        return Ok(await _svc.GetDashboardAsync());
    }

    [HttpGet("reports/sales")]
    public async Task<IActionResult> SalesReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        ForwardToken();
        var dateFrom = from ?? DateTime.UtcNow.AddDays(-30);
        var dateTo = to ?? DateTime.UtcNow;
        return Ok(await _svc.GetSalesReportAsync(dateFrom, dateTo));
    }

    [HttpGet("reports/partners")]
    public async Task<IActionResult> PartnerReport()
    {
        ForwardToken();
        return Ok(await _svc.GetPartnerReportAsync());
    }
}
