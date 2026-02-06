using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 分享链接表实体。
    /// </summary>
    [SugarTable("ShareLink")]
    public class ShareLink
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "分享创建者用户ID")]
        public int UserId { get; set; }

        [SugarColumn(ColumnDescription = "被分享的存储项ID")]
        public int StorageItemId { get; set; }

        [SugarColumn(ColumnDescription = "提取码", Length = 20)]
        public string ShareCode { get; set; } // 提取码

        [SugarColumn(ColumnDescription = "分享令牌", Length = 100)]
        public string ShareToken { get; set; } // 唯一标识

        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; }

        [SugarColumn(ColumnDescription = "过期时间", IsNullable = true)]
        public DateTime? ExpireTime { get; set; }

        [SugarColumn(ColumnDescription = "浏览次数")]
        public int ViewCount { get; set; }

        [SugarColumn(ColumnDescription = "下载次数")]
        public int DownloadCount { get; set; }
    }
}
