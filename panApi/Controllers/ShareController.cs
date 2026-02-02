using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;
using PanSystem.Services;

namespace PanSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController : ControllerBase
    {
        private readonly ISqlSugarClient _db;
        private readonly IStorageService _storageService;

        public ShareController(ISqlSugarClient db, IStorageService storageService)
        {
            _db = db;
            _storageService = storageService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("create")]
        public async Task<IActionResult> CreateShare(CreateShareRequest request)
        {
            var userId = GetUserId();
            var file = await _db.Queryable<StorageItem>().FirstAsync(f => f.Id == request.StorageItemId && f.UserId == userId);

            if (file == null) return NotFound("文件不存在");

            var shareCode = Guid.NewGuid().ToString().Substring(0, 4); // 简单生成4位提取码
            var shareToken = Guid.NewGuid().ToString("N");

            var shareLink = new ShareLink
            {
                UserId = userId,
                StorageItemId = request.StorageItemId,
                ShareCode = shareCode,
                ShareToken = shareToken,
                CreateTime = DateTime.Now,
                ExpireTime = request.ExpireDays > 0 ? DateTime.Now.AddDays(request.ExpireDays) : null,
                ViewCount = 0,
                DownloadCount = 0
            };

            await _db.Insertable(shareLink).ExecuteCommandAsync();

            return Ok(new ShareResponse
            {
                ShareToken = shareToken,
                ShareCode = shareCode,
                ExpireTime = shareLink.ExpireTime
            });
        }

        [AllowAnonymous]
        [HttpGet("detail/{token}")]
        public async Task<IActionResult> GetShareDetail(string token)
        {
            var share = await _db.Queryable<ShareLink>()
                .FirstAsync(s => s.ShareToken == token);

            if (share == null) return NotFound("分享不存在");
            if (share.ExpireTime.HasValue && share.ExpireTime < DateTime.Now) return BadRequest("分享已过期");

            var file = await _db.Queryable<StorageItem>().InSingleAsync(share.StorageItemId);
            var user = await _db.Queryable<UserInfo>().InSingleAsync(share.UserId);

            // 增加浏览次数
            await _db.Updateable<ShareLink>()
                .SetColumns(s => s.ViewCount == s.ViewCount + 1)
                .Where(s => s.Id == share.Id)
                .ExecuteCommandAsync();

            return Ok(new ShareDetailResponse
            {
                Name = file.Name,
                FileSize = file.FileSize,
                IsFolder = file.IsFolder,
                UserName = user.UserName,
                ExpireTime = share.ExpireTime
            });
        }

        [AllowAnonymous]
        [HttpPost("check-code")]
        public async Task<IActionResult> CheckCode(AccessShareRequest request)
        {
            var share = await _db.Queryable<ShareLink>()
               .FirstAsync(s => s.ShareToken == request.ShareToken);

            if (share == null) return NotFound("分享不存在");
            if (share.ExpireTime.HasValue && share.ExpireTime < DateTime.Now) return BadRequest("分享已过期");

            if (share.ShareCode != request.ShareCode) return BadRequest("提取码错误");

            // 返回 token 或 cookie 标识已有权限（此处简化处理，验证通过即可）
            return Ok(new { Token = request.ShareToken });
        }

        [HttpGet("my-shares")]
        public async Task<IActionResult> MyShares()
        {
            var userId = GetUserId();
            var shares = await _db.Queryable<ShareLink>()
                .InnerJoin<StorageItem>((s, f) => s.StorageItemId == f.Id)
                .Where((s, f) => s.UserId == userId && !f.IsDeleted) // 仅显示未删除文件的分享
                .Select((s, f) => new
                {
                    s.Id,
                    s.ShareCode,
                    s.ShareToken,
                    s.CreateTime,
                    s.ExpireTime,
                    s.ViewCount,
                    s.DownloadCount,
                    FileName = f.Name,
                    f.IsFolder
                })
                .ToListAsync();

            return Ok(shares);
        }

        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelShare(int id)
        {
            var userId = GetUserId();
            var share = await _db.Queryable<ShareLink>().FirstAsync(s => s.Id == id && s.UserId == userId);

            if (share == null) return NotFound("分享不存在");

            await _db.Deleteable(share).ExecuteCommandAsync();
            return Ok("取消分享成功");
        }

        [AllowAnonymous]
        [HttpGet("download/{token}")]
        public async Task<IActionResult> DownloadShare(string token, [FromQuery] string code)
        {
            var share = await _db.Queryable<ShareLink>()
                .FirstAsync(s => s.ShareToken == token);

            if (share == null) return NotFound("分享不存在");
            if (share.ExpireTime.HasValue && share.ExpireTime < DateTime.Now) return BadRequest("分享已过期");
            if (share.ShareCode != code) return BadRequest("提取码错误");

            var item = await _db.Queryable<StorageItem>().InSingleAsync(share.StorageItemId);
            if (item == null) return NotFound("文件不存在");

            var fullPath = _storageService.GetFullPath(item.FilePath!);
            if (!System.IO.File.Exists(fullPath)) return NotFound("物理文件丢失");

            // 增加下载次数
            await _db.Updateable<ShareLink>()
                .SetColumns(s => s.DownloadCount == s.DownloadCount + 1)
                .Where(s => s.Id == share.Id)
                .ExecuteCommandAsync();

            var ext = Path.GetExtension(item.Name).ToLower();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType, item.Name, enableRangeProcessing: true);
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveToDrive(SaveShareRequest request)
        {
            var userId = GetUserId();
            var share = await _db.Queryable<ShareLink>()
                .FirstAsync(s => s.ShareToken == request.ShareToken);

            if (share == null) return NotFound("分享不存在");
            if (share.ExpireTime.HasValue && share.ExpireTime < DateTime.Now) return BadRequest("分享已过期");
            if (share.ShareCode != request.ShareCode) return BadRequest("提取码错误");

            var sourceItem = await _db.Queryable<StorageItem>().InSingleAsync(share.StorageItemId);
            if (sourceItem == null) return NotFound("源文件不存在");

            // 秒传逻辑：在当前用户的目录下创建一个指向相同物理文件的新记录
            var newItem = new StorageItem
            {
                Name = sourceItem.Name,
                ParentId = request.TargetParentId,
                UserId = userId,
                IsFolder = sourceItem.IsFolder,
                FileSize = sourceItem.FileSize,
                FileMd5 = sourceItem.FileMd5,
                FilePath = sourceItem.FilePath,
                IsDeleted = false,
                IsFavorite = false,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            await _db.Insertable(newItem).ExecuteCommandAsync();

            // 更新用户空间
            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.UsedSpace == u.UsedSpace + sourceItem.FileSize)
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            return Ok("保存成功");
        }
    }
}
