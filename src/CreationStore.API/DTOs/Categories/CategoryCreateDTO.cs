namespace CreationStore.API.DTOs.Categories
{
    public class CategoryCreateDTO
    {
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}