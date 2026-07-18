using CreationStore.API.DTOs.Cart;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers
{
    [Authorize]
    [Route("api/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var result = await _cartService.GetMyCartAsync();

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemDTO dto)
        {
            var result = await _cartService.AddItemAsync(dto);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateItem(
            int cartItemId,
            [FromBody] UpdateCartItemDTO dto
        )
        {
            var result = await _cartService.UpdateItemAsync(
                cartItemId,
                dto
            );

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var result = await _cartService.RemoveItemAsync(cartItemId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartService.ClearCartAsync();

            return StatusCode(result.StatusCode, result);
        }
    }
}