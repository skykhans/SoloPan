using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 离线下载任务表实体。
    /// </summary>
    [SugarTable("OfflineDownloadTask")]
    public class OfflineDownloadTask
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "用户ID")]
        public int UserId { get; set; }

        [SugarColumn(ColumnDescription = "下载链接", Length = 2048)]
        public string Url { get; set; }

        [SugarColumn(ColumnDescription = "目标父目录ID", IsNullable = true)]
        public int? ParentId { get; set; }

        [SugarColumn(ColumnDescription = "任务状态", Length = 50)]
        public string Status { get; set; }

        [SugarColumn(ColumnDescription = "进度(0-100)")]
        public int Progress { get; set; }

        [SugarColumn(ColumnDescription = "状态消息", Length = 4000, IsNullable = true)]
        public string? Message { get; set; }

        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; }

        [SugarColumn(ColumnDescription = "更新时间")]
        public DateTime UpdateTime { get; set; }
    }
}
