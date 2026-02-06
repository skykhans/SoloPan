namespace PanSystem.DTOs
{
    public class CreateShareRequest
    {
        public int StorageItemId { get; set; }
        public int ExpireDays { get; set; } // 0 为永久
        public DateTime? ExpireTime { get; set; } // 可选：手动指定过期时间
    }

    public class ShareResponse
    {
        public string ShareToken { get; set; }
        public string ShareCode { get; set; }
        public DateTime? ExpireTime { get; set; }
    }

    public class ShareDetailResponse
    {
        public string Name { get; set; }
        public long? FileSize { get; set; }
        public bool IsFolder { get; set; }
        public string UserName { get; set; }
        public DateTime? ExpireTime { get; set; }
    }

    public class AccessShareRequest
    {
        public string ShareToken { get; set; }
        public string ShareCode { get; set; }
    }

    public class SaveShareRequest
    {
        public string ShareToken { get; set; }
        public string ShareCode { get; set; }
        public int? TargetParentId { get; set; }
    }
    public class ShareItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public long FileSize { get; set; }
        public DateTime CreateTime { get; set; }
        public int? ParentId { get; set; }
    }

    public class BatchCancelShareRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
