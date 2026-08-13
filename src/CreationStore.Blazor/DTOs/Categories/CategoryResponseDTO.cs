namespace CreationStore.Blazor.DTOs.Categories
{
    public class CategoryResponseDTO
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}