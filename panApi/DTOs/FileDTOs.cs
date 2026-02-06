namespace PanSystem.DTOs
{
    /// <summary>
    /// 创建文件夹请求。
    /// </summary>
    public class CreateFolderRequest
    {
        /// <summary>
        /// 文件夹名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父目录ID，null 表示根目录。
        /// </summary>
        public int? ParentId { get; set; }
    }

    /// <summary>
    /// 重命名请求。
    /// </summary>
    public class RenameRequest
    {
        /// <summary>
        /// 目标项目ID。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 新名称。
        /// </summary>
        public string NewName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批量移动请求。
    /// </summary>
    public class MoveRequest
    {
        /// <summary>
        /// 需要移动的项目ID集合。
        /// </summary>
        public List<int> Ids { get; set; } = new();

        /// <summary>
        /// 目标父目录ID，null 表示根目录。
        /// </summary>
        public int? TargetParentId { get; set; }
    }

    /// <summary>
    /// 批量删除/恢复请求。
    /// </summary>
    public class BatchDeleteRequest
    {
        /// <summary>
        /// 项目ID集合。
        /// </summary>
        public List<int> Ids { get; set; } = new();
    }

    /// <summary>
    /// 秒传MD5校验请求。
    /// </summary>
    public class Md5CheckRequest
    {
        /// <summary>
        /// 文件MD5。
        /// </summary>
        public string Md5 { get; set; } = string.Empty;

        /// <summary>
        /// 文件名。
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小(byte)。
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 目标父目录ID。
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 文件夹路径（批量上传场景）。
        /// </summary>
        public string? FolderPath { get; set; }
    }

    /// <summary>
    /// 文件项响应对象。
    /// </summary>
    public class FileItemResponse
    {
        /// <summary>
        /// 项目ID。
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
        /// 文件大小(byte)，文件夹可为空。
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间。
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 删除时间。
        /// </summary>
        public DateTime? DeleteTime { get; set; }

        /// <summary>
        /// 收藏时间。
        /// </summary>
        public DateTime? FavoriteTime { get; set; }

        /// <summary>
        /// 回收站剩余保留天数。
        /// </summary>
        public int? RemainingDays { get; set; }

        /// <summary>
        /// 父目录ID。
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 是否已收藏。
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 是否已分享。
        /// </summary>
        public bool IsShared { get; set; }
    }

    /// <summary>
    /// 分片上传请求。
    /// </summary>
    public class ChunkUploadRequest
    {
        /// <summary>
        /// 上传任务标识。
        /// </summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// 分片索引。
        /// </summary>
        public int ChunkIndex { get; set; }
    }

    /// <summary>
    /// 合并分片请求。
    /// </summary>
    public class MergeChunksRequest
    {
        /// <summary>
        /// 上传任务标识。
        /// </summary>
        public string Guid { get; set; } = string.Empty;

        /// <summary>
        /// 文件名。
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件总大小(byte)。
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// 目标父目录ID。
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 文件MD5。
        /// </summary>
        public string Md5 { get; set; } = string.Empty;

        /// <summary>
        /// 文件夹路径（批量上传场景）。
        /// </summary>
        public string? FolderPath { get; set; }
    }

    /// <summary>
    /// 查询分片上传状态请求。
    /// </summary>
    public class ChunkStatusRequest
    {
        /// <summary>
        /// 上传任务标识。
        /// </summary>
        public string Guid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 离线下载新增请求。
    /// </summary>
    public class OfflineDownloadRequest
    {
        /// <summary>
        /// 下载链接。
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 目标父目录ID。
        /// </summary>
        public int? ParentId { get; set; }
    }

    /// <summary>
    /// 离线下载更新请求。
    /// </summary>
    public class OfflineDownloadUpdateRequest
    {
        /// <summary>
        /// 下载链接。
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 目标父目录ID。
        /// </summary>
        public int? ParentId { get; set; }
    }

    /// <summary>
    /// 批量创建文件夹请求。
    /// </summary>
    public class BatchCreateFoldersRequest
    {
        /// <summary>
        /// 需要创建的文件夹路径集合。
        /// </summary>
        public required List<string> FolderPaths { get; set; }

        /// <summary>
        /// 目标父目录ID。
        /// </summary>
        public int? ParentId { get; set; }
    }
}
