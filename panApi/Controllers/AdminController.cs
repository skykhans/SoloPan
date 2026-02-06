using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;
using PanSystem.Utils;
using System.Text.RegularExpressions;
using PanSystem.Services;

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
            public bool? IsEnabled { get; set; }
            public string? SortBy { get; set; }
            public string? Order { get; set; } // asc / desc
        }

        public class UpdateUserUploadLimitRequest
        {
            public int UserId { get; set; }
            public long MaxUploadFileSizeBytes { get; set; }
        }

        public class BatchUserIdsRequest
        {
            public List<int> UserIds { get; set; } = new();
        }

        public class BatchUpdateUserQuotaRequest
        {
            public List<int> UserIds { get; set; } = new();
            public long NewTotalSpaceBytes { get; set; }
        }

        public class BatchUpdateUserUploadLimitRequest
        {
            public List<int> UserIds { get; set; } = new();
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

        public class CreateLoginIpRuleRequest
        {
            public string RuleText { get; set; } = string.Empty;
            public string? Remark { get; set; }
            public bool IsEnabled { get; set; } = true;
        }

        public class UpdateLoginIpRuleStatusRequest
        {
            public bool IsEnabled { get; set; }
        }

        public class AdminFileQuery
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public int? UserId { get; set; }
            public string? UserName { get; set; }
            public string? Name { get; set; }
            public bool? IsFolder { get; set; }
            public bool? IsDeleted { get; set; }
            public bool? IsShared { get; set; }
            public string? SortBy { get; set; }
            public string? Order { get; set; }
        }

        public class AdminCreateFolderRequest
        {
            public int UserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? ParentId { get; set; }
        }

        public class AdminRenameFileRequest
        {
            public int Id { get; set; }
            public string NewName { get; set; } = string.Empty;
        }

        public class AdminBatchFileIdsRequest
        {
            public List<int> Ids { get; set; } = new();
        }

        private class AdminFileListItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public int? ParentId { get; set; }
            public bool IsFolder { get; set; }
            public long FileSize { get; set; }
            public bool IsDeleted { get; set; }
            public bool IsShared { get; set; }
            public DateTime? ShareTime { get; set; }
            public string? ShareToken { get; set; }
            public string? ShareCode { get; set; }
            public DateTime? ShareExpireTime { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime? UpdateTime { get; set; }
            public DateTime? DeleteTime { get; set; }
        }

        private readonly ISqlSugarClient _db;
        private readonly IStorageService _storageService;

        public AdminController(ISqlSugarClient db, IStorageService storageService)
        {
            _db = db;
            _storageService = storageService;
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
                .WhereIF(req.IsAdmin.HasValue, u => u.IsAdmin == req.IsAdmin!.Value)
                .WhereIF(req.IsEnabled.HasValue, u => u.IsEnabled == req.IsEnabled!.Value);
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
                "lastloginip" => query.OrderBy(u => u.LastLoginIp, userOrderType),
                "isadmin" => query.OrderBy(u => u.IsAdmin, userOrderType),
                "isenabled" => query.OrderBy(u => u.IsEnabled, userOrderType),
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
                    u.IsEnabled,
                    u.CreateTime,
                    u.UpdateTime,
                    u.LastLoginTime,
                    u.LastLoginIp,
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

            user.Password = PasswordHelper.HashPassword(request.NewPassword);
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

        [HttpPut("users/enable")]
        public async Task<IActionResult> BatchEnableUsers([FromBody] BatchUserIdsRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.UserIds?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择用户");

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.IsEnabled == true)
                .SetColumns(u => u.UpdateTime == DateTime.Now)
                .Where(u => ids.Contains(u.Id))
                .ExecuteCommandAsync();

            return Ok("批量启用成功");
        }

        [HttpPut("users/disable")]
        public async Task<IActionResult> BatchDisableUsers([FromBody] BatchUserIdsRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.UserIds?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择用户");

            var hasAdmin = await _db.Queryable<UserInfo>()
                .AnyAsync(u => ids.Contains(u.Id) && u.IsAdmin);
            if (hasAdmin) return BadRequest("系统管理员不允许被禁用");

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.IsEnabled == false)
                .SetColumns(u => u.UpdateTime == DateTime.Now)
                .Where(u => ids.Contains(u.Id))
                .ExecuteCommandAsync();

            return Ok("批量禁用成功");
        }

        [HttpPut("users/quota")]
        public async Task<IActionResult> BatchUpdateUserQuota([FromBody] BatchUpdateUserQuotaRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.UserIds?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择用户");
            if (request.NewTotalSpaceBytes < 1L * 1024 * 1024 * 1024) return BadRequest("用户配额不能低于 1GB");
            if (request.NewTotalSpaceBytes > 10240L * 1024 * 1024 * 1024) return BadRequest("用户配额不能超过 10TB");

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.TotalSpace == request.NewTotalSpaceBytes)
                .SetColumns(u => u.UpdateTime == DateTime.Now)
                .Where(u => ids.Contains(u.Id))
                .ExecuteCommandAsync();

            return Ok("批量修改配额成功");
        }

        [HttpPut("users/upload-limit")]
        public async Task<IActionResult> BatchUpdateUserUploadLimit([FromBody] BatchUpdateUserUploadLimitRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.UserIds?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择用户");
            if (request.MaxUploadFileSizeBytes < 1L * 1024 * 1024) return BadRequest("单文件上传上限不能低于 1MB");
            if (request.MaxUploadFileSizeBytes > 10L * 1024 * 1024 * 1024) return BadRequest("单文件上传上限不能超过 10GB");

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.MaxUploadFileSize == request.MaxUploadFileSizeBytes)
                .SetColumns(u => u.UpdateTime == DateTime.Now)
                .Where(u => ids.Contains(u.Id))
                .ExecuteCommandAsync();

            return Ok("批量修改上传上限成功");
        }

        [HttpGet("login-ip-rules")]
        public async Task<IActionResult> GetLoginIpRules()
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var rules = await _db.Queryable<LoginIpRule>()
                .OrderBy(r => r.Id, OrderByType.Desc)
                .Select(r => new
                {
                    r.Id,
                    r.RuleText,
                    r.IsEnabled,
                    r.Remark,
                    r.CreateTime,
                    r.UpdateTime
                })
                .ToListAsync();

            return Ok(rules);
        }

        [HttpPost("login-ip-rules")]
        public async Task<IActionResult> CreateLoginIpRule([FromBody] CreateLoginIpRuleRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (request == null || string.IsNullOrWhiteSpace(request.RuleText)) return BadRequest("请输入IP规则");

            if (!IpRuleParser.TryParseRule(request.RuleText, out var startIp, out var endIp, out var normalizedRule, out var error))
            {
                return BadRequest(error);
            }

            var exists = await _db.Queryable<LoginIpRule>()
                .AnyAsync(r => r.StartIp == startIp && r.EndIp == endIp);
            if (exists) return BadRequest("该IP限制规则已存在");

            await _db.Insertable(new LoginIpRule
            {
                RuleText = normalizedRule,
                StartIp = startIp,
                EndIp = endIp,
                IsEnabled = request.IsEnabled,
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            return Ok("登录IP限制规则新增成功");
        }

        [HttpPut("login-ip-rules/{id}/status")]
        public async Task<IActionResult> UpdateLoginIpRuleStatus(int id, [FromBody] UpdateLoginIpRuleStatusRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var affected = await _db.Updateable<LoginIpRule>()
                .SetColumns(r => r.IsEnabled == request.IsEnabled)
                .SetColumns(r => r.UpdateTime == DateTime.Now)
                .Where(r => r.Id == id)
                .ExecuteCommandAsync();

            if (affected <= 0) return NotFound("规则不存在");
            return Ok("规则状态更新成功");
        }

        [HttpDelete("login-ip-rules/{id}")]
        public async Task<IActionResult> DeleteLoginIpRule(int id)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var affected = await _db.Deleteable<LoginIpRule>().Where(r => r.Id == id).ExecuteCommandAsync();
            if (affected <= 0) return NotFound("规则不存在");

            return Ok("规则删除成功");
        }

        [HttpGet("admin-login-ip-rules")]
        public async Task<IActionResult> GetAdminLoginIpRules()
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var rules = await _db.Queryable<AdminLoginIpRule>()
                .OrderBy(r => r.Id, OrderByType.Desc)
                .Select(r => new
                {
                    r.Id,
                    r.RuleText,
                    r.IsEnabled,
                    r.Remark,
                    r.CreateTime,
                    r.UpdateTime
                })
                .ToListAsync();

            return Ok(rules);
        }

        [HttpPost("admin-login-ip-rules")]
        public async Task<IActionResult> CreateAdminLoginIpRule([FromBody] CreateLoginIpRuleRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (request == null || string.IsNullOrWhiteSpace(request.RuleText)) return BadRequest("请输入IP规则");

            if (!IpRuleParser.TryParseRule(request.RuleText, out var startIp, out var endIp, out var normalizedRule, out var error))
            {
                return BadRequest(error);
            }

            var exists = await _db.Queryable<AdminLoginIpRule>()
                .AnyAsync(r => r.StartIp == startIp && r.EndIp == endIp);
            if (exists) return BadRequest("该管理员IP规则已存在");

            await _db.Insertable(new AdminLoginIpRule
            {
                RuleText = normalizedRule,
                StartIp = startIp,
                EndIp = endIp,
                IsEnabled = request.IsEnabled,
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync();

            return Ok("管理员登录IP规则新增成功");
        }

        [HttpPut("admin-login-ip-rules/{id}/status")]
        public async Task<IActionResult> UpdateAdminLoginIpRuleStatus(int id, [FromBody] UpdateLoginIpRuleStatusRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var affected = await _db.Updateable<AdminLoginIpRule>()
                .SetColumns(r => r.IsEnabled == request.IsEnabled)
                .SetColumns(r => r.UpdateTime == DateTime.Now)
                .Where(r => r.Id == id)
                .ExecuteCommandAsync();

            if (affected <= 0) return NotFound("规则不存在");
            return Ok("规则状态更新成功");
        }

        [HttpDelete("admin-login-ip-rules/{id}")]
        public async Task<IActionResult> DeleteAdminLoginIpRule(int id)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var affected = await _db.Deleteable<AdminLoginIpRule>().Where(r => r.Id == id).ExecuteCommandAsync();
            if (affected <= 0) return NotFound("规则不存在");

            return Ok("规则删除成功");
        }

        [HttpGet("files")]
        public async Task<IActionResult> GetAllFiles([FromQuery] AdminFileQuery req)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");

            var pageIndex = req.Page < 1 ? 1 : req.Page;
            var pageLength = req.PageSize < 1 ? 20 : (req.PageSize > 200 ? 200 : req.PageSize);
            var userNameFilter = string.IsNullOrWhiteSpace(req.UserName) ? null : req.UserName.Trim();
            var nameFilter = string.IsNullOrWhiteSpace(req.Name) ? null : req.Name.Trim();

            var query = _db.Queryable<StorageItem>()
                .WhereIF(req.UserId.HasValue, f => f.UserId == req.UserId!.Value)
                .WhereIF(nameFilter != null, f => f.Name.Contains(nameFilter!))
                .WhereIF(req.IsFolder.HasValue, f => f.IsFolder == req.IsFolder!.Value)
                .WhereIF(req.IsDeleted.HasValue, f => f.IsDeleted == req.IsDeleted!.Value)
                .WhereIF(req.IsShared.HasValue, f => SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id).Any() == req.IsShared!.Value)
                .WhereIF(userNameFilter != null, f => SqlFunc.Subqueryable<UserInfo>()
                    .Where(u => u.Id == f.UserId && u.UserName.Contains(userNameFilter!))
                    .Any());

            var total = await query.CountAsync();
            var orderType = string.Equals(req.Order, "asc", StringComparison.OrdinalIgnoreCase)
                ? OrderByType.Asc
                : OrderByType.Desc;

            query = (req.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "id" => query.OrderBy(f => f.Id, orderType),
                "name" => query.OrderBy(f => f.Name, orderType),
                "username" => query.OrderBy(f => SqlFunc.Subqueryable<UserInfo>().Where(u => u.Id == f.UserId).Select(u => u.UserName), orderType),
                "isfolder" => query.OrderBy(f => f.IsFolder, orderType),
                "filesize" => query.OrderBy(f => f.FileSize, orderType),
                "isdeleted" => query.OrderBy(f => f.IsDeleted, orderType),
                "isshared" => query.OrderBy(f => SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id).Any(), orderType),
                "sharetime" => query.OrderBy(f => SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id).Max(s => s.CreateTime), orderType),
                "createtime" => query.OrderBy(f => f.CreateTime, orderType),
                "updatetime" => query.OrderBy(f => f.UpdateTime, orderType),
                "deletetime" => query.OrderBy(f => f.DeleteTime, orderType),
                _ => query.OrderBy(f => f.CreateTime, OrderByType.Desc)
            };

            var items = await query
                .Skip((pageIndex - 1) * pageLength)
                .Take(pageLength)
                .Select(f => new AdminFileListItem
                {
                    Id = f.Id,
                    Name = f.Name,
                    UserId = f.UserId,
                    UserName = SqlFunc.Subqueryable<UserInfo>().Where(u => u.Id == f.UserId).Select(u => u.UserName),
                    ParentId = f.ParentId,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    IsDeleted = f.IsDeleted,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id).Any(),
                    ShareTime = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id).Max(s => s.CreateTime),
                    CreateTime = f.CreateTime,
                    UpdateTime = f.UpdateTime,
                    DeleteTime = f.DeleteTime
                })
                .ToListAsync();

            var storageIds = items.Select(i => i.Id).Distinct().ToList();
            if (storageIds.Any())
            {
                var latestShares = await _db.Queryable<ShareLink>()
                    .Where(s => storageIds.Contains(s.StorageItemId))
                    .OrderBy(s => s.CreateTime, OrderByType.Desc)
                    .Select(s => new
                    {
                        s.StorageItemId,
                        s.CreateTime,
                        s.ShareToken,
                        s.ShareCode,
                        s.ExpireTime
                    })
                    .ToListAsync();

                var latestMap = latestShares
                    .GroupBy(s => s.StorageItemId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var item in items)
                {
                    if (latestMap.TryGetValue(item.Id, out var share))
                    {
                        item.IsShared = true;
                        item.ShareTime = share.CreateTime;
                        item.ShareToken = share.ShareToken;
                        item.ShareCode = share.ShareCode;
                        item.ShareExpireTime = share.ExpireTime;
                    }
                    else
                    {
                        item.IsShared = false;
                    }
                }
            }

            return Ok(new
            {
                Items = items,
                Total = total,
                Page = pageIndex,
                PageSize = pageLength
            });
        }

        [HttpPost("files/folder")]
        public async Task<IActionResult> CreateFileFolderByAdmin([FromBody] AdminCreateFolderRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (request.UserId <= 0) return BadRequest("无效的用户ID");
            var folderName = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(folderName)) return BadRequest("文件夹名称不能为空");

            var userExists = await _db.Queryable<UserInfo>().AnyAsync(u => u.Id == request.UserId);
            if (!userExists) return NotFound("用户不存在");

            if (request.ParentId.HasValue)
            {
                var parent = await _db.Queryable<StorageItem>()
                    .FirstAsync(f => f.Id == request.ParentId.Value && f.UserId == request.UserId && f.IsFolder && !f.IsDeleted);
                if (parent == null) return BadRequest("父目录不存在");
            }

            var duplicate = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == request.UserId && f.Name == folderName && f.IsFolder && !f.IsDeleted)
                .WhereIF(request.ParentId == null, f => f.ParentId == null)
                .WhereIF(request.ParentId != null, f => f.ParentId == request.ParentId)
                .AnyAsync();
            if (duplicate) return BadRequest("同目录下已存在同名文件夹");

            var newId = await _db.Insertable(new StorageItem
            {
                UserId = request.UserId,
                Name = folderName,
                ParentId = request.ParentId,
                IsFolder = true,
                FileSize = 0,
                IsDeleted = false,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteReturnIdentityAsync();

            return Ok(new { Message = "文件夹创建成功", Id = newId });
        }

        [HttpPost("files/cancel-share/{id}")]
        public async Task<IActionResult> CancelFileShareByAdmin(int id)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (id <= 0) return BadRequest("无效的文件ID");

            var affected = await _db.Deleteable<ShareLink>()
                .Where(s => s.StorageItemId == id)
                .ExecuteCommandAsync();

            if (affected <= 0) return BadRequest("该项目当前没有分享记录");
            return Ok(new { Count = affected, Message = "取消分享成功" });
        }

        [HttpPost("files/batch-cancel-share")]
        public async Task<IActionResult> BatchCancelFileShareByAdmin([FromBody] AdminBatchFileIdsRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.Ids?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择需要取消分享的项目");

            var affected = await _db.Deleteable<ShareLink>()
                .Where(s => ids.Contains(s.StorageItemId))
                .ExecuteCommandAsync();

            return Ok(new
            {
                Count = affected,
                Message = affected > 0 ? $"已取消 {affected} 条分享" : "选中项目没有分享记录"
            });
        }

        [HttpPut("files/rename")]
        public async Task<IActionResult> RenameFileByAdmin([FromBody] AdminRenameFileRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (request.Id <= 0) return BadRequest("无效的文件ID");
            var newName = request.NewName?.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return BadRequest("新名称不能为空");

            var item = await _db.Queryable<StorageItem>().InSingleAsync(request.Id);
            if (item == null) return NotFound("项目不存在");

            var duplicate = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == item.UserId && f.Name == newName && f.Id != item.Id && !f.IsDeleted)
                .WhereIF(item.ParentId == null, f => f.ParentId == null)
                .WhereIF(item.ParentId != null, f => f.ParentId == item.ParentId)
                .AnyAsync();
            if (duplicate) return BadRequest("同目录下已存在同名项目");

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.Name == newName)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => f.Id == request.Id)
                .ExecuteCommandAsync();

            return Ok("重命名成功");
        }

        [HttpDelete("files/{id}")]
        public async Task<IActionResult> SoftDeleteFileByAdmin(int id)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (id <= 0) return BadRequest("无效的文件ID");

            var affected = await _db.Updateable<StorageItem>()
                .SetColumns(f => f.IsDeleted == true)
                .SetColumns(f => f.DeleteTime == DateTime.Now)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => f.Id == id)
                .ExecuteCommandAsync();

            if (affected <= 0) return NotFound("项目不存在");
            return Ok("删除成功");
        }

        [HttpDelete("files/permanent/{id}")]
        public async Task<IActionResult> PermanentDeleteFileByAdmin(int id)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            if (id <= 0) return BadRequest("无效的文件ID");
            return await BatchPermanentDeleteFilesByAdmin(new AdminBatchFileIdsRequest { Ids = new List<int> { id } });
        }

        [HttpPost("files/batch-delete")]
        public async Task<IActionResult> BatchSoftDeleteFilesByAdmin([FromBody] AdminBatchFileIdsRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.Ids?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择需要删除的项目");

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.IsDeleted == true)
                .SetColumns(f => f.DeleteTime == DateTime.Now)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => ids.Contains(f.Id))
                .ExecuteCommandAsync();

            return Ok("批量删除成功");
        }

        [HttpPost("files/batch-permanent-delete")]
        public async Task<IActionResult> BatchPermanentDeleteFilesByAdmin([FromBody] AdminBatchFileIdsRequest request)
        {
            if (!await CheckIsAdmin()) return Forbid("无权访问管理员接口");
            var ids = request?.Ids?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
            if (!ids.Any()) return BadRequest("请选择需要彻底删除的项目");

            var allIdsToDelete = new HashSet<int>(ids);
            var currentLevelIds = ids;

            while (true)
            {
                var childIds = await _db.Queryable<StorageItem>()
                    .Where(f => f.ParentId != null && currentLevelIds.Contains((int)f.ParentId))
                    .Select(f => f.Id)
                    .ToListAsync();

                if (!childIds.Any()) break;

                var newIds = childIds.Where(childId => !allIdsToDelete.Contains(childId)).ToList();
                if (!newIds.Any()) break;

                foreach (var newId in newIds) allIdsToDelete.Add(newId);
                currentLevelIds = newIds;
            }

            var allIds = allIdsToDelete.ToList();
            var allItems = await _db.Queryable<StorageItem>()
                .Where(f => allIds.Contains(f.Id))
                .ToListAsync();
            if (!allItems.Any()) return Ok("没有需要删除的项目");

            var fileItems = allItems.Where(i => !i.IsFolder && !string.IsNullOrEmpty(i.FilePath)).ToList();
            var filePaths = fileItems.Select(i => i.FilePath!).Distinct().ToList();
            var protectedPaths = new HashSet<string>();
            if (filePaths.Any())
            {
                var protectedList = await _db.Queryable<StorageItem>()
                    .Where(f => filePaths.Contains(f.FilePath!) && !allIds.Contains(f.Id))
                    .Select(f => f.FilePath)
                    .Distinct()
                    .ToListAsync();
                protectedPaths = new HashSet<string>(protectedList.Where(p => !string.IsNullOrEmpty(p))!);
            }

            var deletedPathSet = new HashSet<string>();
            var userFreedSize = new Dictionary<int, long>();

            foreach (var item in fileItems)
            {
                if (!protectedPaths.Contains(item.FilePath!) && !deletedPathSet.Contains(item.FilePath!))
                {
                    await _storageService.DeleteFileAsync(item.FilePath!);
                    deletedPathSet.Add(item.FilePath!);
                }

                if (!userFreedSize.ContainsKey(item.UserId))
                {
                    userFreedSize[item.UserId] = 0;
                }
                userFreedSize[item.UserId] += item.FileSize;
            }

            foreach (var pair in userFreedSize.Where(p => p.Value > 0))
            {
                var userId = pair.Key;
                var freed = pair.Value;
                await _db.Updateable<UserInfo>()
                    .SetColumns(u => u.UsedSpace == u.UsedSpace - freed)
                    .Where(u => u.Id == userId)
                    .ExecuteCommandAsync();
            }

            await _db.Deleteable<ShareLink>().Where(s => allIds.Contains(s.StorageItemId)).ExecuteCommandAsync();
            await _db.Deleteable<StorageItem>().In(allIds).ExecuteCommandAsync();

            return Ok("批量永久删除成功");
        }
    }
}
