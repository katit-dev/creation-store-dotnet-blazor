using CreationStore.API.DTOs.Categories;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: /api/categories
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategoriesAsync();

            return StatusCode(result.StatusCode, result);
        }

        // GET: /api/categories/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);

            return StatusCode(result.StatusCode, result);
        }

        // ==========================
        // ADMIN
        // ==========================
        // POST: /api/categories
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "Category name is required",
                    Content = (object?)null,
                    DateTime = System.DateTime.Now
                });
            }

            var result = await _categoryService.CreateCategoryAsync(dto);

            return StatusCode(result.StatusCode, result);
        }

        // PUT: /api/categories/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "Category name is required",
                    Content = (object?)null,
                    DateTime = System.DateTime.Now
                });
            }

            var result = await _categoryService.UpdateCategoryAsync(id, dto);

            return StatusCode(result.StatusCode, result);
        }

        // DELETE: /api/categories/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);

            return StatusCode(result.StatusCode, result);
        }
    }
}