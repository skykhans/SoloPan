# PanSystem - 类似百度网盘的后端系统

基于 ASP.NET Core 10, SqlSugar ORM 和 SQL Server 开发的云盘后端系统。

## 核心特性

- **用户系统**: 注册、登录、JWT 鉴权、个人资料管理、头像上传。
- **文件管理**:
  - 支持文件和文件夹的层级目录。
  - **秒传**: 基于 MD5 校验，相同文件全系统仅存一份物理文件。
  - **分片上传**: 支持大文件的切片上传与合并。
  - **基本操作**: 重命名、移动、批量删除。
  - **回收站**: 支持软删除、还原和彻底清理（释放空间）。
- **分享机制**: 生成带提取码和过期时间的分享链接，匿名访问与下载。
- **多媒体预览**: 实时生成图片缩略图。
- **系统维护**:
  - 后台自动清理过期分享。
  - 定期清理未完成的上传分片。
- **审计日志**: 记录用户关键操作（上传、删除等）及 IP 地址。

## 技术栈

- **框架**: ASP.NET Core Web API (NET 10.0)
- **ORM**: SqlSugar (支持多种数据库，本项目默认为 SQL Server)
- **数据库**: SQL Server
- **图像处理**: SixLabors.ImageSharp
- **安全**: JWT Bearer 认证, SHA256 密码加密

## 快速开始

1. **修改数据库连接**: 
   在 `appsettings.json` 中配置 `ConnectionStrings:DefaultConnection`。
2. **运行**:
   ```bash
   dotnet run
   ```
   项目会自动通过 CodeFirst 初始化数据库表。
3. **API 文档**:
   访问 `http://localhost:5000/swagger` (取决于运行端口) 查看完整接口文档。

## 项目结构

- `/Controllers`: API 控制器 (用户、文件、分享)。
- `/Models`: 数据库实体模型。
- `/Services`: 业务服务逻辑 (存储、审计、后台任务)。
- `/DTOs`: 数据传输对象。
- `/Utils`: 通用工具类 (Hash, 加密)。
- `/Data`: 数据库初始化与配置。

## 核心接口概览

- `POST /api/auth/register`: 注册
- `POST /api/auth/login`: 登录
- `GET /api/file/list`: 获取文件列表
- `POST /api/file/upload`: 文件上传
- `POST /api/file/check-md5`: 秒传校验
- `POST /api/share/create`: 创建分享
- `GET /api/user/profile`: 获取个人信息
