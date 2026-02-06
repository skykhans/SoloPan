using SqlSugar;
using PanSystem.Models;

namespace PanSystem.Services
{
    public class MaintenanceBackgroundService : BackgroundService
    {
        private const int RecycleRetentionDays = 30;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MaintenanceBackgroundService> _logger;
        private readonly string _tempPath;

        public MaintenanceBackgroundService(
            IServiceProvider serviceProvider, 
            ILogger<MaintenanceBackgroundService> logger,
            IWebHostEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _tempPath = Path.Combine(env.ContentRootPath, "Temp");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("维护后台服务已启动。");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("执行定时维护任务...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
                        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

                        // 1. 清理过期的分享记录 (数据库)
                        var expiredCount = await db.Deleteable<ShareLink>()
                            .Where(s => s.ExpireTime != null && s.ExpireTime < DateTime.Now)
                            .ExecuteCommandAsync();
                        
                        if (expiredCount > 0)
                        {
                            _logger.LogInformation($"清理了 {expiredCount} 条过期的分享记录。");
                        }

                        // 2. 清理回收站超期数据（超过 30 天彻底删除）
                        var expireBefore = DateTime.Now.AddDays(-RecycleRetentionDays);
                        var expiredRoots = await db.Queryable<StorageItem>()
                            .Where(i => i.IsDeleted && i.DeleteTime != null && i.DeleteTime < expireBefore)
                            .Select(i => i.Id)
                            .ToListAsync();

                        if (expiredRoots.Any())
                        {
                            var allIdsToDelete = new HashSet<int>(expiredRoots);
                            var currentLevelIds = expiredRoots;

                            while (true)
                            {
                                var childIds = await db.Queryable<StorageItem>()
                                    .Where(i => i.ParentId != null && currentLevelIds.Contains((int)i.ParentId))
                                    .Select(i => i.Id)
                                    .ToListAsync();

                                if (!childIds.Any())
                                {
                                    break;
                                }

                                var newIds = childIds.Where(id => !allIdsToDelete.Contains(id)).ToList();
                                if (!newIds.Any())
                                {
                                    break;
                                }

                                foreach (var id in newIds)
                                {
                                    allIdsToDelete.Add(id);
                                }
                                currentLevelIds = newIds;
                            }

                            var allIds = allIdsToDelete.ToList();
                            var allItems = await db.Queryable<StorageItem>()
                                .Where(i => allIds.Contains(i.Id))
                                .ToListAsync();

                            if (allItems.Any())
                            {
                                long totalFreed = 0;
                                var freedByUser = new Dictionary<int, long>();
                                var fileItems = allItems.Where(i => !i.IsFolder && !string.IsNullOrEmpty(i.FilePath)).ToList();
                                var filePaths = fileItems.Select(i => i.FilePath!).Distinct().ToList();

                                var protectedPaths = new HashSet<string>();
                                if (filePaths.Any())
                                {
                                    var referencedPaths = await db.Queryable<StorageItem>()
                                        .Where(i => i.FilePath != null && filePaths.Contains(i.FilePath) && !allIds.Contains(i.Id))
                                        .Select(i => i.FilePath)
                                        .Distinct()
                                        .ToListAsync();
                                    protectedPaths = new HashSet<string>(referencedPaths.Where(p => !string.IsNullOrEmpty(p))!);
                                }

                                var deletedPathSet = new HashSet<string>();
                                foreach (var item in fileItems)
                                {
                                    if (!protectedPaths.Contains(item.FilePath!) && !deletedPathSet.Contains(item.FilePath!))
                                    {
                                        await storageService.DeleteFileAsync(item.FilePath!);
                                        deletedPathSet.Add(item.FilePath!);
                                    }

                                    totalFreed += item.FileSize;
                                    if (!freedByUser.ContainsKey(item.UserId))
                                    {
                                        freedByUser[item.UserId] = 0;
                                    }
                                    freedByUser[item.UserId] += item.FileSize;
                                }

                                if (freedByUser.Count > 0)
                                {
                                    var userIds = freedByUser.Keys.ToList();
                                    var users = await db.Queryable<UserInfo>().Where(u => userIds.Contains(u.Id)).ToListAsync();
                                    foreach (var user in users)
                                    {
                                        var freed = freedByUser[user.Id];
                                        user.UsedSpace = Math.Max(0, user.UsedSpace - freed);
                                        user.UpdateTime = DateTime.Now;
                                        await db.Updateable(user).ExecuteCommandAsync();
                                    }
                                }

                                await db.Deleteable<ShareLink>().Where(s => allIds.Contains(s.StorageItemId)).ExecuteCommandAsync();
                                await db.Deleteable<StorageItem>().In(allIds).ExecuteCommandAsync();

                                _logger.LogInformation("回收站自动清理完成：删除 {Count} 项，释放 {Freed} bytes。", allItems.Count, totalFreed);
                            }
                        }

                        // 3. 清理孤儿临时分片文件夹 (文件系统)
                        // 清理超过 24 小时未合并的临时文件夹
                        if (Directory.Exists(_tempPath))
                        {
                            var directories = Directory.GetDirectories(_tempPath);
                            foreach (var dir in directories)
                            {
                                var dirInfo = new DirectoryInfo(dir);
                                if (dirInfo.LastWriteTime < DateTime.Now.AddHours(-24))
                                {
                                    Directory.Delete(dir, true);
                                    _logger.LogInformation($"清理了过期的临时分片目录: {dir}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "执行维护任务时发生错误。");
                }

                // 每 1 小时执行一次
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
