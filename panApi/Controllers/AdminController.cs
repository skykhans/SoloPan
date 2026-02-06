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
        public class UserListQuery
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public int? UserId { get; set; }
            public string? UserName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public bool? IsAdmin { get; set; }
            public string? SortBy { get; set; }
            public string? Order { get; set; } // asc / desc
        }

        public class UpdateUserUploadLimitRequest
        {
            public int UserId { get; set; }
            public long MaxUploadFileSizeBytes { get; set; }
        }

        public class AuditLogQuery
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public string? UserName { get; set; }
            public string? Action { get; set; }
            public string? IpAddress { get; set; }
            public string? Keyword { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? EndTime { get; set; }
            public string? SortBy { get; set; }
            public string? Order { get; set; } // asc / desc
        }

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
        public async Task<IActionResult> GetUserList([FromQuery] UserListQuery req)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var pageIndex = req.Page < 1 ? 1 : req.Page;
            var pageLength = req.PageSize < 1 ? 20 : (req.PageSize > 200 ? 200 : req.PageSize);
            var userNameFilter = string.IsNullOrWhiteSpace(req.UserName) ? null : req.UserName.Trim();
            var emailFilter = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            var phoneFilter = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();

            var query = _db.Queryable<UserInfo>()
                .WhereIF(req.UserId.HasValue, u => u.Id == req.UserId!.Value)
                .WhereIF(userNameFilter != null, u => u.UserName.Contains(userNameFilter!))
                .WhereIF(emailFilter != null, u => u.Email != null && u.Email.Contains(emailFilter!))
                .WhereIF(phoneFilter != null, u => u.Phone != null && u.Phone.Contains(phoneFilter!))
                .WhereIF(req.IsAdmin.HasValue, u => u.IsAdmin == req.IsAdmin!.Value);
            var total = await query.CountAsync();

            var userOrderType = string.Equals(req.Order, "asc", StringComparison.OrdinalIgnoreCase)
                ? OrderByType.Asc
                : OrderByType.Desc;
            query = (req.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "id" => query.OrderBy(u => u.Id, userOrderType),
                "username" => query.OrderBy(u => u.UserName, userOrderType),
                "email" => query.OrderBy(u => u.Email, userOrderType),
                "phone" => query.OrderBy(u => u.Phone, userOrderType),
                "usedspace" => query.OrderBy(u => u.UsedSpace, userOrderType),
                "createtime" => query.OrderBy(u => u.CreateTime, userOrderType),
                "updatetime" => query.OrderBy(u => u.UpdateTime, userOrderType),
                "lastlogintime" => query.OrderBy(u => u.LastLoginTime, userOrderType),
                "isadmin" => query.OrderBy(u => u.IsAdmin, userOrderType),
                "maxuploadfilesize" => query.OrderBy(u => u.MaxUploadFileSize, userOrderType),
                _ => query.OrderBy(u => u.CreateTime, OrderByType.Desc)
            };

            var users = await query
                .Skip((pageIndex - 1) * pageLength)
                .Take(pageLength)
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
                    u.LastLoginTime,
                    u.MaxUploadFileSize
                })
                .ToListAsync();

            return Ok(new
            {
                Items = users,
                Total = total,
                Page = pageIndex,
                PageSize = pageLength
            });
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
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQuery req)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var pageIndex = req.Page < 1 ? 1 : req.Page;
            var pageLength = req.PageSize < 1 ? 20 : (req.PageSize > 200 ? 200 : req.PageSize);
            var userNameFilter = string.IsNullOrWhiteSpace(req.UserName) ? null : req.UserName.Trim();
            var actionFilter = string.IsNullOrWhiteSpace(req.Action) ? null : req.Action.Trim();
            var ipFilter = string.IsNullOrWhiteSpace(req.IpAddress) ? null : req.IpAddress.Trim();
            var keywordFilter = string.IsNullOrWhiteSpace(req.Keyword) ? null : req.Keyword.Trim();

            var query = _db.Queryable<AuditLog>()
                .WhereIF(userNameFilter != null, l => l.UserName.Contains(userNameFilter!))
                .WhereIF(actionFilter != null, l => l.Action.Contains(actionFilter!))
                .WhereIF(ipFilter != null, l => l.IpAddress.Contains(ipFilter!))
                .WhereIF(keywordFilter != null, l => l.Detail.Contains(keywordFilter!))
                .WhereIF(req.StartTime.HasValue, l => l.CreateTime >= req.StartTime!.Value)
                .WhereIF(req.EndTime.HasValue, l => l.CreateTime <= req.EndTime!.Value);
            var total = await query.CountAsync();

            var auditOrderType = string.Equals(req.Order, "asc", StringComparison.OrdinalIgnoreCase)
                ? OrderByType.Asc
                : OrderByType.Desc;
            query = (req.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "createtime" => query.OrderBy(l => l.CreateTime, auditOrderType),
                "username" => query.OrderBy(l => l.UserName, auditOrderType),
                "action" => query.OrderBy(l => l.Action, auditOrderType),
                "detail" => query.OrderBy(l => l.Detail, auditOrderType),
                "ipaddress" => query.OrderBy(l => l.IpAddress, auditOrderType),
                _ => query.OrderBy(l => l.CreateTime, OrderByType.Desc)
            };

            var logs = await query
                .Skip((pageIndex - 1) * pageLength)
                .Take(pageLength)
                .ToListAsync();

            return Ok(new
            {
                Items = logs,
                Total = total,
                Page = pageIndex,
                PageSize = pageLength
            });
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

        [HttpPut("user-upload-limit")]
        public async Task<IActionResult> UpdateUserUploadLimit([FromBody] UpdateUserUploadLimitRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (request.UserId <= 0) return BadRequest("无效的用户ID");
            if (request.MaxUploadFileSizeBytes < 1L * 1024 * 1024) return BadRequest("单文件上传上限不能低于 1MB");
            if (request.MaxUploadFileSizeBytes > 10L * 1024 * 1024 * 1024) return BadRequest("单文件上传上限不能超过 10GB");

            var user = await _db.Queryable<UserInfo>().InSingleAsync(request.UserId);
            if (user == null) return NotFound("用户不存在");

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.MaxUploadFileSize == request.MaxUploadFileSizeBytes)
                .SetColumns(u => u.UpdateTime == DateTime.Now)
                .Where(u => u.Id == request.UserId)
                .ExecuteCommandAsync();

            return Ok("单文件上传上限修改成功");
        }
    }
}
