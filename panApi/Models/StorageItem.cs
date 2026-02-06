using SqlSugar;

namespace PanSystem.Models
{
    public class StorageItem
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string Name { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? ParentId { get; set; }
        public int UserId { get; set; }
        public bool IsFolder { get; set; }
        public long FileSize { get; set; }
        [SugarColumn(IsNullable = true)]
        public string? FileMd5 { get; set; }
        [SugarColumn(IsNullable = true)]
        public string? FilePath { get; set; }
        public bool IsDeleted { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? DeleteTime { get; set; }
        public bool IsFavorite { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? FavoriteTime { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
