namespace PanSystem.DTOs
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string? Email { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("phone")]
        public string? Phone { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("verifyCode")]
        public string VerifyCode { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SendCodeRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty; // Email or Phone
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "email" or "phone"
        [System.Text.Json.Serialization.JsonPropertyName("scenario")]
        public string Scenario { get; set; } = string.Empty; // "register" or "reset"
    }

    public class ResetPasswordRequest
    {
        public string Target { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
