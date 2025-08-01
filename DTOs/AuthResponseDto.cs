using System.Text.Json.Serialization;

namespace Todo_List_API.DTOs
{
    public class AuthResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; } = false;
        public string Email { get; set; } = string.Empty;
        [JsonIgnore] public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresOn { get; set; }
    }
}
