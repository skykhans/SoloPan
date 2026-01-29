using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;

namespace PanSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController : ControllerBase
    {
        private readonly ISqlSugarClient _db;

        public ShareController(ISqlSugarClient db)
        {
            _db = db;
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
                .LeftJoin<StorageItem>((s, f) => s.StorageItemId == f.Id)
                .Where(s => s.UserId == userId)
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
    }
}
