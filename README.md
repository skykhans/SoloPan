# PanSystem 项目文档

`PanSystem` 是一个网盘系统，包含后端 `ASP.NET Core Web API` 和前端 `Vue 3 + Element Plus`，支持文件存储、分享、在线预览、回收站、离线下载、系统管理与审计。

## 1. 系统架构

- 后端：`panApi`
  - 技术：`.NET 10`、`SqlSugar`、`SQL Server`、`JWT`
  - 功能：用户、文件、分享、管理、审计、后台任务
- 前端：`panUi`
  - 技术：`Vue 3`、`TypeScript`、`Vite`、`Element Plus`
  - 功能：全部文件、收藏、分享、回收站、离线下载、系统管理页面
- 文件存储
  - 默认目录：`panApi/uploads`
  - 秒传机制：同 MD5 文件仅保留一份物理文件，多条数据库记录引用

## 2. 主要能力

- 用户与安全
  - 注册、登录、JWT 鉴权、个人中心、头像上传
  - 用户状态启用/禁用（禁用后禁止登录）
  - 登录 IP 记录、常用设备识别、设备验证流程
- 文件管理
  - 文件夹层级、上传、分片上传、秒传、移动、重命名、删除
  - 回收站软删除/还原/彻底删除
  - 回收站保留 30 天与后台自动清理
- 分享与收藏
  - 分享提取码、过期时间可配置
  - 我的分享支持分享时间/过期时间/浏览/下载排序
  - 收藏时间记录
- 预览能力
  - 图片、PDF、Word、PPT
  - Excel 文件请下载后查看
  - 文本文件预览
  - Markdown 渲染预览（`.md` / `.markdown`）
- 系统管理
  - 用户管理、审计日志、登录限制
  - 分页、排序、多字段查询
  - 批量启用/禁用、批量配额、批量上传上限

## 3. 项目目录

- `panApi/Controllers`：接口控制器
- `panApi/Models`：数据模型
- `panApi/DTOs`：请求/响应模型
- `panApi/Services`：业务服务与后台任务
- `panApi/Utils`：工具类
- `panUi/src/views`：页面
- `panUi/src/components`：组件（含文件预览组件）
- `panUi/src/utils/request.ts`：HTTP 请求封装

## 4. 环境要求

- `Node.js` 18+
- `.NET SDK` 10.x
- `SQL Server` 2019+

## 5. 配置说明

配置文件：`panApi/appsettings.json`

- `ConnectionStrings:DefaultConnection`
  - SQL Server 连接串
- `Jwt:Key`
  - JWT 签名密钥（生产环境必须更换为高强度随机值）
- `Jwt:Issuer` / `Jwt:Audience`
  - JWT 标识信息
- `Storage:UploadPath`
  - 上传根目录（默认 `uploads`）

本地开发建议使用 user-secrets 保存敏感配置：

```bash
cd panApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=PanSystem;User Id=sa;Password=your_password;TrustServerCertificate=True;Connect Timeout=30;"
dotnet user-secrets set "Jwt:Key" "your-32-chars-or-longer-dev-secret"
dotnet user-secrets set "BootstrapAdmin:Password" "123456"
```

前端 API 地址：`panUi/src/utils/request.ts`

- 当前默认：`https://localhost:7296/api`
- 后端地址变化时需要同步修改

## 6. 本地运行

### 6.1 启动后端

```bash
cd panApi
dotnet run
```

说明：
- 默认开发地址见 `panApi/Properties/launchSettings.json`
- 常用：`https://localhost:7296`，兼容 `http://localhost:5080`
- 开发环境可访问 Swagger：`/swagger`

### 6.2 启动前端

```bash
cd panUi
npm install
npm run dev
```

默认访问：`http://localhost:5173`

### 6.3 构建

```bash
cd panUi
npm run build
```

```bash
cd panApi
dotnet build
```

## 7. 生产环境部署

以下给出一套可直接落地的生产部署流程（Windows / Linux 通用思路）。

### 7.1 后端发布

```bash
cd panApi
dotnet publish PanSystem.csproj -c Release -o ./publish
```

发布目录：`panApi/publish`

### 7.2 前端构建与静态发布

```bash
cd panUi
npm install
npm run build
```

构建产物：`panUi/dist`

推荐两种部署方式：

- 方式 A：前端静态站点由 Nginx/IIS 直接托管，`/api` 反向代理到后端
- 方式 B：前端由独立静态服务托管（CDN/Nginx），后端仅提供 API

### 7.3 生产配置基线

- 后端 `appsettings.Production.json` 建议至少覆盖：
  - `ConnectionStrings:DefaultConnection`
  - `Jwt:Key`（必须替换）
  - `Storage:UploadPath`（建议绝对路径）
  - `Aria2:Path`（离线下载需要）
- 将环境变量设置为：
  - `ASPNETCORE_ENVIRONMENT=Production`

### 7.4 反向代理要点（Nginx 示例）

```nginx
server {
    listen 80;
    server_name your-domain.com;

    root /var/www/pansystem-ui;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 7.5 后端进程托管建议

- Windows：
  - 使用 IIS（ASP.NET Core Hosting Bundle）或 NSSM 注册为服务
- Linux：
  - 使用 `systemd` 托管 Kestrel 进程
- 无论平台，建议加：
  - 自动拉起
  - 标准输出日志落盘
  - 健康检查与重启策略

### 7.6 上线检查清单

1. 前端可打开并成功登录。
2. 上传、下载、分享、图片/PDF/Word/PPT/Markdown 预览正常。
3. 后端 `/swagger` 在生产建议关闭或受控访问。
4. 上传目录有写权限，数据库连接正常。
5. JWT 密钥已替换，默认管理员密码已修改。

## 8. 离线下载依赖与安装

离线下载分两类：

- HTTP/HTTPS 直链下载：无需安装额外软件
- 磁力/BT/ed2k 下载：必须安装 `aria2c`

### 8.1 必装组件（P2P 场景）

- `aria2c` 可执行文件

项目已包含可参考目录：`panApi/Data/Aria2`

### 8.2 配置方式

在后端配置中增加：

```json
{
  "Aria2": {
    "Path": "C:\\tools\\aria2\\aria2c.exe"
  }
}
```

说明：

- 若未配置 `Aria2:Path`，系统会默认尝试直接执行 `aria2c`
- 即要求 `aria2c` 在系统 `PATH` 中可被找到

### 8.3 验证命令

在部署机执行：

```bash
aria2c --version
```

若命令不可用，离线下载磁力/BT任务会失败，并在任务消息中出现“未找到 aria2c”或“无法启动 aria2c”。

### 8.4 运行注意事项

- 下载超时时间当前为 2 小时（代码内控制）
- 磁力任务会先下到临时目录，再导入网盘存储目录
- 导入前会校验用户剩余空间
- 任务执行由后台服务 `OfflineDownloadWorker` 处理，应用进程必须常驻

## 9. 数据库初始化与兼容策略

启动时由 `panApi/Program.cs` 执行：

- `CodeFirst` 建表/字段同步
- 历史版本兼容列补全（如 `UpdateTime`、`IsEnabled`、`MaxUploadFileSize` 等）
- 默认管理员初始化（不存在时创建）
- 表注释同步到 SQL Server 扩展属性（用于 Navicat 注释显示）

默认管理员：

- 用户名：`admin`
- 密码：读取 `BootstrapAdmin:Password` 配置；本地示例为 `123456`，首次登录后请立即修改

## 10. 核心接口分组

- 用户接口：`/api/user/*`
  - 注册、登录、个人信息、修改密码
- 文件接口：`/api/file/*`
  - 列表、上传、秒传、分片、下载、预览、回收站、收藏、离线下载
- 分享接口：`/api/share/*`
  - 创建分享、访问分享、下载分享、我的分享
- 管理接口：`/api/admin/*`
  - 用户管理、审计日志、登录限制

建议通过 Swagger 查看完整参数与响应结构。

## 11. 数据表说明（简版）

- `UserInfo`：用户主体信息（账户、空间、状态、登录信息）
- `StorageItem`：文件/文件夹结构与元数据
- `ShareLink`：分享记录（提取码、过期时间、浏览/下载统计）
- `AuditLog`：审计日志
- `OfflineDownloadTask`：离线下载任务
- `LoginIpRule`：普通用户登录限制规则
- `AdminLoginIpRule`：管理员登录白名单规则
- `UserLoginDevice`：用户常用登录设备记录

## 12. 备份与恢复方案（推荐）

本系统至少要备份两类数据：

- 数据库（账号、目录结构、分享、审计、任务）
- 文件目录（`UploadPath` 指向的物理文件）

### 12.1 备份策略

- 每日全量数据库备份
- 每日文件目录增量备份，至少每周一次全量
- 保留策略建议：
  - 日备份保留 14 天
  - 周备份保留 8 周
  - 月备份保留 6-12 个月
- 每月至少做一次恢复演练

### 12.2 SQL Server 备份示例

```sql
BACKUP DATABASE [PanSystem]
TO DISK = N'D:\backup\PanSystem_full.bak'
WITH INIT, COMPRESSION, STATS = 5;
```

### 12.3 文件目录备份示例（Windows）

```powershell
robocopy "C:\Test\SoloPan\panApi\uploads" "D:\backup\pan_uploads" /MIR /R:2 /W:2
```

说明：
- `/MIR` 为镜像同步，执行前请确认目标目录专用于备份。

### 12.4 恢复流程（建议）

1. 停止后端服务，冻结写入。
2. 恢复数据库到目标实例。
3. 恢复 `uploads` 目录到配置路径。
4. 启动后端，检查日志无异常。
5. 使用管理员账户验证：
   - 用户登录
   - 文件列表与下载
   - 分享访问
   - 审计日志新增

## 13. 安全与生产建议

- 当前密码存储为 `PBKDF2-SHA256`，并兼容历史 `MD5` 登录后自动迁移
- 生产环境必须更换 `Jwt:Key`
- 限制 CORS 来源，不建议 `AllowAnyOrigin`
- 启用 HTTPS 反向代理与访问日志采集
- 文件上传建议增加：
  - 病毒扫描
  - 文件类型白名单
  - 敏感后缀拦截

## 14. 常见问题排查

- 前端 401
  - 检查 token 是否过期，检查 `request.ts` 的 `baseURL` 是否正确
- 构建失败
  - 执行 `npm install` 后再 `npm run build`
- 启动报表结构错误
  - 检查 SQL Server 权限
  - 查看 `Program.cs` 中建表与兼容字段日志
- 离线下载失败
  - 检查 `Aria2:Path` 是否配置正确
  - 检查 `aria2c --version` 是否可执行
  - 检查服务器是否允许访问目标下载地址和端口
- 预览异常
  - 检查下载接口是否可访问
  - 检查 token 在预览 URL 中是否传递

## 15. 后续规划建议

- 密码算法升级与历史密码平滑迁移
- 对象存储适配（MinIO/S3）
- 预览服务拆分（文档转换与缓存）
- 更细粒度的 RBAC 权限模型
