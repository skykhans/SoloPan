/*
 Navicat Premium Dump SQL

 Source Server         : localhost - SQL Server
 Source Server Type    : SQL Server
 Source Server Version : 17001050 (17.00.1050)
 Source Host           : localhost\SQLEXPRESS:1433
 Source Catalog        : PanSystem
 Source Schema         : dbo

 Target Server Type    : SQL Server
 Target Server Version : 17001050 (17.00.1050)
 File Encoding         : 65001

 Date: 02/02/2026 13:37:37
*/


-- ----------------------------
-- Table structure for ShareLink
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[ShareLink]') AND type IN ('U'))
	DROP TABLE [dbo].[ShareLink]
GO

CREATE TABLE [dbo].[ShareLink] (
  [Id] int  IDENTITY(1,1) NOT NULL,
  [UserId] int  NOT NULL,
  [StorageItemId] int  NOT NULL,
  [ShareCode] varchar(255) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [ShareToken] varchar(255) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [CreateTime] datetime  NOT NULL,
  [ExpireTime] datetime  NOT NULL,
  [ViewCount] int  NOT NULL,
  [DownloadCount] int  NOT NULL
)
GO

ALTER TABLE [dbo].[ShareLink] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of ShareLink
-- ----------------------------
SET IDENTITY_INSERT [dbo].[ShareLink] ON
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'1', N'2', N'9', N'd885', N'4606e8e7fc6a4d019dccb8dc587b3557', N'2026-01-29 14:19:00.133', N'2026-02-05 14:19:00.133', N'1', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'2', N'2', N'13', N'b4e5', N'6d5da8da15b843fab17524afbe43aa0d', N'2026-01-29 15:38:56.253', N'2026-02-05 15:38:56.253', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'3', N'2', N'13', N'4374', N'1f64644313d6492b98e3ad1a35e3c462', N'2026-01-29 15:39:06.413', N'2026-02-05 15:39:06.413', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'4', N'1', N'1', N'f878', N'8de2ddaea11d4c3581bd1c5e2d78f9a1', N'2026-01-30 09:47:16.840', N'2026-02-06 09:47:16.840', N'6', N'4')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'5', N'1', N'4', N'9f62', N'06759bbd84334067b55c1dec05b8458c', N'2026-01-30 10:09:33.913', N'2026-02-06 10:09:33.913', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'6', N'1', N'4', N'edaa', N'f17102ecaac746c592cfa4fc68063276', N'2026-01-30 10:09:40.263', N'2026-02-06 10:09:40.263', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'7', N'1', N'21', N'cf87', N'47dee62659e2480a8f05790befd3ade7', N'2026-01-30 11:16:03.370', N'2026-02-06 11:16:03.370', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'8', N'1', N'31', N'ef13', N'4081f29697644c73b7584244ba51ca65', N'2026-01-30 14:11:06.480', N'2026-02-06 14:11:06.480', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'9', N'1', N'42', N'8cc4', N'17d788c03c7547c681e3c5e65296851d', N'2026-02-02 13:30:44.097', N'2026-02-09 13:30:44.097', N'0', N'0')
GO

INSERT INTO [dbo].[ShareLink] ([Id], [UserId], [StorageItemId], [ShareCode], [ShareToken], [CreateTime], [ExpireTime], [ViewCount], [DownloadCount]) VALUES (N'10', N'1', N'42', N'a12f', N'fa0e7d2a001a45bab2280707149971d6', N'2026-02-02 13:31:07.763', N'2026-02-09 13:31:07.763', N'0', N'0')
GO

SET IDENTITY_INSERT [dbo].[ShareLink] OFF
GO


-- ----------------------------
-- Table structure for StorageItem
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[StorageItem]') AND type IN ('U'))
	DROP TABLE [dbo].[StorageItem]
GO

CREATE TABLE [dbo].[StorageItem] (
  [Id] int  IDENTITY(1,1) NOT NULL,
  [Name] varchar(255) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [ParentId] int  NULL,
  [UserId] int  NOT NULL,
  [IsFolder] bit  NOT NULL,
  [FileSize] bigint  NOT NULL,
  [FileMd5] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [FilePath] varchar(255) COLLATE Chinese_PRC_CI_AS  NULL,
  [IsDeleted] bit  NOT NULL,
  [IsFavorite] bit  NOT NULL,
  [CreateTime] datetime  NOT NULL,
  [UpdateTime] datetime  NOT NULL
)
GO

ALTER TABLE [dbo].[StorageItem] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of StorageItem
-- ----------------------------
SET IDENTITY_INSERT [dbo].[StorageItem] ON
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'1', N'Loo0ng small.png', N'26', N'1', N'0', N'966221', N'0f1634821f824909aa5e39ccfecf42cc', N'2026-01-29/9b10128b-0f80-49b4-98d1-5d0c92d294ea_Loong small.png', N'0', N'1', N'2026-01-29 13:37:36.910', N'2026-01-30 11:56:45.677')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'3', N'324324', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'1', N'2026-01-29 13:39:30.247', N'2026-01-29 14:04:37.517')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'4', N'Loong.png', N'3', N'1', N'0', N'2899647', N'8c0d09e18d180d5ea97951c127e8e2da', N'2026-01-29/321b53da-90ca-4513-9132-2de21c82c0d0_Loong.png', N'0', N'0', N'2026-01-29 13:45:21.563', N'2026-01-30 14:19:24.547')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'5', N'22222', N'3', N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-29 13:59:49.700', N'2026-01-29 13:59:49.700')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'6', N'Golden DeepSeek.png', N'5', N'1', N'0', N'3176525', N'79552ee9db55edb7e65e474a674c5f21', N'2026-01-29/6d5df776-199d-416c-8a31-1129ba3b4262_Golden DeepSeek.png', N'0', N'0', N'2026-01-29 14:10:10.493', N'2026-01-30 11:06:25.267')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'7', N'23432', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'1', N'2026-01-29 14:10:57.077', N'2026-01-30 16:22:45.017')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'8', N'133243', NULL, N'2', N'1', N'0', NULL, NULL, N'0', N'1', N'2026-01-29 14:14:47.407', N'2026-01-30 10:01:47.230')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'9', N'22222', NULL, N'2', N'1', N'0', NULL, NULL, N'0', N'1', N'2026-01-29 14:14:51.197', N'2026-01-30 10:01:47.683')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'10', N'debug.log', N'9', N'2', N'0', N'5750', N'd7677f8fabbf1dfeb5f76a7eda032909', N'2026-01-29/50931025-cefc-46b7-944c-ae991b7dd2a2_debug.log', N'0', N'0', N'2026-01-29 14:14:58.497', N'2026-01-29 14:14:58.497')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'11', N'Loong.png', N'8', N'2', N'0', N'2899647', N'8c0d09e18d180d5ea97951c127e8e2da', N'2026-01-29/321b53da-90ca-4513-9132-2de21c82c0d0_Loong.png', N'0', N'0', N'2026-01-29 14:15:01.103', N'2026-01-29 15:33:02.387')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'12', N'Loong.png', N'9', N'2', N'0', N'2899647', N'8c0d09e18d180d5ea97951c127e8e2da', N'2026-01-29/321b53da-90ca-4513-9132-2de21c82c0d0_Loong.png', N'0', N'0', N'2026-01-29 14:15:09.447', N'2026-01-29 14:15:11.633')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'13', N'a5744f251c0776245542bcd15a67ef37_raw.mp4', N'8', N'2', N'0', N'2668611', N'b99e7ac1b1392757d9a786dc231a04b4', N'2026-01-29/8d9af4f9-c22e-480a-bc1d-3e9b22f3e8bd_a5744f251c0776245542bcd15a67ef37_raw.mp4', N'0', N'0', N'2026-01-29 14:45:25.560', N'2026-01-29 14:45:25.560')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'14', N'供应商列表_1768799956343.xlsx', N'8', N'2', N'0', N'6459', N'de634eb65fcfc59c1a9b9fccbf28c58b', N'2026-01-29/db301a99-eb3d-4ff7-be93-32c7b7dc431f_供应商列表_1768799956343.xlsx', N'0', N'0', N'2026-01-29 14:46:04.287', N'2026-01-29 14:46:04.287')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'15', N'对账单 (26).docx', N'8', N'2', N'0', N'9726', N'9dcdb1930c5cc355cb28ca8794ee8f7f', N'2026-01-29/d2155f7a-a963-4e7f-a4ba-dc6332e4dfe8_对账单 (26).docx', N'0', N'0', N'2026-01-29 14:46:10.910', N'2026-01-29 14:46:10.910')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'16', N'订单明细 (51).pdf', N'8', N'2', N'0', N'71058', N'16ee010afc2e36ada3bedebb49c1e1a6', N'2026-01-29/96252193-cf0e-4e43-ae07-08974e980bf9_订单明细 (51).pdf', N'0', N'0', N'2026-01-29 15:01:44.267', N'2026-01-29 15:01:44.267')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'17', N'宝姿SRM成衣生产进度管理蓝图汇报_V1.1.pptx', N'8', N'2', N'0', N'2102535', N'fc18260a71f051e1cba249615cc810ce', N'2026-01-30/d0e58552-741e-4034-9317-638c16cc8661_宝姿SRM成衣生产进度管理蓝图汇报_V1.1.pptx', N'0', N'0', N'2026-01-30 09:17:55.967', N'2026-01-30 09:17:55.967')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'18', N'更新 供应商各状态、阶段权限设置2022331.xlsx', N'8', N'2', N'0', N'15509', N'a5772250e23f2fef0775979566e95a8e', N'2026-01-30/a5e0b3b1-a976-42b1-88fb-5f76949772f9_更新 供应商各状态、阶段权限设置2022331.xlsx', N'0', N'0', N'2026-01-30 09:30:37.587', N'2026-01-30 09:30:37.587')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'19', N'宝姿SRM退换货管理蓝图汇报_V2.0.pptx', N'8', N'2', N'0', N'747979', N'205f461eb3db79668d40764dfdc3dcf7', N'2026-01-30/d621e497-9818-41db-a22c-fc51ed6a54e1_宝姿SRM退换货管理蓝图汇报_V2.0.pptx', N'0', N'0', N'2026-01-30 09:33:45.147', N'2026-01-30 09:33:45.147')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'20', N'宝姿SRM项目_供应商管理模块_蓝图方案V1.13_成衣.pptx', N'8', N'2', N'0', N'3932971', N'db6b2bd4e532a27fe676f7c5ca13427b', N'2026-01-30/bca9129f-db7b-46cd-8e3d-90e9df2d01d5_宝姿SRM项目_供应商管理模块_蓝图方案V1.13_成衣.pptx', N'0', N'0', N'2026-01-30 09:33:49.830', N'2026-01-30 09:33:49.830')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'21', N'SQL语句格式化.html', NULL, N'1', N'0', N'9229', N'8da7bdc26127790f0a34e8939ae301fc', N'2026-01-30/2402683d-f4d8-4a69-a252-a25f0a97f00b_SQL语句格式化.html', N'0', N'0', N'2026-01-30 10:08:27.767', N'2026-01-30 10:08:27.767')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'22', N'物料调整单查询.xlsx', NULL, N'1', N'0', N'11821', N'4c61257ffde517e5e15229221069355a', N'2026-01-30/a5ab6dbf-9484-4bca-acb4-8fd2d66e5098_物料调整单查询.xlsx', N'0', N'0', N'2026-01-30 10:08:38.450', N'2026-01-30 10:08:38.450')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'23', N'验布报告.pdf', NULL, N'1', N'0', N'55367', N'85d385be052fe17d42a6366caa1b2154', N'2026-01-30/8c2e69a6-c6c2-4aeb-ba62-fe27abfc3701_验布报告.pdf', N'0', N'0', N'2026-01-30 10:08:41.197', N'2026-01-30 10:08:41.197')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'24', N'企业微信截图_17339912086955.png', NULL, N'1', N'0', N'64731', N'cb1ed3e75aa8cbf9f909ee7d2641d740', N'2026-01-30/b9c2a215-54f8-4e34-a555-f5681ad44334_企业微信截图_17339912086955.png', N'1', N'0', N'2026-01-30 10:08:51.183', N'2026-01-30 14:19:33.313')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'25', N'【定版】世纪宝姿_SRM实施项目_方案蓝图_供应商绩效_V1.4.docx', N'5', N'1', N'0', N'2666125', N'f4c3b04ad8f257ddfadbe4e131b65425', N'2026-01-30/f1b4d318-5d91-491e-b492-a2c76bda72ad_【定版】世纪宝姿_SRM实施项目_方案蓝图_供应商绩效_V1.4.docx', N'0', N'0', N'2026-01-30 10:08:59.693', N'2026-01-30 11:04:16.577')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'26', N'432432', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 11:01:01.250', N'2026-01-30 11:01:01.250')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'27', N'432143', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 13:36:45.590', N'2026-01-30 13:36:45.590')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'28', N'ggfgfds', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 13:36:51.907', N'2026-01-30 13:36:51.907')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'29', N'【定版】世纪宝姿_SRM实施项目_方案蓝图_供应商绩效_V1.4.docx', NULL, N'1', N'0', N'2666125', N'f4c3b04ad8f257ddfadbe4e131b65425', N'2026-01-30/f1b4d318-5d91-491e-b492-a2c76bda72ad_【定版】世纪宝姿_SRM实施项目_方案蓝图_供应商绩效_V1.4.docx', N'0', N'0', N'2026-01-30 13:45:22.983', N'2026-01-30 13:45:22.983')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'30', N'1961广宣申请流程及表单 - 20240820.xlsx', NULL, N'1', N'0', N'48531', N'0b3b9698f489fe7a357a2252d499127f', N'2026-01-30/d1273d93-1b67-45b4-a941-ff8415528599_1961广宣申请流程及表单 - 20240820.xlsx', N'0', N'0', N'2026-01-30 13:55:42.593', N'2026-01-30 13:55:42.593')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'31', N'2', NULL, N'1', N'1', N'0', NULL, NULL, N'1', N'0', N'2026-01-30 14:03:24.100', N'2026-01-30 14:32:19.133')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'32', N'1', NULL, N'1', N'1', N'0', NULL, NULL, N'1', N'0', N'2026-01-30 14:03:26.527', N'2026-01-30 14:32:19.133')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'33', N'2', NULL, N'1', N'1', N'0', NULL, NULL, N'1', N'0', N'2026-01-30 14:03:29.243', N'2026-01-30 14:32:19.133')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'34', N'3', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 14:03:32.737', N'2026-01-30 14:03:32.737')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'35', N'4', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 14:03:35.480', N'2026-01-30 14:03:35.480')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'36', N'2', NULL, N'1', N'1', N'0', NULL, NULL, N'1', N'0', N'2026-01-30 14:31:34.887', N'2026-01-30 14:32:19.133')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'37', N'4', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 14:31:39.173', N'2026-01-30 14:31:39.173')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'38', N'宝姿SRM成衣生产进度管理蓝图汇报_V1.1.pptx', NULL, N'1', N'0', N'2102535', N'fc18260a71f051e1cba249615cc810ce', N'2026-01-30/d0e58552-741e-4034-9317-638c16cc8661_宝姿SRM成衣生产进度管理蓝图汇报_V1.1.pptx', N'0', N'0', N'2026-01-30 14:31:43.783', N'2026-01-30 14:31:43.783')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'39', N'宝姿SRM成衣生产进度管理蓝图汇报_V1.1.pptx', NULL, N'1', N'0', N'2102535', N'fc18260a71f051e1cba249615cc810ce', N'2026-01-30/d0e58552-741e-4034-9317-638c16cc8661_宝姿SRM成衣生产进度管理蓝图汇报_V1.1.pptx', N'0', N'0', N'2026-01-30 14:31:49.700', N'2026-01-30 14:31:49.700')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'40', N'12132', NULL, N'1', N'1', N'0', NULL, NULL, N'0', N'0', N'2026-01-30 16:17:07.920', N'2026-01-30 16:17:07.920')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'41', N'DeepSeek.jpg', N'40', N'1', N'0', N'42053', N'8459765d31577e68426b185297e85bdf', N'2026-02-02/6ea65af3-28d5-4249-add0-f431b0ab92fd_DeepSeek.jpg', N'0', N'1', N'2026-02-02 13:30:18.513', N'2026-02-02 13:30:21.477')
GO

INSERT INTO [dbo].[StorageItem] ([Id], [Name], [ParentId], [UserId], [IsFolder], [FileSize], [FileMd5], [FilePath], [IsDeleted], [IsFavorite], [CreateTime], [UpdateTime]) VALUES (N'42', N'Golden DeepSeek Small.png', N'7', N'1', N'0', N'901921', N'f320430a1281c719c2163485b79c50b5', N'2026-02-02/49325e00-c814-4c84-ad4f-0f4e21304ff4_Golden DeepSeek Small.png', N'0', N'0', N'2026-02-02 13:30:34.763', N'2026-02-02 13:30:34.763')
GO

SET IDENTITY_INSERT [dbo].[StorageItem] OFF
GO


-- ----------------------------
-- Table structure for UserInfo
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[UserInfo]') AND type IN ('U'))
	DROP TABLE [dbo].[UserInfo]
GO

CREATE TABLE [dbo].[UserInfo] (
  [Id] int  IDENTITY(1,1) NOT NULL,
  [UserName] varchar(50) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [Password] varchar(100) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [Email] varchar(100) COLLATE Chinese_PRC_CI_AS  NULL,
  [TotalSpace] bigint  NOT NULL,
  [UsedSpace] bigint  NOT NULL,
  [IsAdmin] bit  NOT NULL,
  [CreateTime] datetime  NOT NULL
)
GO

ALTER TABLE [dbo].[UserInfo] SET (LOCK_ESCALATION = TABLE)
GO

EXEC sp_addextendedproperty
'MS_Description', N'用户名',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'UserName'
GO

EXEC sp_addextendedproperty
'MS_Description', N'密码',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'Password'
GO

EXEC sp_addextendedproperty
'MS_Description', N'邮箱',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'Email'
GO

EXEC sp_addextendedproperty
'MS_Description', N'总空间(byte)',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'TotalSpace'
GO

EXEC sp_addextendedproperty
'MS_Description', N'已用空间(byte)',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'UsedSpace'
GO

EXEC sp_addextendedproperty
'MS_Description', N'是否管理员',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'IsAdmin'
GO

EXEC sp_addextendedproperty
'MS_Description', N'创建时间',
'SCHEMA', N'dbo',
'TABLE', N'UserInfo',
'COLUMN', N'CreateTime'
GO


-- ----------------------------
-- Records of UserInfo
-- ----------------------------
SET IDENTITY_INSERT [dbo].[UserInfo] ON
GO

INSERT INTO [dbo].[UserInfo] ([Id], [UserName], [Password], [Email], [TotalSpace], [UsedSpace], [IsAdmin], [CreateTime]) VALUES (N'1', N'admin', N'e10adc3949ba59abbe56e057f20f883e', NULL, N'1073741824', N'17713366', N'1', N'2026-01-29 13:37:07.080')
GO

INSERT INTO [dbo].[UserInfo] ([Id], [UserName], [Password], [Email], [TotalSpace], [UsedSpace], [IsAdmin], [CreateTime]) VALUES (N'2', N'vaerand', N'e10adc3949ba59abbe56e057f20f883e', NULL, N'5368709120', N'15359892', N'0', N'2026-01-29 14:12:20.190')
GO

SET IDENTITY_INSERT [dbo].[UserInfo] OFF
GO


-- ----------------------------
-- Auto increment value for ShareLink
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[ShareLink]', RESEED, 10)
GO


-- ----------------------------
-- Primary Key structure for table ShareLink
-- ----------------------------
ALTER TABLE [dbo].[ShareLink] ADD CONSTRAINT [PK_ShareLink_Id] PRIMARY KEY CLUSTERED ([Id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for StorageItem
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[StorageItem]', RESEED, 42)
GO


-- ----------------------------
-- Primary Key structure for table StorageItem
-- ----------------------------
ALTER TABLE [dbo].[StorageItem] ADD CONSTRAINT [PK_StorageItem_Id] PRIMARY KEY CLUSTERED ([Id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for UserInfo
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[UserInfo]', RESEED, 2)
GO


-- ----------------------------
-- Primary Key structure for table UserInfo
-- ----------------------------
ALTER TABLE [dbo].[UserInfo] ADD CONSTRAINT [PK_UserInfo_Id] PRIMARY KEY CLUSTERED ([Id])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO

