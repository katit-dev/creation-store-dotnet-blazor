namespace CreationStore.API.DTOs.Auth
{
    public class LoginDTO
    {
        public string LoginIdentifier { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}