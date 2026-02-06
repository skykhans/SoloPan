namespace PanSystem.DTOs
{
    /// <summary>
    /// 登录请求。
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 账号（用户名/邮箱/手机号）。
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码（明文，服务端会做哈希校验）。
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 设备标识（前端本地持久化）。
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// 设备名称（前端传入，可选）。
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 新设备登录邮箱验证码（可选）。
        /// </summary>
        public string? VerifyCode { get; set; }
    }

    /// <summary>
    /// 注册请求。
    /// </summary>
    public class RegisterRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("username")]
        /// <summary>
        /// 用户名。
        /// </summary>
        public string Username { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        /// <summary>
        /// 密码。
        /// </summary>
        public string Password { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("email")]
        /// <summary>
        /// 邮箱。
        /// </summary>
        public string? Email { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("phone")]
        /// <summary>
        /// 手机号。
        /// </summary>
        public string? Phone { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("verifyCode")]
        /// <summary>
        /// 验证码。
        /// </summary>
        public string VerifyCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 个人资料更新请求。
    /// </summary>
    public class UpdateProfileRequest
    {
        /// <summary>
        /// 邮箱。
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 手机号。
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 头像URL。
        /// </summary>
        public string? Avatar { get; set; }
    }

    /// <summary>
    /// 修改密码请求。
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// 旧密码。
        /// </summary>
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码。
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 发送验证码请求。
    /// </summary>
    public class SendCodeRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("target")]
        /// <summary>
        /// 目标地址（邮箱或手机号）。
        /// </summary>
        public string Target { get; set; } = string.Empty; // Email or Phone
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        /// <summary>
        /// 验证码类型（email/phone）。
        /// </summary>
        public string Type { get; set; } = string.Empty; // "email" or "phone"
        [System.Text.Json.Serialization.JsonPropertyName("scenario")]
        /// <summary>
        /// 业务场景（register/reset）。
        /// </summary>
        public string Scenario { get; set; } = string.Empty; // "register" or "reset"
    }

    /// <summary>
    /// 重置密码请求。
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// 目标地址（邮箱或手机号）。
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// 验证码。
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 新密码。
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 管理员修改用户密码请求。
    /// </summary>
    public class AdminUpdateUserPasswordRequest
    {
        /// <summary>
        /// 目标用户ID。
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 新密码。
        /// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
