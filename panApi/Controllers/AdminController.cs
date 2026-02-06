using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;
using PanSystem.Utils;
using System.Text.RegularExpressions;

namespace PanSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public AdminController(ISqlSugarClient db)
        {
            _db = db;
        }

        private async Task<bool> CheckIsAdmin()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            return user != null && user.IsAdmin;
        }

        private static string? ValidatePasswordStrength(string password)
        {
            if (password.Length < 8) return "密码长度至少为 8 位";
            if (!Regex.IsMatch(password, @"[a-zA-Z]") || !Regex.IsMatch(password, @"[0-9]"))
                return "密码必须包含字母和数字";
            return null;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUserList()
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var users = await _db.Queryable<UserInfo>()
                .OrderBy(u => u.CreateTime, OrderByType.Desc)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Phone,
                    u.TotalSpace,
                    u.UsedSpace,
                    u.IsAdmin,
                    u.CreateTime,
                    u.UpdateTime,
                    u.LastLoginTime
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var totalUsers = await _db.Queryable<UserInfo>().CountAsync();
            var totalFiles = await _db.Queryable<StorageItem>().Where(f => !f.IsFolder && !f.IsDeleted).CountAsync();
            var totalFolders = await _db.Queryable<StorageItem>().Where(f => f.IsFolder && !f.IsDeleted).CountAsync();
            var totalStorageUsed = await _db.Queryable<UserInfo>().SumAsync(u => u.UsedSpace);

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalFiles = totalFiles,
                TotalFolders = totalFolders,
                TotalStorageUsedBytes = totalStorageUsed,
                TotalStorageUsedGB = Math.Round(totalStorageUsed / 1024.0 / 1024.0 / 1024.0, 2)
            });
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var logs = await _db.Queryable<AuditLog>()
                .OrderBy(l => l.CreateTime, OrderByType.Desc)
                .Take(500) // 仅返回最近500条
                .ToListAsync();

            return Ok(logs);
        }

        [HttpPut("user-quota")]
        public async Task<IActionResult> UpdateUserQuota(int userId, long newTotalSpaceBytes)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.TotalSpace == newTotalSpaceBytes)
                .SetColumns(u => u.UpdateTime == DateTime.Now)
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            return Ok("用户配额更新成功");
        }

        [HttpPut("user-password")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] AdminUpdateUserPasswordRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (request.UserId <= 0) return BadRequest("无效的用户ID");

            var pwdError = ValidatePasswordStrength(request.NewPassword);
            if (pwdError != null) return BadRequest(pwdError);

            var user = await _db.Queryable<UserInfo>().InSingleAsync(request.UserId);
            if (user == null) return NotFound("用户不存在");

            user.Password = HashHelper.ComputeMd5(request.NewPassword);
            user.UpdateTime = DateTime.Now;
            await _db.Updateable(user).UpdateColumns(u => new { u.Password, u.UpdateTime }).ExecuteCommandAsync();

            return Ok("用户密码修改成功");
        }
    }
}
