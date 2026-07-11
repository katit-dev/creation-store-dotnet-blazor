namespace CreationStore.API.DTOs.Auth
{
    public class RegisterResponseDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;
    }
}