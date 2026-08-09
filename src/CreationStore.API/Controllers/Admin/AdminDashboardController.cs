using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/dashboard")]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _adminDashboardService;

        public AdminDashboardController(
            IAdminDashboardService adminDashboardService
        )
        {
            _adminDashboardService = adminDashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _adminDashboardService.GetSummaryAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate
        )
        {
            var result = await _adminDashboardService.GetRevenueAsync(
                fromDate,
                toDate
            );

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts(
            [FromQuery] int take = 5
        )
        {
            var result = await _adminDashboardService.GetTopProductsAsync(
                take
            );

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("recent-orders")]
        public async Task<IActionResult> GetRecentOrders(
            [FromQuery] int take = 10
        )
        {
            var result = await _adminDashboardService.GetRecentOrdersAsync(
                take
            );

            return StatusCode(result.StatusCode, result);
        }
    }
}