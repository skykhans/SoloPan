using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Security.Claims;
using PanSystem.Models;
using PanSystem.DTOs;
using PanSystem.Services;

using PanSystem.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ImageMagick;

namespace PanSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly ISqlSugarClient _db;
        private readonly IStorageService _storageService;

        public FileController(ISqlSugarClient db, IStorageService storageService)
        {
            _db = db;
            _storageService = storageService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("list")]
        public async Task<IActionResult> List(int? parentId, string? category, string? sortBy, string? order)
        {
            var userId = GetUserId();
            var query = _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && !f.IsDeleted);

            if (!string.IsNullOrEmpty(category))
            {
                var extensions = GetExtensionsByCategory(category);
                if (extensions.Any())
                {
                    query = query.Where(f => !f.IsFolder && extensions.Contains(Path.GetExtension(f.Name).ToLower()));
                }
            }
            else
            {
                query = query.Where(f => f.ParentId == parentId);
            }

            // 应用排序
            // 默认排序：文件夹优先，然后按名称
            if (string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderBy(f => f.IsFolder, OrderByType.Desc).OrderBy(f => f.Name);
            }
            else
            {
                // 先按文件夹优先排序
                query = query.OrderBy(f => f.IsFolder, OrderByType.Desc);

                var orderType = order?.ToLower() == "desc" ? OrderByType.Desc : OrderByType.Asc;
                switch (sortBy.ToLower())
                {
                    case "name":
                        query = query.OrderBy(f => f.Name, orderType);
                        break;
                    case "size":
                        query = query.OrderBy(f => f.FileSize, orderType);
                        break;
                    case "time":
                        query = query.OrderBy(f => f.UpdateTime, orderType); // 改为 UpdateTime，通常用户更关心最后修改时间
                        break;
                    default:
                        query = query.OrderBy(f => f.Name, orderType);
                        break;
                }
            }

            var items = await query
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    ParentId = f.ParentId,
                    IsFavorite = f.IsFavorite,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id && s.UserId == userId).Any()
                })
                .ToListAsync();

            return Ok(items);
        }

        private string[] GetExtensionsByCategory(string category)
        {
            return category.ToLower() switch
            {
                "image" => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".heif" },
                "video" => new[] { ".mp4", ".avi", ".mkv", ".mov", ".flv", ".webm" },
                "audio" => new[] { ".mp3", ".wav", ".flac", ".aac" },
                "document" => new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" },
                _ => Array.Empty<string>()
            };
        }

        [HttpPost("check-md5")]
        public async Task<IActionResult> CheckMd5(Md5CheckRequest request)
        {
            var userId = GetUserId();

            // 处理文件名冲突
            var uniqueName = await GetUniqueName(request.FileName, request.ParentId, userId);

            // 查找系统中是否存在该 MD5 的文件（不分用户，实现全局秒传）
            var existingFile = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.FileMd5 == request.Md5 && !f.IsFolder);

            if (existingFile != null)
            {
                // 检查用户空间
                var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
                if (user.UsedSpace + request.FileSize > user.TotalSpace)
                {
                    return BadRequest("存储空间不足");
                }

                // 存在相同 MD5 文件，直接在数据库创建关联记录（秒传）
                var newItem = new StorageItem
                {
                    Name = uniqueName,
                    ParentId = request.ParentId,
                    UserId = userId,
                    IsFolder = false,
                    FileSize = request.FileSize,
                    FileMd5 = request.Md5,
                    FilePath = existingFile.FilePath, // 引用同一个物理路径
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                await _db.Insertable(newItem).ExecuteCommandAsync();

                // 更新用户空间
                await _db.Updateable<UserInfo>()
                    .SetColumns(u => u.UsedSpace == u.UsedSpace + request.FileSize)
                    .Where(u => u.Id == userId)
                    .ExecuteCommandAsync();

                return Ok(new { Message = "秒传成功", ItemId = newItem.Id });
            }

            return NotFound("文件不存在，请正常上传");
        }

        [HttpPost("folder")]
        public async Task<IActionResult> CreateFolder(CreateFolderRequest request)
        {
            var userId = GetUserId();

            // 检查同名
            var isExist = await _db.Queryable<StorageItem>()
                .AnyAsync(f => f.ParentId == request.ParentId && f.UserId == userId && f.Name == request.Name && !f.IsDeleted);
            if (isExist) return BadRequest("已存在同名文件夹或文件");

            var folder = new StorageItem
            {
                Name = request.Name,
                ParentId = request.ParentId,
                UserId = userId,
                IsFolder = true,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            var newId = await _db.Insertable(folder).ExecuteReturnIdentityAsync();
            return Ok(new { Message = "文件夹创建成功", ItemId = newId });
        }

        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] int? parentId)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest("文件为空");

                var userId = GetUserId();

                // 处理文件名冲突
                var uniqueName = await GetUniqueName(file.FileName, parentId, userId);

                // 计算 MD5 以便后续秒传
                string md5;
                using (var stream = file.OpenReadStream())
                {
                    md5 = HashHelper.ComputeMd5(stream);
                }

                // 再次检查 MD5（防止前端没调用 check-md5）
                var existingFile = await _db.Queryable<StorageItem>()
                    .FirstAsync(f => f.FileMd5 == md5 && !f.IsFolder);

                var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
                if (user.UsedSpace + file.Length > user.TotalSpace)
                {
                    return BadRequest("存储空间不足");
                }

                string relativePath;
                if (existingFile != null)
                {
                    relativePath = existingFile.FilePath!;
                }
                else
                {
                    using (var stream = file.OpenReadStream())
                    {
                        relativePath = await _storageService.SaveFileAsync(stream, file.FileName);
                    }
                }

                var storageItem = new StorageItem
                {
                    Name = uniqueName,
                    ParentId = parentId,
                    UserId = userId,
                    IsFolder = false,
                    FileSize = file.Length,
                    FileMd5 = md5,
                    FilePath = relativePath,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                await _db.Insertable(storageItem).ExecuteCommandAsync();

                await _db.Updateable<UserInfo>()
                    .SetColumns(u => u.UsedSpace == u.UsedSpace + file.Length)
                    .Where(u => u.Id == userId)
                    .ExecuteCommandAsync();

                return Ok("上传成功");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"上传失败: {ex.Message}");
            }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(int id, [FromQuery] bool preview = false)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId && !f.IsFolder);

            if (item == null) return NotFound("文件不存在");

            var fullPath = _storageService.GetFullPath(item.FilePath!);
            if (!System.IO.File.Exists(fullPath)) return NotFound("物理文件丢失");

            var contentType = GetContentType(item.Name);

            // 如果是预览模式，不设置 fileDownloadName，这样浏览器会尝试内联显示
            if (preview)
            {
                Response.Headers.Append("Content-Disposition", "inline; filename=\"" + System.Web.HttpUtility.UrlEncode(item.Name) + "\"");
                return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
            }

            return PhysicalFile(fullPath, contentType, item.Name, enableRangeProcessing: true);
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
                ".xml" => "text/xml",
                ".md" => "text/markdown",
                ".log" => "text/plain",
                ".ini" => "text/plain",
                ".conf" => "text/plain",
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

        [HttpGet("thumbnail/{id}")]
        public async Task<IActionResult> GetThumbnail(int id)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId && !f.IsFolder);

            if (item == null) return NotFound("文件不存在");

            var fullPath = _storageService.GetFullPath(item.FilePath!);
            if (!System.IO.File.Exists(fullPath)) return NotFound("物理文件丢失");

            // 简单判断是不是图片
            var ext = Path.GetExtension(item.Name).ToLower();
            if (!new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".heic", ".heif" }.Contains(ext))
            {
                // 不是图片，返回默认图标或原文件?
                // 暂时重定向到下载，携带 token
                var token = Request.Query["access_token"];
                return RedirectToAction(nameof(Download), new { id = id, access_token = token });
            }

            // 特殊处理 HEIC/HEIF
            if (ext == ".heic" || ext == ".heif")
            {
                try
                {
                    using var magickImage = new MagickImage(fullPath);
                    // 调整大小
                    magickImage.Resize(100, 100);
                    // 转换为 Jpeg
                    magickImage.Format = MagickFormat.Jpeg;

                    var memory = new MemoryStream();
                    await magickImage.WriteAsync(memory);
                    memory.Position = 0;
                    return File(memory, "image/jpeg");
                }
                catch
                {
                    var token = Request.Query["access_token"];
                    return RedirectToAction(nameof(Download), new { id = id, access_token = token });
                }
            }

            // 使用 ImageSharp 生成缩略图
            try
            {
                using var image = await Image.LoadAsync(fullPath);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(100, 100),
                    Mode = ResizeMode.Max
                }));

                var memory = new MemoryStream();
                await image.SaveAsJpegAsync(memory);
                memory.Position = 0;
                return File(memory, "image/jpeg");
            }
            catch
            {
                // 生成失败，返回原图
                var token = Request.Query["access_token"];
                return RedirectToAction(nameof(Download), new { id = id, access_token = token });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId);

            if (item == null) return NotFound("项目不存在");

            // 软删除
            item.IsDeleted = true;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).ExecuteCommandAsync();

            return Ok("删除成功");
        }

        [HttpPut("rename")]
        public async Task<IActionResult> Rename(RenameRequest request)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == request.Id && f.UserId == userId);

            if (item == null) return NotFound("项目不存在");

            // 检查同名 (排除自己)
            var isExist = await _db.Queryable<StorageItem>()
                .AnyAsync(f => f.ParentId == item.ParentId && f.UserId == userId && f.Name == request.NewName && f.Id != request.Id && !f.IsDeleted);
            if (isExist) return BadRequest("已存在同名文件夹或文件");

            item.Name = request.NewName;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).ExecuteCommandAsync();

            return Ok("重命名成功");
        }

        [HttpGet("recycle-bin")]
        public async Task<IActionResult> GetRecycleBin()
        {
            var userId = GetUserId();
            var items = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && f.IsDeleted)
                .OrderBy(f => f.UpdateTime, OrderByType.Desc)
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    ParentId = f.ParentId
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId && f.IsDeleted);

            if (item == null) return NotFound("项目不存在或未被删除");

            item.IsDeleted = false;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).ExecuteCommandAsync();

            return Ok("还原成功");
        }

        [HttpDelete("permanent/{id}")]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId && f.IsDeleted);

            if (item == null) return NotFound("项目不存在或未在回收站中");

            if (item.IsFolder)
            {
                // 如果是文件夹，递归删除所有子项 (此处简化处理，只删除文件夹记录)
                // 实际生产中应递归物理删除所有子文件
                await _db.Deleteable<StorageItem>().In(item.Id).ExecuteCommandAsync();
            }
            else
            {
                await _storageService.DeleteFileAsync(item.FilePath!);

                // 释放用户空间
                await _db.Updateable<UserInfo>()
                    .SetColumns(u => u.UsedSpace == u.UsedSpace - item.FileSize)
                    .Where(u => u.Id == userId)
                    .ExecuteCommandAsync();

                // 删除关联的分享记录
                await _db.Deleteable<ShareLink>().Where(s => s.StorageItemId == item.Id).ExecuteCommandAsync();

                await _db.Deleteable<StorageItem>().In(item.Id).ExecuteCommandAsync();
            }

            return Ok("永久删除成功");
        }

        [HttpPost("favorite/{id}")]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId);

            if (item == null) return NotFound("文件不存在");

            item.IsFavorite = !item.IsFavorite;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).UpdateColumns(it => new { it.IsFavorite, it.UpdateTime }).ExecuteCommandAsync();

            return Ok(new { IsFavorite = item.IsFavorite });
        }

        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var userId = GetUserId();
            var items = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && f.IsFavorite && !f.IsDeleted)
                .OrderBy(f => f.UpdateTime, OrderByType.Desc)
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    ParentId = f.ParentId,
                    IsFavorite = true,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id && s.UserId == userId).Any()
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return BadRequest("搜索关键词不能为空");

            var userId = GetUserId();
            var items = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && f.Name.Contains(keyword) && !f.IsDeleted)
                .OrderBy(f => f.IsFolder, OrderByType.Desc)
                .OrderBy(f => f.Name)
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    ParentId = f.ParentId,
                    IsFavorite = f.IsFavorite,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id && s.UserId == userId).Any()
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move(MoveRequest request)
        {
            var userId = GetUserId();

            // 检查目标文件夹是否存在且属于该用户 (如果不是根目录)
            if (request.TargetParentId.HasValue)
            {
                var targetFolder = await _db.Queryable<StorageItem>()
                    .FirstAsync(f => f.Id == request.TargetParentId && f.UserId == userId && f.IsFolder && !f.IsDeleted);
                if (targetFolder == null) return BadRequest("目标文件夹不存在");
            }

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.ParentId == request.TargetParentId)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => request.Ids.Contains(f.Id) && f.UserId == userId)
                .ExecuteCommandAsync();

            return Ok("移动成功");
        }

        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete(BatchDeleteRequest request)
        {
            var userId = GetUserId();

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.IsDeleted == true)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => request.Ids.Contains(f.Id) && f.UserId == userId)
                .ExecuteCommandAsync();

            return Ok("批量删除成功");
        }

        [HttpPost("batch-delete-permanent")]
        public async Task<IActionResult> BatchPermanentDelete(BatchDeleteRequest request)
        {
            var userId = GetUserId();
            var items = await _db.Queryable<StorageItem>()
                .Where(f => request.Ids.Contains(f.Id) && f.UserId == userId && f.IsDeleted)
                .ToListAsync();

            if (items.Count == 0) return Ok("没有需要删除的文件");

            foreach (var item in items)
            {
                if (!item.IsFolder)
                {
                    // 删除物理文件
                    if (!string.IsNullOrEmpty(item.FilePath))
                    {
                        await _storageService.DeleteFileAsync(item.FilePath);
                    }

                    // 释放用户空间
                    await _db.Updateable<UserInfo>()
                        .SetColumns(u => u.UsedSpace == u.UsedSpace - item.FileSize)
                        .Where(u => u.Id == userId)
                        .ExecuteCommandAsync();
                }
            }

            var ids = items.Select(i => i.Id).ToList();

            // 删除关联的分享记录
            await _db.Deleteable<ShareLink>().Where(s => ids.Contains(s.StorageItemId)).ExecuteCommandAsync();

            // 批量删除数据库记录
            await _db.Deleteable<StorageItem>().In(ids).ExecuteCommandAsync();

            return Ok("批量永久删除成功");
        }

        private async Task<string> GetUniqueName(string name, int? parentId, int userId)
        {
            var existingNames = await _db.Queryable<StorageItem>()
                .Where(f => f.ParentId == parentId && f.UserId == userId && !f.IsDeleted)
                .Select(f => f.Name)
                .ToListAsync();

            if (!existingNames.Contains(name))
            {
                return name;
            }

            var extension = Path.GetExtension(name);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
            int count = 1;

            string newName;
            do
            {
                newName = string.IsNullOrEmpty(extension)
                    ? $"{nameWithoutExtension} ({count})"
                    : $"{nameWithoutExtension} ({count}){extension}";
                count++;
            } while (existingNames.Contains(newName));

            return newName;
        }
    }
}
