using CreationStore.API.DTOs.Products;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/products")]
    [ApiController]
    public class AdminProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public AdminProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDTO dto)
        {
            var result = await _productService.CreateProductAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromBody] ProductUpdateDTO dto
        )
        {
            var result = await _productService.UpdateProductAsync(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}