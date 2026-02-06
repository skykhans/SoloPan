using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 文件/文件夹存储项表实体。
    /// </summary>
    [SugarTable("StorageItem")]
    public class StorageItem
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "名称", Length = 255)]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "父目录ID", IsNullable = true)]
        public int? ParentId { get; set; }

        [SugarColumn(ColumnDescription = "所属用户ID")]
        public int UserId { get; set; }

        [SugarColumn(ColumnDescription = "是否文件夹")]
        public bool IsFolder { get; set; }

        [SugarColumn(ColumnDescription = "文件大小(byte)")]
        public long FileSize { get; set; }

        [SugarColumn(ColumnDescription = "文件MD5", Length = 64, IsNullable = true)]
        public string? FileMd5 { get; set; }

        [SugarColumn(ColumnDescription = "文件存储路径", Length = 500, IsNullable = true)]
        public string? FilePath { get; set; }

        [SugarColumn(ColumnDescription = "是否已删除")]
        public bool IsDeleted { get; set; }

        [SugarColumn(ColumnDescription = "删除时间", IsNullable = true)]
        public DateTime? DeleteTime { get; set; }

        [SugarColumn(ColumnDescription = "是否收藏")]
        public bool IsFavorite { get; set; }

        [SugarColumn(ColumnDescription = "收藏时间", IsNullable = true)]
        public DateTime? FavoriteTime { get; set; }

        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; }

        [SugarColumn(ColumnDescription = "更新时间")]
        public DateTime UpdateTime { get; set; }
    }
}
