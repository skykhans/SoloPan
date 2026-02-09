using System.Diagnostics;
using System.Text;
using PanSystem.Models;
using SqlSugar;

namespace PanSystem.Services
{
    public class OfflineDownloadWorker : BackgroundService
    {
        private readonly OfflineDownloadQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OfflineDownloadWorker> _logger;

        public OfflineDownloadWorker(OfflineDownloadQueue queue, IServiceScopeFactory scopeFactory, ILogger<OfflineDownloadWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RequeuePendingTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离线下载启动补偿入队失败");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                OfflineDownloadJob job;
                try
                {
                    job = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "离线下载任务处理异常，TaskId={TaskId}", job.TaskId);
                }
            }
        }

        private async Task RequeuePendingTasksAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var pendingTasks = await db.Queryable<OfflineDownloadTask>()
                .Where(t => t.Status == "queued" || t.Status == "downloading" || t.Status == "importing")
                .OrderBy(t => t.CreateTime, OrderByType.Asc)
                .ToListAsync();

            if (!pendingTasks.Any()) return;

            foreach (var task in pendingTasks)
            {
                if (stoppingToken.IsCancellationRequested) break;

                if (task.Status != "queued")
                {
                    await UpdateTaskAsync(db, task.Id, "queued", 0, "服务重启，任务已重新入队");
                }

                await _queue.EnqueueAsync(new OfflineDownloadJob(task.Id));
            }
        }

        private async Task ProcessJobAsync(OfflineDownloadJob job, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var task = await db.Queryable<OfflineDownloadTask>().InSingleAsync(job.TaskId);
            if (task == null) return;

            var user = await db.Queryable<UserInfo>().InSingleAsync(task.UserId);
            if (user == null)
            {
                await UpdateTaskAsync(db, task.Id, "failed", 0, "用户不存在");
                return;
            }

            try
            {
                await UpdateTaskAsync(db, task.Id, "downloading", 5, null);

                if (IsP2pUrl(task.Url))
                {
                    await HandleP2pAsync(db, storageService, auditService, env, config, task, user, stoppingToken);
                }
                else
                {
                    await HandleHttpAsync(db, storageService, auditService, httpClientFactory, task, user, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await UpdateTaskAsync(db, task.Id, "failed", task.Progress, ex.Message);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "离线下载任务状态回写失败，TaskId={TaskId}", task.Id);
                }
            }
        }

        private async Task HandleHttpAsync(ISqlSugarClient db, IStorageService storageService, IAuditService auditService, IHttpClientFactory httpClientFactory, OfflineDownloadTask task, UserInfo user, CancellationToken ct)
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            if (!client.DefaultRequestHeaders.UserAgent.Any())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PanSystemOfflineDownloader/1.0");
            }

            var response = await client.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                await UpdateTaskAsync(db, task.Id, "failed", task.Progress, $"无法从链接下载文件，状态码: {response.StatusCode}");
                return;
            }

            var fileName = "";
            var contentDisposition = response.Content.Headers.ContentDisposition;
            if (contentDisposition != null && !string.IsNullOrEmpty(contentDisposition.FileName))
            {
                fileName = contentDisposition.FileName.Trim('\"');
            }

            if (string.IsNullOrEmpty(fileName))
            {
                try { fileName = Path.GetFileName(new Uri(task.Url).LocalPath); } catch { }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "offline_file_" + DateTime.Now.Ticks;
            }

            var contentLength = response.Content.Headers.ContentLength ?? 0;
            if (contentLength > 0 && user.UsedSpace + contentLength > user.TotalSpace)
            {
                await UpdateTaskAsync(db, task.Id, "failed", task.Progress, "存储空间不足");
                return;
            }

            var uniqueName = await GetUniqueNameAsync(db, fileName, task.UserId, task.ParentId);

            await UpdateTaskAsync(db, task.Id, "importing", 70, null);

            using (var stream = await response.Content.ReadAsStreamAsync(ct))
            {
                var relativePath = await storageService.SaveFileAsync(stream, uniqueName);

                if (contentLength == 0)
                {
                    var fullPath = storageService.GetFullPath(relativePath);
                    contentLength = new FileInfo(fullPath).Length;
                }

                var storageItem = new StorageItem
                {
                    Name = uniqueName,
                    ParentId = task.ParentId,
                    UserId = task.UserId,
                    IsFolder = false,
                    FileSize = contentLength,
                    FilePath = relativePath,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                await db.Insertable(storageItem).ExecuteCommandAsync();

                if (contentLength > 0)
                {
                    await db.Updateable<UserInfo>()
                        .SetColumns(u => u.UsedSpace == u.UsedSpace + contentLength)
                        .Where(u => u.Id == task.UserId)
                        .ExecuteCommandAsync();
                }

                await auditService.LogAsync(task.UserId, user.UserName, "离线下载", $"URL: {task.Url}, 文件: {uniqueName}", "");
            }

            await UpdateTaskAsync(db, task.Id, "completed", 100, null);
        }

        private async Task HandleP2pAsync(ISqlSugarClient db, IStorageService storageService, IAuditService auditService, IWebHostEnvironment env, IConfiguration config, OfflineDownloadTask task, UserInfo user, CancellationToken ct)
        {
            var aria2Path = ResolveAria2Path(config, env);
            if (string.IsNullOrWhiteSpace(aria2Path))
            {
                await UpdateTaskAsync(db, task.Id, "failed", task.Progress, "未找到 aria2c，请安装后在配置中设置 Aria2:Path 或加入 PATH");
                return;
            }

            var tempRoot = Path.Combine(env.ContentRootPath, "Temp", "Offline");
            Directory.CreateDirectory(tempRoot);
            var taskDir = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(taskDir);

            try
            {
                var progress = 10;
                await UpdateTaskAsync(db, task.Id, "downloading", progress, null);

                var (exitCode, stdout, stderr, timedOut) = await RunAria2Async(aria2Path, task.Url, taskDir, TimeSpan.FromHours(2), async p =>
                {
                    if (p > progress)
                    {
                        progress = p;
                        await UpdateTaskAsync(db, task.Id, "downloading", progress, null);
                    }
                });

                if (timedOut)
                {
                    await UpdateTaskAsync(db, task.Id, "failed", progress, "离线下载超时");
                    return;
                }

                if (exitCode != 0)
                {
                    var msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                    await UpdateTaskAsync(db, task.Id, "failed", progress, $"离线下载失败: {msg}");
                    return;
                }

                var topEntries = Directory.GetFileSystemEntries(taskDir);
                var topFiles = topEntries.Where(p => System.IO.File.Exists(p) && !IsIgnoredP2pFile(p)).ToList();
                var topDirs = topEntries.Where(Directory.Exists).ToList();

                string basePath = taskDir;
                int? rootFolderId = null;

                if (topFiles.Count == 1 && topDirs.Count == 0)
                {
                    // 单文件：直接保存到当前目录
                }
                else
                {
                    string rootFolderName;
                    if (topFiles.Count == 0 && topDirs.Count == 1)
                    {
                        basePath = topDirs[0];
                        rootFolderName = Path.GetFileName(basePath);
                    }
                    else
                    {
                        rootFolderName = GetP2pTaskName(task.Url);
                    }

                    rootFolderName = SanitizeName(rootFolderName);
                    var uniqueRootName = await GetUniqueNameAsync(db, rootFolderName, task.UserId, task.ParentId);
                    var rootFolder = new StorageItem
                    {
                        Name = uniqueRootName,
                        ParentId = task.ParentId,
                        UserId = task.UserId,
                        IsFolder = true,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        IsDeleted = false
                    };
                    rootFolderId = await db.Insertable(rootFolder).ExecuteReturnIdentityAsync();
                }

                var files = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
                    .Where(f => !IsIgnoredP2pFile(f))
                    .ToList();

                if (files.Count == 0)
                {
                    await UpdateTaskAsync(db, task.Id, "failed", progress, "未找到下载文件");
                    return;
                }

                long totalSize = files.Sum(f => new FileInfo(f).Length);
                if (totalSize > 0 && user.UsedSpace + totalSize > user.TotalSpace)
                {
                    await UpdateTaskAsync(db, task.Id, "failed", progress, "存储空间不足");
                    return;
                }

                await UpdateTaskAsync(db, task.Id, "importing", 85, null);

                int? rootParentId = rootFolderId ?? task.ParentId;
                foreach (var filePath in files)
                {
                    var relativePath = Path.GetRelativePath(basePath, filePath);
                    var relativeDir = Path.GetDirectoryName(relativePath);
                    var parentId = rootParentId;

                    if (!string.IsNullOrEmpty(relativeDir) && relativeDir != ".")
                    {
                        parentId = await GetOrCreateFolderPathAsync(db, relativeDir, rootParentId, task.UserId);
                    }

                    var fileName = SanitizeName(Path.GetFileName(filePath));
                    var uniqueName = await GetUniqueNameAsync(db, fileName, task.UserId, parentId);

                    using var stream = System.IO.File.OpenRead(filePath);
                    var storedPath = await storageService.SaveFileAsync(stream, uniqueName);

                    var fileInfo = new FileInfo(filePath);
                    var storageItem = new StorageItem
                    {
                        Name = uniqueName,
                        ParentId = parentId,
                        UserId = task.UserId,
                        IsFolder = false,
                        FileSize = fileInfo.Length,
                        FilePath = storedPath,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    };

                    await db.Insertable(storageItem).ExecuteCommandAsync();
                }

                if (totalSize > 0)
                {
                    await db.Updateable<UserInfo>()
                        .SetColumns(u => u.UsedSpace == u.UsedSpace + totalSize)
                        .Where(u => u.Id == task.UserId)
                        .ExecuteCommandAsync();
                }

                await auditService.LogAsync(task.UserId, user.UserName, "离线下载", $"URL: {task.Url}", "");
                await UpdateTaskAsync(db, task.Id, "completed", 100, null);
            }
            finally
            {
                try { if (Directory.Exists(taskDir)) Directory.Delete(taskDir, true); } catch { }
            }
        }

        private static bool IsP2pUrl(string url)
        {
            var lower = url.Trim().ToLowerInvariant();
            return lower.StartsWith("magnet:")
                || lower.StartsWith("ed2k://")
                || lower.EndsWith(".torrent")
                || lower.Contains("xt=urn:btih:");
        }

        private static bool IsIgnoredP2pFile(string filePath)
        {
            return filePath.EndsWith(".aria2", StringComparison.OrdinalIgnoreCase)
                || filePath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveAria2Path(IConfiguration config, IWebHostEnvironment env)
        {
            var configured = config["Aria2:Path"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                if (File.Exists(configured))
                {
                    var fileName = Path.GetFileName(configured);
                    if (fileName.Equals("aria2.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var siblingAria2c = Path.Combine(Path.GetDirectoryName(configured) ?? "", "aria2c.exe");
                        if (File.Exists(siblingAria2c))
                        {
                            return siblingAria2c;
                        }
                    }
                    return configured;
                }
            }

            var bundledAria2c = Path.Combine(env.ContentRootPath, "Data", "Aria2", "aria2c.exe");
            if (File.Exists(bundledAria2c))
            {
                return bundledAria2c;
            }

            return "aria2c";
        }

        private static async Task<(int ExitCode, string StdOut, string StdErr, bool TimedOut)> RunAria2Async(
            string aria2Path,
            string url,
            string downloadDir,
            TimeSpan timeout,
            Func<int, Task> progressCallback)
        {
            var psi = new ProcessStartInfo
            {
                FileName = aria2Path,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("--dir");
            psi.ArgumentList.Add(downloadDir);
            psi.ArgumentList.Add("--seed-time=0");
            psi.ArgumentList.Add("--summary-interval=1");
            psi.ArgumentList.Add("--auto-file-renaming=false");
            psi.ArgumentList.Add("--allow-overwrite=true");
            psi.ArgumentList.Add("--check-certificate=false");
            psi.ArgumentList.Add(url);

            using var process = new Process { StartInfo = psi };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += async (_, e) =>
            {
                if (e.Data == null) return;
                if (stdout.Length < 4000) stdout.AppendLine(e.Data);

                var percent = TryParsePercent(e.Data);
                if (percent.HasValue)
                {
                    await progressCallback(Math.Clamp(percent.Value, 1, 80));
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null && stderr.Length < 4000)
                {
                    stderr.AppendLine(e.Data);
                }
            };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                return (-1, "", $"无法启动 aria2c: {ex.Message}", false);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
                return (process.ExitCode, stdout.ToString(), stderr.ToString(), false);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                return (-1, stdout.ToString(), "aria2c 运行超时", true);
            }
        }

        private static int? TryParsePercent(string line)
        {
            // aria2 output line often contains "(xx%)"
            var start = line.IndexOf('%');
            if (start <= 0) return null;
            var i = start - 1;
            while (i >= 0 && char.IsDigit(line[i])) i--;
            var num = line[(i + 1)..start];
            if (int.TryParse(num, out var percent))
            {
                return percent;
            }
            return null;
        }

        private static string GetP2pTaskName(string url)
        {
            var lower = url.Trim().ToLowerInvariant();

            if (lower.StartsWith("magnet:"))
            {
                try
                {
                    var uri = new Uri(url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var dn = query.Get("dn");
                    if (!string.IsNullOrWhiteSpace(dn))
                    {
                        return dn;
                    }
                }
                catch { }
            }

            if (lower.StartsWith("ed2k://"))
            {
                try
                {
                    var parts = url.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3 && parts[1] == "file")
                    {
                        return Uri.UnescapeDataString(parts[2]);
                    }
                }
                catch { }
            }

            if (lower.EndsWith(".torrent"))
            {
                try
                {
                    return Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
                }
                catch { }
            }

            return $"offline_task_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "offline_task";

            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "offline_task" : sanitized;
        }

        private static async Task<string> GetUniqueNameAsync(ISqlSugarClient db, string name, int userId, int? parentId = null)
        {
            var existingNames = await db.Queryable<StorageItem>()
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

        private static async Task<int?> GetOrCreateFolderPathAsync(ISqlSugarClient db, string folderPath, int? parentId, int userId)
        {
            if (string.IsNullOrEmpty(folderPath)) return parentId;

            var folders = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var currentParentId = parentId;

            foreach (var folderName in folders)
            {
                var name = folderName.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var query = db.Queryable<StorageItem>()
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
                    currentParentId = await db.Insertable(newFolder).ExecuteReturnIdentityAsync();
                }
            }

            return currentParentId;
        }

        private static async Task UpdateTaskAsync(ISqlSugarClient db, int taskId, string status, int progress, string? message)
        {
            await db.Updateable<OfflineDownloadTask>()
                .SetColumns(t => t.Status == status)
                .SetColumns(t => t.Progress == progress)
                .SetColumns(t => t.Message == message)
                .SetColumns(t => t.UpdateTime == DateTime.Now)
                .Where(t => t.Id == taskId)
                .ExecuteCommandAsync();
        }
    }
}
