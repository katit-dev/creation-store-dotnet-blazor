using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Products;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
//using CreationStore.API.Models;

namespace CreationStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // ==========================
        // MEMBER
        // ==========================

        // GET: /api/products
        // Lấy danh sách sản phẩm đang active
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();

            return Ok(products);
        }

        // GET: /api/products/1
        // Lấy chi tiết sản phẩm theo id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            return Ok(product);
        }

        // GET: /api/products/category/1
        // Lọc sản phẩm theo danh mục
        [HttpGet("category/{categoryId:int}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _productService.GetProductsByCategoryAsync(categoryId);

            return Ok(products);
        }

        // Tìm kiếm sản phẩm theo tên
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? keyword)
        {
            var products = await _productService.SearchProductsAsync(keyword);

            return Ok(products);
        }

        // Lọc nâng cao: category, keyword, minPrice, maxPrice
        [HttpGet("filter")]
        public async Task<IActionResult> FilterProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice)
        {
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Min price cannot be greater than max price"
                });
            }

            var products = await _productService.FilterProductsAsync(
                categoryId,
                keyword,
                minPrice,
                maxPrice
            );

            return Ok(products);
        }

        // ==========================
        // ADMIN
        // ==========================

        // Thêm sản phẩm mới
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDTO dto)
        {
            if(dto.Price < 0)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Price cannot be negative"
                });
            }

             if (dto.ValidityDays.HasValue && dto.ValidityDays < 0)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Validity days must be greater than or equal to 0"
                });
            }

            var product = await _productService.CreateProductAsync(dto);

            if(product == null)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Invalid category"
                });
            }
            return CreatedAtAction(
                nameof(GetProductById),
                new { id = product.ProductId },
                product
            );
        }

        // Cập nhật sản phẩm
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDTO dto)
        {
            if (dto.Price < 0)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Price must be greater than or equal to 0"
                });
            }

            if (dto.ValidityDays.HasValue && dto.ValidityDays < 0)
            {
                return BadRequest(new
                {
                    IsSuccess = false,
                    Message = "Validity days must be greater than or equal to 0"
                });
            }

            var product = await _productService.UpdateProductAsync(id, dto);

            if (product == null)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Message = "Product not found or invalid category"
                });
            }

            return Ok(product);
        }

        // Xóa sản phẩm (soft delete)
         [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            return Ok(new
            {
                IsSuccess = true,
                Message = "Product deleted successfully"
            });
        }

    }
}