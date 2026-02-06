using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 管理员登录IP白名单规则表。
    /// </summary>
    [SugarTable("AdminLoginIpRule")]
    public class AdminLoginIpRule
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "规则文本", Length = 100)]
        public string RuleText { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "起始IP数值")]
        public long StartIp { get; set; }

        [SugarColumn(ColumnDescription = "结束IP数值")]
        public long EndIp { get; set; }

        [SugarColumn(ColumnDescription = "是否启用")]
        public bool IsEnabled { get; set; } = true;

        [SugarColumn(ColumnDescription = "备注", Length = 300, IsNullable = true)]
        public string? Remark { get; set; }

        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDescription = "修改时间")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
