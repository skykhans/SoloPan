using PanSystem.Models;
using PanSystem.Utils;
using SqlSugar;

namespace PanSystem.Services
{
    public interface IAuditService
    {
        Task LogAsync(int userId, string userName, string action, string detail, string ipAddress);
    }

    public class AuditService : IAuditService
    {
        private readonly ISqlSugarClient _db;

        public AuditService(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task LogAsync(int userId, string userName, string action, string detail, string ipAddress)
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Detail = detail,
                IpAddress = IpRuleParser.NormalizeIpForDisplay(ipAddress),
                CreateTime = DateTime.Now
            };

            await _db.Insertable(log).ExecuteCommandAsync();
        }
    }
}
