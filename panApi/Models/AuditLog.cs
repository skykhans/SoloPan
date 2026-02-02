using SqlSugar;

namespace PanSystem.Models
{
    public class AuditLog
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
