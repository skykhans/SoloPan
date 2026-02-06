using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 审计日志表实体。
    /// </summary>
    [SugarTable("AuditLog")]
    public class AuditLog
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "用户ID")]
        public int UserId { get; set; }

        [SugarColumn(ColumnDescription = "用户名", Length = 50)]
        public string UserName { get; set; }

        [SugarColumn(ColumnDescription = "操作类型", Length = 100)]
        public string Action { get; set; }

        [SugarColumn(ColumnDescription = "操作详情", Length = 4000)]
        public string Detail { get; set; }

        [SugarColumn(ColumnDescription = "客户端IP", Length = 64)]
        public string IpAddress { get; set; }

        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; }
    }
}
