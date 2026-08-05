using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategoriesAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var result = await _categoryService.GetCategoryByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}