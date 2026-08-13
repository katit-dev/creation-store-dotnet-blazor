using System.ComponentModel.DataAnnotations;

namespace CreationStore.Blazor.DTOs.Categories
{
    public class CategoryUpdateDTO
    {
        [Required(ErrorMessage = "Category name is required")]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}