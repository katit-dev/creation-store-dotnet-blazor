namespace CreationStore.API.DTOs.Categories
{
    public class CategoryUpdateDTO
    {
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}