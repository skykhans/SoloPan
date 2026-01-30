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

        [AllowAnonymous]
        [HttpGet("download/{token}")]
        public async Task<IActionResult> Download(string token, [FromQuery] string code)
        {
            var share = await _db.Queryable<ShareLink>()
                .FirstAsync(s => s.ShareToken == token);

            if (share == null) return NotFound("分享不存在");
            if (share.ExpireTime.HasValue && share.ExpireTime < DateTime.Now) return BadRequest("分享已过期");
            if (share.ShareCode != code) return BadRequest("提取码错误");

            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == share.StorageItemId && !f.IsDeleted);

            if (item == null) return NotFound("文件不存在");
            if (item.IsFolder) return BadRequest("文件夹暂不支持直接下载");

            var fullPath = _storageService.GetFullPath(item.FilePath!);
            if (!System.IO.File.Exists(fullPath)) return NotFound("物理文件丢失");

            // 更新下载次数
            await _db.Updateable<ShareLink>()
                .SetColumns(s => s.DownloadCount == s.DownloadCount + 1)
                .Where(s => s.Id == share.Id)
                .ExecuteCommandAsync();

            var contentType = GetContentType(item.Name);
            return PhysicalFile(fullPath, contentType, item.Name, enableRangeProcessing: true);
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveToDrive([FromBody] SaveShareRequest request)
        {
            var userId = GetUserId();
            
            var share = await _db.Queryable<ShareLink>()
                .FirstAsync(s => s.ShareToken == request.ShareToken);

            if (share == null) return NotFound("分享不存在");
            if (share.ExpireTime.HasValue && share.ExpireTime < DateTime.Now) return BadRequest("分享已过期");
            if (share.ShareCode != request.ShareCode) return BadRequest("提取码错误");

            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == share.StorageItemId && !f.IsDeleted);

            if (item == null) return NotFound("文件不存在");
            
            // 检查是否已经是自己的文件
            if (item.UserId == userId) return BadRequest("你不能保存自己分享的文件");

            try 
            {
                await CopyItemRecursive(item, request.TargetParentId, userId);

                // 更新下载次数
                await _db.Updateable<ShareLink>()
                    .SetColumns(s => s.DownloadCount == s.DownloadCount + 1)
                    .Where(s => s.Id == share.Id)
                    .ExecuteCommandAsync();

                return Ok("保存成功");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task CopyItemRecursive(StorageItem sourceItem, int? parentId, int userId)
        {
             if (sourceItem.IsFolder)
             {
                 var newFolder = new StorageItem
                 {
                     Name = sourceItem.Name,
                     ParentId = parentId,
                     UserId = userId,
                     IsFolder = true,
                     CreateTime = DateTime.Now,
                     UpdateTime = DateTime.Now
                 };
                 var newFolderId = await _db.Insertable(newFolder).ExecuteReturnIdentityAsync();
                 
                 var children = await _db.Queryable<StorageItem>()
                     .Where(f => f.ParentId == sourceItem.Id && !f.IsDeleted)
                     .ToListAsync();
                 
                 foreach (var child in children)
                 {
                     await CopyItemRecursive(child, newFolderId, userId);
                 }
             }
             else
             {
                 // 检查空间
                 var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
                 if (user.UsedSpace + sourceItem.FileSize > user.TotalSpace)
                 {
                      throw new Exception($"存储空间不足，无法保存文件: {sourceItem.Name}");
                 }

                 var newItem = new StorageItem
                 {
                     Name = sourceItem.Name,
                     ParentId = parentId,
                     UserId = userId,
                     IsFolder = false,
                     FileSize = sourceItem.FileSize,
                     FileMd5 = sourceItem.FileMd5,
                     FilePath = sourceItem.FilePath,
                     CreateTime = DateTime.Now,
                     UpdateTime = DateTime.Now
                 };
                 
                 await _db.Insertable(newItem).ExecuteCommandAsync();
                 
                 await _db.Updateable<UserInfo>()
                    .SetColumns(u => u.UsedSpace == u.UsedSpace + sourceItem.FileSize)
                    .Where(u => u.Id == userId)
                    .ExecuteCommandAsync();
             }
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                _ => "application/octet-stream"
            };
        }
    }
}
