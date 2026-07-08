using CreationStore.API.DTOs.Products;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // ==========================
        // PUBLIC / MEMBER
        // ==========================
        // GET: /api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var result = await _productService.GetAllProductsAsync();

            return StatusCode(result.StatusCode, result);
        }

        // GET: /api/products/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var result = await _productService.GetProductByIdAsync(id);

            return StatusCode(result.StatusCode, result);
        }

        // GET: /api/products/category/1
        [HttpGet("category/{categoryId:int}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var result = await _productService.GetProductsByCategoryAsync(categoryId);

            return StatusCode(result.StatusCode, result);
        }

        // GET: /api/products/search?keyword=spotify
        // GET: /api/products/search
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? keyword)
        {
            var result = await _productService.SearchProductsAsync(keyword);

            return StatusCode(result.StatusCode, result);
        }

        // GET: /api/products/filter?categoryId=1&keyword=spotify&minPrice=100000&maxPrice=500000
        // GET: /api/products/filter
        [HttpGet("filter")]
        public async Task<IActionResult> FilterProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice
        )
        {
            var result = await _productService.FilterProductsAsync(
                categoryId,
                keyword,
                minPrice,
                maxPrice
            );

            return StatusCode(result.StatusCode, result);
        }

        // ==========================
        // ADMIN
        // ==========================
        // POST: /api/products
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDTO dto)
        {
            var result = await _productService.CreateProductAsync(dto);

            return StatusCode(result.StatusCode, result);
        }

        // PUT: /api/products/1
        // ==========================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDTO dto)
        {
            var result = await _productService.UpdateProductAsync(id, dto);

            return StatusCode(result.StatusCode, result);
        }

        // DELETE: /api/products/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);

            return StatusCode(result.StatusCode, result);
        }
    }
}