using CreationStore.API.DTOs.Order;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/orders")]
    [ApiController]
    public class AdminOrderController : ControllerBase
    {
        private readonly IAdminOrderService _adminOrderService;

        public AdminOrderController(IAdminOrderService adminOrderService)
        {
            _adminOrderService = adminOrderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var result = await _adminOrderService.GetAllOrdersAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var result = await _adminOrderService.GetOrderByIdAsync(orderId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{orderId:int}/complete")]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var result = await _adminOrderService.CompleteOrderAsync(orderId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(
            int orderId,
            [FromBody] CancelOrderDTO dto
        )
        {
            var result = await _adminOrderService.CancelOrderAsync(
                orderId,
                dto
            );

            return StatusCode(result.StatusCode, result);
        }
    }
}