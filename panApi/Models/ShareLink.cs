using SqlSugar;

namespace PanSystem.Models
{
    public class ShareLink
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int StorageItemId { get; set; }
        public string ShareCode { get; set; } // 提取码
        public string ShareToken { get; set; } // 唯一标识
        public DateTime CreateTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int ViewCount { get; set; }
        public int DownloadCount { get; set; }
    }
}
