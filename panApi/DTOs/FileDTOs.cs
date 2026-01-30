namespace PanSystem.DTOs
{
    public class CreateFolderRequest
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
    }

    public class RenameRequest
    {
        public int Id { get; set; }
        public string NewName { get; set; }
    }

    public class MoveRequest
    {
        public List<int> Ids { get; set; }
        public int? TargetParentId { get; set; }
    }

    public class BatchDeleteRequest
    {
        public List<int> Ids { get; set; }
    }

    public class Md5CheckRequest
    {
        public string Md5 { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public int? ParentId { get; set; }
    }

    public class FileItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public long? FileSize { get; set; }
        public DateTime CreateTime { get; set; }
        public int? ParentId { get; set; }
        public bool IsFavorite { get; set; }
    }
}
