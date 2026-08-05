using CreationStore.API.DTOs.Categories;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/categories")]
    [ApiController]
    public class AdminCategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public AdminCategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(
            [FromBody] CategoryCreateDTO dto
        )
        {
            var result = await _categoryService.CreateCategoryAsync(dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromBody] CategoryUpdateDTO dto
        )
        {
            var result = await _categoryService.UpdateCategoryAsync(id, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            return StatusCode(result.StatusCode, result);
        }
    }
}