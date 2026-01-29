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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Queryable<UserInfo>()
                .FirstAsync(u => u.UserName == request.Username);

            if (user == null) return BadRequest("用户名或密码错误");

            // 简单 MD5 验证，实际生产应用加盐哈希
            var md5Password = HashHelper.ComputeMd5(request.Password);
            if (user.Password != md5Password) return BadRequest("用户名或密码错误");

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token, Username = user.UserName });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _db.Queryable<UserInfo>().AnyAsync(u => u.UserName == request.Username))
            {
                return BadRequest("用户名已存在");
            }

            var newUser = new UserInfo
            {
                UserName = request.Username,
                Password = HashHelper.ComputeMd5(request.Password),
                Email = request.Email,
                CreateTime = DateTime.Now
            };

            await _db.Insertable(newUser).ExecuteCommandAsync();
            
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
            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.UserName,
                    u.Email,
                    u.TotalSpace,
                    u.UsedSpace,
                    u.CreateTime,
                    u.IsAdmin
                })
                .FirstAsync();

            if (user == null) return NotFound("用户不存在");

            return Ok(user);
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
