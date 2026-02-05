using SqlSugar;

namespace PanSystem.Models
{
    public class OfflineDownloadTask
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Url { get; set; }
        public int? ParentId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string? Message { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
