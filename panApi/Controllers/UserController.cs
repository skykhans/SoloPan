using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;
using PanSystem.Services;
using PanSystem.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using System.Linq;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace PanSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ISqlSugarClient _db;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public UserController(ISqlSugarClient db, IConfiguration configuration, IAuditService auditService)
        {
            _db = db;
            _configuration = configuration;
            _auditService = auditService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpireAt)> _deviceVerifyCodes = new();

        // --- 校验辅助方法 ---
        private bool IsValidEmail(string email) => Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        private bool IsValidPhone(string phone) => Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
        private bool IsValidUsername(string username) => Regex.IsMatch(username, @"^[a-zA-Z][a-zA-Z0-9]*$");
        private string? ValidatePasswordStrength(string password)
        {
            if (password.Length < 8) return "密码长度至少为 8 位";
            if (!Regex.IsMatch(password, @"[a-zA-Z]") || !Regex.IsMatch(password, @"[0-9]"))
                return "密码必须包含字母和数字";
            return null;
        }

        [AllowAnonymous]
        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            var name = username?.Trim();
            if (string.IsNullOrEmpty(name)) return BadRequest("用户名不能为空");
            var exists = await _db.Queryable<UserInfo>().AnyAsync(u => u.UserName == name);
            return Ok(new { exists });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var account = request.Username?.Trim();
            if (string.IsNullOrEmpty(account)) return BadRequest("请输入用户名、手机号或邮箱");

            var user = await _db.Queryable<UserInfo>()
                .FirstAsync(u => u.UserName == account || u.Email == account || u.Phone == account);

            if (user == null) return BadRequest("账号或密码错误");

            if (!user.IsEnabled) return BadRequest("账号已被禁用，请联系管理员");

            if (!PasswordHelper.VerifyPassword(request.Password, user.Password, out var needsRehash))
            {
                return BadRequest("账号或密码错误");
            }

            var deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? "unknown-device" : request.DeviceId.Trim();
            var deviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? null : request.DeviceName.Trim();
            var userAgent = Request.Headers.UserAgent.ToString();
            var deviceType = ResolveDeviceType(userAgent);

            var clientIpText = GetClientIpText();
            if (IpRuleParser.TryParseClientIp(clientIpText, out var clientIpNumber, out var normalizedIp))
            {
                var isBlockedIp = await _db.Queryable<LoginIpRule>()
                    .AnyAsync(r => r.IsEnabled && r.StartIp <= clientIpNumber && r.EndIp >= clientIpNumber);
                if (isBlockedIp) return BadRequest($"当前IP({normalizedIp})不允许登录");

                if (user.IsAdmin)
                {
                    var enabledAdminRuleCount = await _db.Queryable<AdminLoginIpRule>()
                        .CountAsync(r => r.IsEnabled);

                    if (enabledAdminRuleCount > 0)
                    {
                        var inAdminWhitelist = await _db.Queryable<AdminLoginIpRule>()
                            .AnyAsync(r => r.IsEnabled && r.StartIp <= clientIpNumber && r.EndIp >= clientIpNumber);
                        if (!inAdminWhitelist) return BadRequest($"管理员账号不允许从当前IP({normalizedIp})登录");
                    }
                }
            }
            else
            {
                normalizedIp = IpRuleParser.NormalizeIpForDisplay(clientIpText);

                if (user.IsAdmin)
                {
                    var enabledAdminRuleCount = await _db.Queryable<AdminLoginIpRule>()
                        .CountAsync(r => r.IsEnabled);
                    if (enabledAdminRuleCount > 0) return BadRequest("管理员账号当前IP不可识别，禁止登录");
                }
            }

            // 新设备需要邮箱验证码校验
            var knownDevice = await _db.Queryable<UserLoginDevice>()
                .FirstAsync(d => d.UserId == user.Id && d.DeviceId == deviceId && d.IsTrusted);
            var needsDeviceEmailVerify = knownDevice == null;
            if (needsDeviceEmailVerify)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return BadRequest("当前账号未绑定邮箱，无法进行新设备校验，请联系管理员");
                }

                var verifyKey = $"{user.Id}:{deviceId}";
                var codeInput = (request.VerifyCode ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(codeInput))
                {
                    var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                    _deviceVerifyCodes[verifyKey] = (code, DateTime.Now.AddMinutes(5));
                    Console.WriteLine($"设备验证码 {MaskEmail(user.Email)}: {code}");
                    return Ok(new
                    {
                        requireEmailVerify = true,
                        message = $"新设备登录，请输入发送到邮箱 {MaskEmail(user.Email)} 的验证码",
                        expireSeconds = 300
                    });
                }

                if (!_deviceVerifyCodes.TryGetValue(verifyKey, out var verifyData) || verifyData.ExpireAt < DateTime.Now || verifyData.Code != codeInput)
                {
                    return BadRequest("验证码错误或已过期");
                }
                _deviceVerifyCodes.TryRemove(verifyKey, out _);
            }

            user.LastLoginTime = DateTime.Now;
            user.LastLoginIp = normalizedIp;
            if (needsRehash)
            {
                user.Password = PasswordHelper.HashPassword(request.Password);
                user.UpdateTime = DateTime.Now;
                await _db.Updateable(user)
                    .UpdateColumns(u => new { u.Password, u.LastLoginTime, u.LastLoginIp, u.UpdateTime })
                    .ExecuteCommandAsync();
            }
            else
            {
                await _db.Updateable(user)
                    .UpdateColumns(u => new { u.LastLoginTime, u.LastLoginIp })
                    .ExecuteCommandAsync();
            }

            if (knownDevice == null)
            {
                await _db.Insertable(new UserLoginDevice
                {
                    UserId = user.Id,
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    DeviceType = deviceType,
                    LastLoginIp = normalizedIp,
                    LastUserAgent = userAgent,
                    LoginCount = 1,
                    FirstLoginTime = DateTime.Now,
                    LastLoginTime = DateTime.Now,
                    IsTrusted = true
                }).ExecuteCommandAsync();
            }
            else
            {
                knownDevice.DeviceName = deviceName ?? knownDevice.DeviceName;
                knownDevice.DeviceType = deviceType;
                knownDevice.LastLoginIp = normalizedIp;
                knownDevice.LastUserAgent = userAgent;
                knownDevice.LoginCount += 1;
                knownDevice.LastLoginTime = DateTime.Now;
                await _db.Updateable(knownDevice)
                    .UpdateColumns(d => new { d.DeviceName, d.DeviceType, d.LastLoginIp, d.LastUserAgent, d.LoginCount, d.LastLoginTime })
                    .ExecuteCommandAsync();
            }

            await _auditService.LogAsync(user.Id, user.UserName, "用户登录", "登录成功", normalizedIp);

            var token = GenerateJwtToken(user);
            return Ok(new { token, username = user.UserName });
        }

        private string? GetClientIpText()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var firstIp = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstIp)) return firstIp;
            }

            var realIp = Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(realIp)) return realIp.Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "未知邮箱";
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name = parts[0];
            if (name.Length <= 2) return $"{name[0]}***@{parts[1]}";
            return $"{name[..2]}***@{parts[1]}";
        }

        private static string ResolveDeviceType(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent)) return "Unknown";
            var ua = userAgent.ToLowerInvariant();
            if (ua.Contains("android") || ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("mobile")) return "Mobile";
            return "Desktop";
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("请选择头像文件");
            if (file.Length > 2 * 1024 * 1024) return BadRequest("头像文件不能超过 2MB");
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("仅支持图片文件");

            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            if (user == null) return NotFound("用户不存在");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (string.IsNullOrEmpty(ext) || !allowedExts.Contains(ext))
                return BadRequest("头像格式仅支持 jpg/jpeg/png/webp/gif");

            var datePath = DateTime.Now.ToString("yyyy-MM-dd");
            var relativePath = Path.Combine(datePath, $"{Guid.NewGuid():N}{ext}");
            var avatarRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
            var fullPath = Path.Combine(avatarRoot, relativePath);
            var fullDir = Path.GetDirectoryName(fullPath)!;
            if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            var normalizedRelativePath = relativePath.Replace('\\', '/');
            var avatarUrl = $"{Request.Scheme}://{Request.Host}/api/user/avatar/{normalizedRelativePath}";

            user.Avatar = avatarUrl;
            user.UpdateTime = DateTime.Now;
            await _db.Updateable(user).UpdateColumns(u => new { u.Avatar, u.UpdateTime }).ExecuteCommandAsync();

            return Ok(new { avatar = avatarUrl });
        }

        [AllowAnonymous]
        [HttpGet("avatar/{*filePath}")]
        public IActionResult GetAvatar(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return NotFound();

            var normalizedPath = filePath.Replace('\\', '/').Trim('/');
            if (normalizedPath.Contains("..")) return BadRequest("非法路径");

            var avatarRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "avatars");
            var fullPath = Path.Combine(avatarRoot, normalizedPath);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fullPath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(fullPath, contentType);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!IsValidUsername(request.Username))
                return BadRequest("用户名需以字母开头，仅包含字母和数字");

            if (await _db.Queryable<UserInfo>().AnyAsync(u => u.UserName == request.Username))
                return BadRequest("用户名已存在");

            if (!string.IsNullOrEmpty(request.Email))
            {
                if (!IsValidEmail(request.Email)) return BadRequest("无效的邮箱格式");
                if (await _db.Queryable<UserInfo>().AnyAsync(u => u.Email == request.Email))
                    return BadRequest("邮箱已注册");
            }

            if (!string.IsNullOrEmpty(request.Phone))
            {
                if (!IsValidPhone(request.Phone)) return BadRequest("无效的手机号格式");
                if (await _db.Queryable<UserInfo>().AnyAsync(u => u.Phone == request.Phone))
                    return BadRequest("手机号已注册");
            }

            var pwdError = ValidatePasswordStrength(request.Password);
            if (pwdError != null) return BadRequest(pwdError);

            // 注册验证码校验
            // 优先使用手机号验证，如果没有手机号则使用邮箱
            var verifyTarget = !string.IsNullOrEmpty(request.Phone) ? request.Phone : request.Email;
            if (string.IsNullOrEmpty(verifyTarget)) return BadRequest("手机号或邮箱不能为空");

            if (!TryConsumeVerificationCode(verifyTarget, request.VerifyCode))
            {
                return BadRequest("验证码错误或已过期");
            }

            var user = new UserInfo
            {
                UserName = request.Username,
                Password = PasswordHelper.HashPassword(request.Password),
                Email = request.Email,
                Phone = request.Phone,
                MaxUploadFileSize = 100L * 1024 * 1024,
                IsEnabled = true,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                TotalSpace = 1024 * 1024 * 1024 // 默认 1GB
            };

            await _db.Insertable(user).ExecuteCommandAsync();

            return Ok("注册成功");
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.UserName,
                    u.Email,
                    u.Phone,
                    u.Avatar,
                    u.TotalSpace,
                    u.UsedSpace,
                    u.CreateTime,
                    u.IsAdmin,
                    u.LastLoginTime,
                    u.LastLoginIp
                })
                .FirstAsync();

            if (user == null) return NotFound("用户不存在");

            return Ok(user);
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetInfo()
        {
            return await GetProfile();
        }

        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            if (user == null) return NotFound("用户不存在");

            if (!string.IsNullOrEmpty(request.Email))
            {
                if (!IsValidEmail(request.Email)) return BadRequest("无效的邮箱格式");
                user.Email = request.Email;
            }
            if (!string.IsNullOrEmpty(request.Phone))
            {
                if (!IsValidPhone(request.Phone)) return BadRequest("无效的手机号格式");
                user.Phone = request.Phone;
            }
            if (!string.IsNullOrEmpty(request.Avatar)) user.Avatar = request.Avatar;
            user.UpdateTime = DateTime.Now;

            await _db.Updateable(user).UpdateColumns(u => new { u.Email, u.Phone, u.Avatar, u.UpdateTime }).ExecuteCommandAsync();
            return Ok("个人资料更新成功");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            if (user == null) return NotFound("用户不存在");

            if (!PasswordHelper.VerifyPassword(request.OldPassword, user.Password, out _))
            {
                return BadRequest("旧密码错误");
            }

            var pwdError = ValidatePasswordStrength(request.NewPassword);
            if (pwdError != null) return BadRequest(pwdError);

            user.Password = PasswordHelper.HashPassword(request.NewPassword);
            user.UpdateTime = DateTime.Now;
            await _db.Updateable(user).UpdateColumns(u => new { u.Password, u.UpdateTime }).ExecuteCommandAsync();
            return Ok("密码修改成功");
        }

        private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpireAt)> _verificationCodes = new();

        [AllowAnonymous]
        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] SendCodeRequest request)
        {
            var target = request.Target?.Trim() ?? "";
            var type = request.Type?.Trim().ToLowerInvariant() ?? "";
            var scenario = request.Scenario?.Trim().ToLowerInvariant() ?? "";
            if (scenario != "register" && scenario != "reset") return BadRequest("验证码场景错误");

            bool userExists = false;
            if (type == "email")
            {
                if (!IsValidEmail(target)) return BadRequest("无效的邮箱格式");
                userExists = await _db.Queryable<UserInfo>().AnyAsync(u => u.Email == target);
            }
            else if (type == "phone")
            {
                if (!IsValidPhone(target)) return BadRequest("无效的手机号格式");
                userExists = await _db.Queryable<UserInfo>().AnyAsync(u => u.Phone == target);
            }
            else return BadRequest("验证码类型错误");

            // 根据场景校验
            if (scenario == "register")
            {
                if (userExists)
                {
                    if (type == "email") return BadRequest("邮箱已注册");
                    if (type == "phone") return BadRequest("手机号已注册");
                    return BadRequest("账号已注册");
                }
            }
            else if (scenario == "reset")
            {
                if (!userExists) return BadRequest("该账号未绑定任何用户");
            }

            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            _verificationCodes[target] = (code, DateTime.UtcNow.AddMinutes(5));
            Console.WriteLine($"验证码 {target}: {code}");

            return Ok(new { Message = $"验证码已发送至 {target}", ExpireSeconds = 300 });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var target = request.Target?.Trim() ?? "";
            var pwdError = ValidatePasswordStrength(request.NewPassword);
            if (pwdError != null) return BadRequest(pwdError);

            var user = await _db.Queryable<UserInfo>()
                .FirstAsync(u => u.Email == target || u.Phone == target);

            if (user == null) return BadRequest("在该邮箱或手机号下未找到用户");
            if (!TryConsumeVerificationCode(target, request.Code))
            {
                return BadRequest("验证码错误或已过期");
            }

            user.Password = PasswordHelper.HashPassword(request.NewPassword);
            user.UpdateTime = DateTime.Now;
            await _db.Updateable(user).UpdateColumns(u => new { u.Password, u.UpdateTime }).ExecuteCommandAsync();

            return Ok("密码重置成功");
        }

        private static bool TryConsumeVerificationCode(string target, string code)
        {
            if (!_verificationCodes.TryGetValue(target, out var data)) return false;
            if (data.ExpireAt < DateTime.UtcNow)
            {
                _verificationCodes.TryRemove(target, out _);
                return false;
            }
            return data.Code == code && _verificationCodes.TryRemove(target, out _);
        }

        private string GenerateJwtToken(UserInfo user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                }),
                Issuer = _configuration["Jwt:Issuer"] ?? "PanSystem",
                Audience = _configuration["Jwt:Audience"] ?? "PanSystemUser",
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
