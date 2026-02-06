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
        private readonly IWebHostEnvironment _env;
        private readonly IAuditService _auditService;
        private readonly OfflineDownloadQueue _offlineQueue;
        private static readonly System.Threading.SemaphoreSlim _folderLock = new System.Threading.SemaphoreSlim(1, 1);

        public FileController(ISqlSugarClient db, IStorageService storageService, IWebHostEnvironment env, IAuditService auditService, OfflineDownloadQueue offlineQueue)
        {
            _db = db;
            _storageService = storageService;
            _env = env;
            _auditService = auditService;
            _offlineQueue = offlineQueue;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("list")]
        public async Task<IActionResult> List(int? parentId, string? category, string? sortBy, string? order, int page = 1, int pageSize = 20)
        {
            var userId = GetUserId();
            var pageIndex = page < 1 ? 1 : page;
            var pageLength = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);
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
                    case "createtime":
                        query = query.OrderBy(f => f.CreateTime, orderType);
                        break;
                    default:
                        query = query.OrderBy(f => f.Name, orderType);
                        break;
                }
            }

            var total = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageLength)
                .Take(pageLength)
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    UpdateTime = f.UpdateTime,
                    FavoriteTime = f.FavoriteTime,
                    ParentId = f.ParentId,
                    IsFavorite = f.IsFavorite,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id && s.UserId == userId).Any()
                })
                .ToListAsync();

            return Ok(new
            {
                Items = items,
                Total = total,
                Page = pageIndex,
                PageSize = pageLength
            });
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

            // 如果有文件夹路径，先创建/获取文件夹结构
            var effectiveParentId = await GetOrCreateFolderPath(request.FolderPath, request.ParentId, userId);

            // 处理文件名冲突
            var uniqueName = await GetUniqueName(request.FileName, effectiveParentId, userId);

            // 查找系统中是否存在该 MD5 的文件（不分用户，实现全局秒传）
            var possibleFiles = await _db.Queryable<StorageItem>()
                .Where(f => f.FileMd5 == request.Md5 && !f.IsFolder && !f.IsDeleted)
                .ToListAsync();

            StorageItem? existingFile = null;
            foreach (var item in possibleFiles)
            {
                if (System.IO.File.Exists(_storageService.GetFullPath(item.FilePath!)))
                {
                    existingFile = item;
                    break;
                }
            }

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
                    ParentId = effectiveParentId,
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

                await _auditService.LogAsync(userId, User.Identity!.Name!, "秒传文件", $"文件名: {uniqueName}, 大小: {request.FileSize}", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

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

        [HttpPost("batch-create-folders")]
        public async Task<IActionResult> BatchCreateFolders(BatchCreateFoldersRequest request)
        {
            var userId = GetUserId();

            if (request.FolderPaths == null || !request.FolderPaths.Any())
            {
                return Ok(new { Message = "无需创建文件夹" });
            }

            // 获取所有唯一的文件夹路径
            var allFolderPaths = new HashSet<string>();
            foreach (var path in request.FolderPaths)
            {
                if (string.IsNullOrEmpty(path)) continue;

                // 添加所有层级的路径
                var parts = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var currentPath = "";
                foreach (var part in parts)
                {
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";
                    allFolderPaths.Add(currentPath);
                }
            }

            // 按路径深度排序，确保父文件夹先创建
            var sortedPaths = allFolderPaths.OrderBy(p => p.Split('/').Length).ToList();

            foreach (var folderPath in sortedPaths)
            {
                await GetOrCreateFolderPath(folderPath, request.ParentId, userId);
            }

            return Ok(new { Message = $"成功创建 {sortedPaths.Count} 个文件夹结构" });
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

        [HttpPost("upload-chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] ChunkUploadRequest request, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("分片为空");

            var tempRoot = Path.Combine(_env.ContentRootPath, "Temp");
            var tempPath = Path.Combine(tempRoot, request.Guid);
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

            var chunkPath = Path.Combine(tempPath, request.ChunkIndex.ToString());
            using (var stream = new FileStream(chunkPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok("分片上传成功");
        }

        [HttpPost("merge-chunks")]
        public async Task<IActionResult> MergeChunks(MergeChunksRequest request)
        {
            var userId = GetUserId();
            var tempRoot = Path.Combine(_env.ContentRootPath, "Temp");
            var tempPath = Path.Combine(tempRoot, request.Guid);

            if (request.TotalSize > 0 && !Directory.Exists(tempPath)) return BadRequest("分片不存在或已过期");

            // 如果有文件夹路径，先创建/获取文件夹结构
            var effectiveParentId = await GetOrCreateFolderPath(request.FolderPath, request.ParentId, userId);

            // 处理文件名冲突
            var uniqueName = await GetUniqueName(request.FileName, effectiveParentId, userId);

            // 检查 MD5 秒传
            var possibleFiles = await _db.Queryable<StorageItem>()
                .Where(f => f.FileMd5 == request.Md5 && !f.IsFolder && !f.IsDeleted)
                .ToListAsync();

            StorageItem? existingFile = null;
            foreach (var item in possibleFiles)
            {
                if (System.IO.File.Exists(_storageService.GetFullPath(item.FilePath!)))
                {
                    existingFile = item;
                    break;
                }
            }

            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            if (user.UsedSpace + request.TotalSize > user.TotalSpace)
            {
                return BadRequest("存储空间不足");
            }

            string relativePath;
            if (existingFile != null)
            {
                relativePath = existingFile.FilePath!;
                // 清理临时文件
                try { Directory.Delete(tempPath, true); } catch { }
            }
            else
            {
                if (request.TotalSize == 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        relativePath = await _storageService.SaveFileAsync(ms, request.FileName);
                    }
                }
                else
                {
                    // 合并分片
                    var chunkFiles = Directory.GetFiles(tempPath)
                        .Where(f => int.TryParse(Path.GetFileName(f), out _))
                        .OrderBy(f => int.Parse(Path.GetFileName(f)))
                        .ToList();

                    if (!chunkFiles.Any()) return BadRequest("未找到有效分片");

                    // 暂时存到一个临时文件
                    var finalTempFile = Path.Combine(tempPath, "final_" + Guid.NewGuid());
                    try
                    {
                        using (var finalStream = new FileStream(finalTempFile, FileMode.Create))
                        {
                            foreach (var chunkFile in chunkFiles)
                            {
                                using (var chunkStream = new FileStream(chunkFile, FileMode.Open))
                                {
                                    await chunkStream.CopyToAsync(finalStream);
                                }
                            }
                        }

                        // 计算最终 MD5 校验
                        using (var fs = new FileStream(finalTempFile, FileMode.Open))
                        {
                            var actualMd5 = HashHelper.ComputeMd5(fs);
                            if (actualMd5 != request.Md5)
                            {
                                return BadRequest("MD5 校验失败，文件可能已损坏");
                            }
                            fs.Position = 0;
                            relativePath = await _storageService.SaveFileAsync(fs, request.FileName);
                        }
                    }
                    finally
                    {
                        // 清理临时目录
                        try { Directory.Delete(tempPath, true); } catch { }
                    }
                }
            }

            var storageItem = new StorageItem
            {
                Name = uniqueName,
                ParentId = effectiveParentId,
                UserId = userId,
                IsFolder = false,
                FileSize = request.TotalSize,
                FileMd5 = request.Md5,
                FilePath = relativePath,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            await _db.Insertable(storageItem).ExecuteCommandAsync();

            await _db.Updateable<UserInfo>()
                .SetColumns(u => u.UsedSpace == u.UsedSpace + request.TotalSize)
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();

            await _auditService.LogAsync(userId, User.Identity!.Name!, "合并上传", $"文件名: {storageItem.Name}, 大小: {storageItem.FileSize}", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return Ok(new { Message = "上传成功", ItemId = storageItem.Id });
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
            item.DeleteTime = DateTime.Now;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).ExecuteCommandAsync();

            await _auditService.LogAsync(userId, User.Identity!.Name!, "删除文件", $"项目: {item.Name}", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

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
        public async Task<IActionResult> GetRecycleBin(string? sortBy, string? order, int page = 1, int pageSize = 20)
        {
            var userId = GetUserId();
            var pageIndex = page < 1 ? 1 : page;
            var pageLength = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);
            var query = _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && f.IsDeleted);

            if (string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderBy(f => f.DeleteTime, OrderByType.Desc)
                    .OrderBy(f => f.UpdateTime, OrderByType.Desc);
            }
            else
            {
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
                        query = query.OrderBy(f => f.UpdateTime, orderType);
                        break;
                    case "createtime":
                        query = query.OrderBy(f => f.CreateTime, orderType);
                        break;
                    default:
                        query = query.OrderBy(f => f.DeleteTime, OrderByType.Desc)
                            .OrderBy(f => f.UpdateTime, OrderByType.Desc);
                        break;
                }
            }

            var total = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageLength)
                .Take(pageLength)
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    UpdateTime = f.UpdateTime,
                    DeleteTime = f.DeleteTime,
                    FavoriteTime = f.FavoriteTime,
                    ParentId = f.ParentId
                })
                .ToListAsync();

            return Ok(new
            {
                Items = items,
                Total = total,
                Page = pageIndex,
                PageSize = pageLength
            });
        }

        [HttpPost("restore/{id}")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = GetUserId();
            var item = await _db.Queryable<StorageItem>()
                .FirstAsync(f => f.Id == id && f.UserId == userId && f.IsDeleted);

            if (item == null) return NotFound("项目不存在或未被删除");

            item.IsDeleted = false;
            item.DeleteTime = null;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).ExecuteCommandAsync();

            return Ok("还原成功");
        }

        [HttpPost("batch-restore")]
        public async Task<IActionResult> BatchRestore(BatchDeleteRequest request)
        {
            var userId = GetUserId();
            if (request.Ids == null || request.Ids.Count == 0) return Ok("无需还原");

            // 递归查找所有需要还原的 ID（包含子项）
            var allIdsToRestore = new HashSet<int>(request.Ids);
            var currentLevelIds = request.Ids;

            while (true)
            {
                var childIds = await _db.Queryable<StorageItem>()
                    .Where(f => f.ParentId != null && currentLevelIds.Contains((int)f.ParentId) && f.UserId == userId)
                    .Select(f => f.Id)
                    .ToListAsync();

                if (!childIds.Any()) break;

                var newIds = childIds.Where(id => !allIdsToRestore.Contains(id)).ToList();
                if (!newIds.Any()) break;

                foreach (var id in newIds) allIdsToRestore.Add(id);
                currentLevelIds = newIds;
            }

            var allIdsList = allIdsToRestore.ToList();

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.IsDeleted == false)
                .SetColumns(f => f.DeleteTime == null)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => allIdsList.Contains(f.Id) && f.UserId == userId)
                .ExecuteCommandAsync();

            await _auditService.LogAsync(userId, User.Identity!.Name!, "批量还原", $"还原了 {allIdsList.Count} 个项目", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return Ok("批量还原成功");
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
                // 如果是文件夹，需要递归找到所有子文件进行处理
                // 由于单项删除文件夹可能涉及很多子文件，建议重用批量删除的逻辑，或者仅调用批量删除方法
                // 这里为了简化，直接调用 BatchPermanentDelete 逻辑，构造包含所有子 ID 的列表

                // 为了避免代码重复，我们将复杂逻辑封装到 BatchPermanentDelete 中
                // 但这里需要特殊的内部调用方式，或者我们直接重构本方法去调用 BatchPermanentDelete 的逻辑?
                // 简单起见，我们在客户端通常调用 Batch 删除。这里为了完整性，手动处理递归比较繁琐。
                // 我们可以构造一个 list，内部处理。

                // 暂时简单处理：只允许删除空文件夹? 不，需求是彻底删除。
                // 既然已经实现了 BatchPermanentDelete 的完善递归逻辑，我们构造一个 Request 调用它
                return await BatchPermanentDelete(new BatchDeleteRequest { Ids = new List<int> { id } });
            }
            else
            {
                // Check if file is used by others
                var isReferenced = await _db.Queryable<StorageItem>()
                    .AnyAsync(f => f.FilePath == item.FilePath && f.Id != item.Id);

                if (!string.IsNullOrEmpty(item.FilePath) && !isReferenced)
                {
                    await _storageService.DeleteFileAsync(item.FilePath);
                }

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
            item.FavoriteTime = item.IsFavorite ? DateTime.Now : null;
            item.UpdateTime = DateTime.Now;
            await _db.Updateable(item).UpdateColumns(it => new { it.IsFavorite, it.FavoriteTime, it.UpdateTime }).ExecuteCommandAsync();

            return Ok(new { IsFavorite = item.IsFavorite });
        }

        [HttpPost("batch-unfavorite")]
        public async Task<IActionResult> BatchUnfavorite(BatchDeleteRequest request)
        {
            var userId = GetUserId();

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.IsFavorite == false)
                .SetColumns(f => f.FavoriteTime == null)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => request.Ids.Contains(f.Id) && f.UserId == userId)
                .ExecuteCommandAsync();

            await _auditService.LogAsync(userId, User.Identity!.Name!, "取消收藏", $"取消收藏了 {request.Ids.Count} 个项目", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return Ok("批量取消收藏成功");
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
                    UpdateTime = f.UpdateTime,
                    FavoriteTime = f.FavoriteTime,
                    ParentId = f.ParentId,
                    IsFavorite = true,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id && s.UserId == userId).Any()
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string keyword, string? sortBy, string? order, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return BadRequest("搜索关键词不能为空");

            var userId = GetUserId();
            var pageIndex = page < 1 ? 1 : page;
            var pageLength = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);
            var query = _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && f.Name.Contains(keyword) && !f.IsDeleted);

            if (string.IsNullOrEmpty(sortBy))
            {
                query = query.OrderBy(f => f.IsFolder, OrderByType.Desc).OrderBy(f => f.Name);
            }
            else
            {
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
                        query = query.OrderBy(f => f.UpdateTime, orderType);
                        break;
                    case "createtime":
                        query = query.OrderBy(f => f.CreateTime, orderType);
                        break;
                    default:
                        query = query.OrderBy(f => f.Name, orderType);
                        break;
                }
            }

            var total = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageLength)
                .Take(pageLength)
                .Select(f => new FileItemResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    IsFolder = f.IsFolder,
                    FileSize = f.FileSize,
                    CreateTime = f.CreateTime,
                    UpdateTime = f.UpdateTime,
                    ParentId = f.ParentId,
                    IsFavorite = f.IsFavorite,
                    IsShared = SqlFunc.Subqueryable<ShareLink>().Where(s => s.StorageItemId == f.Id && s.UserId == userId).Any()
                })
                .ToListAsync();

            return Ok(new
            {
                Items = items,
                Total = total,
                Page = pageIndex,
                PageSize = pageLength
            });
        }

        [HttpPost("offline-download")]
        public async Task<IActionResult> OfflineDownload([FromBody] OfflineDownloadRequest request)
        {
            var userId = GetUserId();
            var user = await _db.Queryable<UserInfo>().InSingleAsync(userId);
            if (user == null) return NotFound("用户不存在");
            if (string.IsNullOrWhiteSpace(request.Url)) return BadRequest("下载链接不能为空");

            var task = new OfflineDownloadTask
            {
                UserId = userId,
                Url = request.Url.Trim(),
                ParentId = request.ParentId,
                Status = "queued",
                Progress = 0,
                Message = "",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            var taskId = await _db.Insertable(task).ExecuteReturnIdentityAsync();
            await _offlineQueue.EnqueueAsync(new OfflineDownloadJob(taskId));

            return Ok(new { Message = "已加入离线下载队列", TaskId = taskId });
        }

        [HttpGet("offline-tasks")]
        public async Task<IActionResult> GetOfflineTasks()
        {
            var userId = GetUserId();
            var tasks = await _db.Queryable<OfflineDownloadTask>()
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.CreateTime, OrderByType.Desc)
                .Take(50)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("offline-tasks/{id}")]
        public async Task<IActionResult> GetOfflineTask(int id)
        {
            var userId = GetUserId();
            var task = await _db.Queryable<OfflineDownloadTask>()
                .FirstAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound("任务不存在");
            return Ok(task);
        }

        [HttpPut("offline-tasks/{id}")]
        public async Task<IActionResult> UpdateOfflineTask(int id, [FromBody] OfflineDownloadUpdateRequest request)
        {
            var userId = GetUserId();
            var task = await _db.Queryable<OfflineDownloadTask>()
                .FirstAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound("任务不存在");
            if (task.Status == "downloading" || task.Status == "importing")
            {
                return BadRequest("任务正在进行，无法修改");
            }

            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                task.Url = request.Url.Trim();
            }

            task.ParentId = request.ParentId;
            task.Status = "queued";
            task.Progress = 0;
            task.Message = "";
            task.UpdateTime = DateTime.Now;

            await _db.Updateable(task).ExecuteCommandAsync();
            await _offlineQueue.EnqueueAsync(new OfflineDownloadJob(task.Id));

            return Ok("任务已更新并重新入队");
        }

        [HttpPost("offline-tasks/{id}/retry")]
        public async Task<IActionResult> RetryOfflineTask(int id)
        {
            var userId = GetUserId();
            var task = await _db.Queryable<OfflineDownloadTask>()
                .FirstAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound("任务不存在");
            if (task.Status == "downloading" || task.Status == "importing")
            {
                return BadRequest("任务正在进行，无法重试");
            }

            task.Status = "queued";
            task.Progress = 0;
            task.Message = "";
            task.UpdateTime = DateTime.Now;

            await _db.Updateable(task).ExecuteCommandAsync();
            await _offlineQueue.EnqueueAsync(new OfflineDownloadJob(task.Id));

            return Ok("任务已重新入队");
        }

        [HttpDelete("offline-tasks/{id}")]
        public async Task<IActionResult> DeleteOfflineTask(int id)
        {
            var userId = GetUserId();
            var task = await _db.Queryable<OfflineDownloadTask>()
                .FirstAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) return NotFound("任务不存在");
            if (task.Status == "downloading" || task.Status == "importing")
            {
                return BadRequest("任务正在进行，暂不支持删除");
            }

            await _db.Deleteable<OfflineDownloadTask>().In(id).ExecuteCommandAsync();
            return Ok("任务已删除");
        }


        [HttpPost("empty-recycle-bin")]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            var userId = GetUserId();
            var items = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && f.IsDeleted)
                .Select(f => f.Id)
                .ToListAsync();

            if (!items.Any()) return Ok("回收站已是空的");

            // 直接重用批量彻底删除逻辑
            var request = new BatchDeleteRequest { Ids = items };
            return await BatchPermanentDelete(request);
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move(MoveRequest request)
        {
            var userId = GetUserId();
            if (request.Ids == null || request.Ids.Count == 0) return BadRequest("请选择要移动的项目");

            // 检查目标文件夹是否存在且属于该用户 (如果不是根目录)
            if (request.TargetParentId.HasValue)
            {
                var targetFolder = await _db.Queryable<StorageItem>()
                    .FirstAsync(f => f.Id == request.TargetParentId && f.UserId == userId && f.IsFolder && !f.IsDeleted);
                if (targetFolder == null) return BadRequest("目标文件夹不存在");

                // 禁止移动到自身或子目录中
                if (request.Ids.Contains(request.TargetParentId.Value))
                {
                    return BadRequest("不能移动到自身目录");
                }

                if (await IsDescendantOfAny(request.TargetParentId.Value, request.Ids, userId))
                {
                    return BadRequest("不能移动到所选文件夹的子目录");
                }
            }

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.ParentId == request.TargetParentId)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => request.Ids.Contains(f.Id) && f.UserId == userId)
                .ExecuteCommandAsync();

            await _auditService.LogAsync(userId, User.Identity!.Name!, "移动文件", $"移动了 {request.Ids.Count} 个项目", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return Ok("移动成功");
        }

        private async Task<bool> IsDescendantOfAny(int targetParentId, List<int> candidateParentIds, int userId)
        {
            var currentId = targetParentId;
            while (true)
            {
                var current = await _db.Queryable<StorageItem>()
                    .FirstAsync(f => f.Id == currentId && f.UserId == userId);

                if (current == null || current.ParentId == null) return false;
                if (candidateParentIds.Contains(current.ParentId.Value)) return true;
                currentId = current.ParentId.Value;
            }
        }

        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete(BatchDeleteRequest request)
        {
            var userId = GetUserId();

            await _db.Updateable<StorageItem>()
                .SetColumns(f => f.IsDeleted == true)
                .SetColumns(f => f.DeleteTime == DateTime.Now)
                .SetColumns(f => f.UpdateTime == DateTime.Now)
                .Where(f => request.Ids.Contains(f.Id) && f.UserId == userId)
                .ExecuteCommandAsync();

            await _auditService.LogAsync(userId, User.Identity!.Name!, "批量删除", $"删除了 {request.Ids.Count} 个项目", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");

            return Ok("批量删除成功");
        }

        [HttpPost("batch-delete-permanent")]
        public async Task<IActionResult> BatchPermanentDelete(BatchDeleteRequest request)
        {
            var userId = GetUserId();

            // 1. 递归查找所有需要删除的 ID (包括子文件/子文件夹)
            var allIdsToDelete = new HashSet<int>(request.Ids);
            var currentLevelIds = request.Ids;

            while (true)
            {
                // 查找当前层级的所有子项 (无论是否标记为已删除，只要父级被彻底删除，子级也必须删除)
                var childIds = await _db.Queryable<StorageItem>()
                    .Where(f => f.ParentId != null && currentLevelIds.Contains((int)f.ParentId) && f.UserId == userId)
                    .Select(f => f.Id)
                    .ToListAsync();

                if (!childIds.Any()) break;

                // 仅添加尚未包含的 ID
                var newIds = childIds.Where(id => !allIdsToDelete.Contains(id)).ToList();
                if (!newIds.Any()) break;

                foreach (var id in newIds) allIdsToDelete.Add(id);
                currentLevelIds = newIds;
            }

            var allIdsList = allIdsToDelete.ToList();

            // 2. 获取所有详细信息
            var allItems = await _db.Queryable<StorageItem>()
                .Where(f => allIdsList.Contains(f.Id) && f.UserId == userId)
                .ToListAsync();

            if (allItems.Count == 0) return Ok("没有需要删除的文件");

            long totalSizeFreed = 0;
            var fileItems = allItems.Where(i => !i.IsFolder && !string.IsNullOrEmpty(i.FilePath)).ToList();
            var filePaths = fileItems.Select(i => i.FilePath).Distinct().ToList();

            // 找出仍被其他用户（或当前用户未删除的文件）引用的物理文件路径
            // 条件：FilePath 在待删除集合中，但 StorageItemId 不在待删除集合中
            var protectedPaths = new HashSet<string>();
            if (filePaths.Any())
            {
                var protectedList = await _db.Queryable<StorageItem>()
                    .Where(f => filePaths.Contains(f.FilePath) && !allIdsList.Contains(f.Id))
                    .Select(f => f.FilePath)
                    .Distinct()
                    .ToListAsync();
                protectedPaths = new HashSet<string>(protectedList.Where(p => p != null)!);
            }

            var deletedPathSet = new HashSet<string>();

            // 3. 删除物理文件 & 计算释放空间
            foreach (var item in fileItems)
            {
                // 只有当文件路径未被保护，且此时尚未执行过删除操作时，才进行物理删除
                if (!protectedPaths.Contains(item.FilePath!) && !deletedPathSet.Contains(item.FilePath!))
                {
                    await _storageService.DeleteFileAsync(item.FilePath!);
                    deletedPathSet.Add(item.FilePath!); // 标记已删除，避免同一批次中重复删除
                }

                // 无论物理文件是否保留，该用户都释放了空间占用（因为他删除了自己的引用）
                totalSizeFreed += item.FileSize;
            }

            // 4. 释放用户空间
            if (totalSizeFreed > 0)
            {
                await _db.Updateable<UserInfo>()
                    .SetColumns(u => u.UsedSpace == u.UsedSpace - totalSizeFreed)
                    .Where(u => u.Id == userId)
                    .ExecuteCommandAsync();
            }

            // 5. 删除关联的分享记录 (所有层级)
            await _db.Deleteable<ShareLink>().Where(s => allIdsList.Contains(s.StorageItemId)).ExecuteCommandAsync();

            // 6. 批量删除数据库记录
            await _db.Deleteable<StorageItem>().In(allIdsList).ExecuteCommandAsync();

            return Ok("批量永久删除成功");
        }

        private async Task<string> GetUniqueName(string name, int? parentId, int userId)
        {
            var existingNames = await _db.Queryable<StorageItem>()
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .WhereIF(parentId == null, f => f.ParentId == null)
                .WhereIF(parentId != null, f => f.ParentId == parentId)
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

        private async Task<int?> GetOrCreateFolderPath(string? folderPath, int? parentId, int userId)
        {
            if (string.IsNullOrEmpty(folderPath)) return parentId;

            var folders = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var currentParentId = parentId;

            await _folderLock.WaitAsync();
            try
            {
                foreach (var folderName in folders)
                {
                    var name = folderName.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    var query = _db.Queryable<StorageItem>()
                        .Where(f => f.UserId == userId && f.Name == name && f.IsFolder && !f.IsDeleted)
                        .WhereIF(currentParentId == null, f => f.ParentId == null)
                        .WhereIF(currentParentId != null, f => f.ParentId == currentParentId);

                    var existingFolder = await query.FirstAsync();

                    if (existingFolder != null)
                    {
                        currentParentId = existingFolder.Id;
                    }
                    else
                    {
                        var newFolder = new StorageItem
                        {
                            Name = name,
                            ParentId = currentParentId,
                            UserId = userId,
                            IsFolder = true,
                            CreateTime = DateTime.Now,
                            UpdateTime = DateTime.Now,
                            IsDeleted = false
                        };
                        currentParentId = await _db.Insertable(newFolder).ExecuteReturnIdentityAsync();
                    }
                }
            }
            finally
            {
                _folderLock.Release();
            }

            return currentParentId;
        }
    }
}
