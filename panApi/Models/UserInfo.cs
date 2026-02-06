using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 用户信息表实体。
    /// </summary>
    [SugarTable("UserInfo")]
    public class UserInfo
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "用户名", Length = 50)]
        public string UserName { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "密码", Length = 100)]
        public string Password { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "邮箱", Length = 100, IsNullable = true)]
        public string? Email { get; set; }

        [SugarColumn(ColumnDescription = "手机号", Length = 20, IsNullable = true)]
        public string? Phone { get; set; }

        [SugarColumn(ColumnDescription = "头像", Length = 500, IsNullable = true)]
        public string? Avatar { get; set; }

        [SugarColumn(ColumnDescription = "总空间(byte)")]
        public long TotalSpace { get; set; } = 1024L * 1024 * 1024 * 5; // 默认5GB

        [SugarColumn(ColumnDescription = "已用空间(byte)")]
        public long UsedSpace { get; set; } = 0;

        [SugarColumn(ColumnDescription = "最大单文件上传大小(byte)")]
        public long MaxUploadFileSize { get; set; } = 100L * 1024 * 1024; // 默认100MB

        [SugarColumn(ColumnDescription = "是否管理员")]
        public bool IsAdmin { get; set; } = false;

        [SugarColumn(ColumnDescription = "是否启用")]
        public bool IsEnabled { get; set; } = true;

        [SugarColumn(ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDescription = "修改时间")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDescription = "登录时间", IsNullable = true)]
        public DateTime? LastLoginTime { get; set; }

        [SugarColumn(ColumnDescription = "登录IP", Length = 64, IsNullable = true)]
        public string? LastLoginIp { get; set; }
    }
}
