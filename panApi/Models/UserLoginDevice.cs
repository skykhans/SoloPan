using SqlSugar;

namespace PanSystem.Models
{
    /// <summary>
    /// 用户常用登录设备表。
    /// </summary>
    [SugarTable("UserLoginDevice")]
    public class UserLoginDevice
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键ID")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "用户ID")]
        public int UserId { get; set; }

        [SugarColumn(ColumnDescription = "设备标识", Length = 128)]
        public string DeviceId { get; set; } = string.Empty;

        [SugarColumn(ColumnDescription = "设备名称", Length = 200, IsNullable = true)]
        public string? DeviceName { get; set; }

        [SugarColumn(ColumnDescription = "设备类型", Length = 40, IsNullable = true)]
        public string? DeviceType { get; set; }

        [SugarColumn(ColumnDescription = "最后一次登录IP", Length = 64, IsNullable = true)]
        public string? LastLoginIp { get; set; }

        [SugarColumn(ColumnDescription = "最后一次UserAgent", Length = 1000, IsNullable = true)]
        public string? LastUserAgent { get; set; }

        [SugarColumn(ColumnDescription = "登录次数")]
        public int LoginCount { get; set; } = 0;

        [SugarColumn(ColumnDescription = "首次登录时间")]
        public DateTime FirstLoginTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDescription = "最近登录时间")]
        public DateTime LastLoginTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnDescription = "是否常用设备")]
        public bool IsTrusted { get; set; } = true;
    }
}
