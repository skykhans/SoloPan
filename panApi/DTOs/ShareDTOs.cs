namespace PanSystem.DTOs
{
    public class CreateShareRequest
    {
        public int StorageItemId { get; set; }
        public int ExpireDays { get; set; } // 0 为永久
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
}
