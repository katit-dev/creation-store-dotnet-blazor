using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CreationStore.API.Data;
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
        
         [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();

            return Ok(products);
        }

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

    }
}