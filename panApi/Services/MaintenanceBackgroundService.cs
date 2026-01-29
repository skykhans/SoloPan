using SqlSugar;
using PanSystem.Models;

namespace PanSystem.Services
{
    public class MaintenanceBackgroundService : BackgroundService
    {
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

                        // 1. 清理过期的分享记录 (数据库)
                        var expiredCount = await db.Deleteable<ShareLink>()
                            .Where(s => s.ExpireTime != null && s.ExpireTime < DateTime.Now)
                            .ExecuteCommandAsync();
                        
                        if (expiredCount > 0)
                        {
                            _logger.LogInformation($"清理了 {expiredCount} 条过期的分享记录。");
                        }

                        // 2. 清理孤儿临时分片文件夹 (文件系统)
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
