using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;
using PanSystem.Utils;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;

namespace PanSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ISqlSugarClient _db;
        private readonly IConfiguration _configuration;

        public UserController(ISqlSugarClient db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

            // 简单 MD5 验证，实际生产应用加盐哈希
            var md5Password = HashHelper.ComputeMd5(request.Password);
            if (user.Password != md5Password) return BadRequest("账号或密码错误");

            var token = GenerateJwtToken(user);
            return Ok(new { token, username = user.UserName });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            Console.WriteLine($"[Debug] 注册请求: Phone='{request.Phone}', Email='{request.Email}'");

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

            if (!_verificationCodes.ContainsKey(verifyTarget) || _verificationCodes[verifyTarget] != request.VerifyCode)
            {
                return BadRequest("验证码错误或已过期");
            }

            var user = new UserInfo
            {
                UserName = request.Username,
                Password = HashHelper.ComputeMd5(request.Password),
                Email = request.Email,
                Phone = request.Phone,
                CreateTime = DateTime.Now,
                TotalSpace = 1024 * 1024 * 1024 // 默认 1GB
            };

            await _db.Insertable(user).ExecuteCommandAsync();
            _verificationCodes.Remove(verifyTarget);

            Console.WriteLine($"[Success] 用户 {request.Username} 注册成功, 手机: {request.Phone}, 邮箱: {request.Email}");
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
                    u.IsAdmin
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

            await _db.Updateable(user).UpdateColumns(u => new { u.Email, u.Phone, u.Avatar }).ExecuteCommandAsync();
            return Ok("个人资料更新成功");
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            if (user == null) return NotFound("用户不存在");

            if (user.Password != HashHelper.ComputeMd5(request.OldPassword))
            {
                return BadRequest("旧密码错误");
            }

            var pwdError = ValidatePasswordStrength(request.NewPassword);
            if (pwdError != null) return BadRequest(pwdError);

            user.Password = HashHelper.ComputeMd5(request.NewPassword);
            await _db.Updateable(user).UpdateColumns(u => new { u.Password }).ExecuteCommandAsync();
            return Ok("密码修改成功");
        }

        // 验证码内存存储 (仅用于演示找回密码逻辑)
        private static readonly Dictionary<string, string> _verificationCodes = new();

        [AllowAnonymous]
        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] SendCodeRequest request)
        {
            bool userExists = false;
            if (request.Type == "email")
            {
                if (!IsValidEmail(request.Target)) return BadRequest("无效的邮箱格式");
                userExists = await _db.Queryable<UserInfo>().AnyAsync(u => u.Email == request.Target);
            }
            else if (request.Type == "phone")
            {
                if (!IsValidPhone(request.Target)) return BadRequest("无效的手机号格式");
                userExists = await _db.Queryable<UserInfo>().AnyAsync(u => u.Phone == request.Target);
            }

            // 根据场景校验
            if (request.Scenario == "register")
            {
                if (userExists)
                {
                    if (request.Type == "email") return BadRequest("邮箱已注册");
                    if (request.Type == "phone") return BadRequest("手机号已注册");
                    return BadRequest("账号已注册");
                }
            }
            else if (request.Scenario == "reset")
            {
                if (!userExists) return BadRequest("该账号未绑定任何用户");
            }

            // 简单生成一个 6 位验证码
            var code = new Random().Next(100000, 999999).ToString();
            _verificationCodes[request.Target] = code;

            return Ok(new { Message = $"验证码已发送至 {request.Target}", Code = code });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!_verificationCodes.ContainsKey(request.Target) || _verificationCodes[request.Target] != request.Code)
            {
                return BadRequest("验证码错误或已过期");
            }

            var pwdError = ValidatePasswordStrength(request.NewPassword);
            if (pwdError != null) return BadRequest(pwdError);

            var user = await _db.Queryable<UserInfo>()
                .FirstAsync(u => u.Email == request.Target || u.Phone == request.Target);

            if (user == null) return BadRequest("在该邮箱或手机号下未找到用户");

            user.Password = HashHelper.ComputeMd5(request.NewPassword);
            await _db.Updateable(user).UpdateColumns(u => new { u.Password }).ExecuteCommandAsync();

            _verificationCodes.Remove(request.Target); // 使用后移除
            return Ok("密码重置成功");
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
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
