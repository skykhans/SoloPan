namespace PanSystem.DTOs
{
    /// <summary>
    /// 创建分享请求。
    /// </summary>
    public class CreateShareRequest
    {
        /// <summary>
        /// 被分享的存储项ID。
        /// </summary>
        public int StorageItemId { get; set; }

        /// <summary>
        /// 过期天数（0表示永久）。
        /// </summary>
        public int ExpireDays { get; set; } // 0 为永久

        /// <summary>
        /// 手动指定过期时间。
        /// </summary>
        public DateTime? ExpireTime { get; set; } // 可选：手动指定过期时间
    }

    /// <summary>
    /// 分享创建响应。
    /// </summary>
    public class ShareResponse
    {
        /// <summary>
        /// 分享令牌。
        /// </summary>
        public string ShareToken { get; set; } = string.Empty;

        /// <summary>
        /// 提取码。
        /// </summary>
        public string ShareCode { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间。
        /// </summary>
        public DateTime? ExpireTime { get; set; }
    }

    /// <summary>
    /// 分享详情响应。
    /// </summary>
    public class ShareDetailResponse
    {
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小(byte)。
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// 是否文件夹。
        /// </summary>
        public bool IsFolder { get; set; }

        /// <summary>
        /// 分享者用户名。
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间。
        /// </summary>
        public DateTime? ExpireTime { get; set; }
    }

    /// <summary>
    /// 访问分享请求。
    /// </summary>
    public class AccessShareRequest
    {
        /// <summary>
        /// 分享令牌。
        /// </summary>
        public string ShareToken { get; set; } = string.Empty;

        /// <summary>
        /// 提取码。
        /// </summary>
        public string ShareCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 保存分享到个人网盘请求。
    /// </summary>
    public class SaveShareRequest
    {
        /// <summary>
        /// 分享令牌。
        /// </summary>
        public string ShareToken { get; set; } = string.Empty;

        /// <summary>
        /// 提取码。
        /// </summary>
        public string ShareCode { get; set; } = string.Empty;

        /// <summary>
        /// 保存目标父目录ID。
        /// </summary>
        public int? TargetParentId { get; set; }
    }

    /// <summary>
    /// 我的分享列表项响应。
    /// </summary>
    public class ShareItemResponse
    {
        /// <summary>
        /// 分享记录ID。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否文件夹。
        /// </summary>
        public bool IsFolder { get; set; }

        /// <summary>
        /// 文件大小(byte)。
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 分享创建时间。
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 父目录ID。
        /// </summary>
        public int? ParentId { get; set; }
    }

    /// <summary>
    /// 批量取消分享请求。
    /// </summary>
    public class BatchCancelShareRequest
    {
        /// <summary>
        /// 分享记录ID集合。
        /// </summary>
        public List<int> Ids { get; set; } = new();
    }
}
