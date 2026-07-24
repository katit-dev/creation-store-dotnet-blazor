using CreationStore.API.DTOs.Order;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers
{
    [Authorize]
    [Route("api/orders")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(
            [FromBody] CheckoutOrderDTO dto
        )
        {
            var result = await _orderService.CheckoutAsync(dto);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var result = await _orderService.GetMyOrdersAsync();

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetMyOrderById(int orderId)
        {
            var result = await _orderService.GetMyOrderByIdAsync(orderId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(
            int orderId,
            [FromBody] CancelOrderDTO dto
        )
        {
            var result = await _orderService.CancelOrderAsync(
                orderId,
                dto
            );

            return StatusCode(result.StatusCode, result);
        }
    }
}