<template>
  <div 
    class="file-list-view" 
    @dragover.prevent="handleGlobalDragOver"
    @dragleave.prevent="handleGlobalDragLeave"
    @drop.prevent="handleGlobalDrop"
    :class="{ 'drag-over': isDragOver }"
  >
    <div class="action-bar">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span
              :class="['breadcrumb-link', { 'is-last': pathStack.length === 0 && !route.query.q }]"
              @click="handleBreadcrumbClick(-1)"
            >
              {{ rootName }}
            </span>
          </el-breadcrumb-item>
          <el-breadcrumb-item v-for="(item, index) in pathStack" :key="item.id">
            <span 
              :class="['breadcrumb-link', { 'is-last': index === pathStack.length - 1 }]" 
              @click="handleBreadcrumbClick(index)"
            >
              {{ item.name }}
            </span>
          </el-breadcrumb-item>
          <el-breadcrumb-item v-if="route.query.q">
            <span class="breadcrumb-link is-last">搜索: {{ route.query.q }}</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>

      <div class="search-wrapper">
        <el-input
          v-model="searchKeyword"
          placeholder="搜索您的文件..."
          :prefix-icon="Search"
          clearable
          @keyup.enter="handleSearch"
          @clear="clearSearch"
          class="search-input"
        />
      </div>

      <div class="buttons">
        <el-upload
          v-if="category === 'files'"
          :auto-upload="true"
          :show-file-list="false"
          :http-request="handleUpload"
          multiple
          class="upload-btn"
        >
          <el-button type="primary" class="pan-button-primary" :icon="Upload">上传文件</el-button>
        </el-upload>

        <div v-if="category === 'files'" class="upload-btn">
          <input
            type="file"
            ref="folderInputRef"
            style="display: none"
            webkitdirectory
            multiple
            @change="handleFolderSelect"
          />
          <el-button type="success" plain :icon="Folder" @click="folderInputRef?.click()">上传文件夹</el-button>
        </div>
        <el-button v-if="category === 'files'" type="warning" plain :icon="FolderAdd" @click="showCreateFolder = true">新建文件夹</el-button>
        <el-button v-if="selectedIds.length > 0 && category === 'files'" :icon="Rank" @click="handleBatchMove">批量移动</el-button>
        <el-button
          v-if="selectedIds.length > 0"
          type="danger"
          plain
          :icon="Delete"
          @click="handleBatchDelete"
        >
          {{ props.category === 'favorites' ? '批量取消收藏' : '批量删除' }}
        </el-button>
        <el-button
          v-if="selectedIds.length > 0 && category === 'recycle-bin'"
          type="success"
          plain
          :icon="RefreshLeft"
          @click="handleBatchRestore"
        >
          批量还原
        </el-button>
        <el-button v-if="category === 'recycle-bin'" type="danger" :icon="Delete" @click="handleEmptyRecycleBin">清空回收站</el-button>
        
        <div class="view-switch-wrapper">
          <el-tooltip :content="viewMode === 'list' ? '切换到缩略图模式' : '切换到列表模式'" placement="top">
            <el-button link :icon="viewMode === 'list' ? Grid : Menu" @click="toggleViewMode" class="view-switch-btn" />
          </el-tooltip>
        </div>
      </div>
    </div>

    <div class="table-container pan-card" v-show="viewMode === 'list'">
      <el-table 
        :data="fileList" 
        style="width: 100%" 
        :height="enablePagination ? 'calc(100% - 52px)' : '100%'"
        @selection-change="handleSelectionChange"
        @sort-change="handleSortChange"
        v-loading="loading"
        row-key="id"
      >
        <el-table-column type="selection" width="55" />
        
        <el-table-column label="名称" prop="name" sortable="custom">
          <template #default="{ row }">
            <div 
              class="file-name-cell" 
              @click="handleRowClick(row)"
              draggable="true"
              @dragstart="handleDragStart($event, row)"
              @dragover="handleDragOver($event, row)"
              @dragleave="handleDragLeave($event)"
              @drop="handleDrop($event, row)"
            >
              <div v-if="!row.isFolder && isImage(row.name)" class="image-preview-wrapper" @click.stop="handlePreviewImage(row)">
                <el-image 
                  :src="getThumbnailUrl(row.id)" 
                  fit="cover"
                  class="thumbnail"
                />
              </div>
              <el-icon v-else :size="24" :class="row.isFolder ? 'folder-icon' : 'file-icon'">
                <FolderOpened v-if="row.isFolder" />
                <component :is="getFileIcon(row.name)" v-else />
              </el-icon>
              <span class="name">{{ row.name }}</span>
              <el-tooltip content="已分享" placement="top" v-if="row.isShared">
                <el-icon class="share-status-icon"><Share /></el-icon>
              </el-tooltip>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="大小" width="120" prop="size" sortable="custom">
          <template #default="{ row }">
            <span class="mono" v-if="!row.isFolder">{{ formatSize(row.fileSize) }}</span>
            <span v-else>-</span>
          </template>
        </el-table-column>

        <el-table-column v-if="category !== 'favorites' && category !== 'recycle-bin'" label="创建时间" width="200" prop="createTime" sortable="custom">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.createTime) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="修改时间" width="200" prop="time" sortable="custom">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.updateTime) }}</span>
          </template>
        </el-table-column>

        <el-table-column v-if="category === 'recycle-bin'" label="删除时间" width="200">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.deleteTime) }}</span>
          </template>
        </el-table-column>

        <el-table-column v-if="category === 'recycle-bin'" label="剩余天数" width="110" prop="remainingDays" align="center" header-align="center">
          <template #default="{ row }">
            <span class="mono">{{ row.remainingDays ?? 30 }} 天</span>
          </template>
        </el-table-column>

        <el-table-column v-if="category === 'favorites'" label="收藏时间" width="200" prop="favoriteTime" sortable="custom">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.favoriteTime) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="操作" width="180" fixed="right">
          <template #header>
            <div class="operation-header">
              <span>操作</span>
            </div>
          </template>
          <template #default="{ row }">
            <div class="row-actions" v-if="category !== 'recycle-bin'">
              <el-tooltip content="收藏" placement="top">
                <el-button 
                  link 
                  :type="row.isFavorite ? 'warning' : ''" 
                  :icon="row.isFavorite ? StarFilled : Star"
                  @click="handleToggleFavorite(row)"
                />
              </el-tooltip>
              <el-tooltip content="下载" placement="top" v-if="!row.isFolder">
                <el-button link :icon="Download" @click="handleDownload(row)" />
              </el-tooltip>
              <el-tooltip content="更多" placement="top">
                <el-dropdown trigger="click">
                  <el-button link :icon="More" />
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item :icon="Edit" @click="handleRename(row)">重命名</el-dropdown-item>
                      <el-dropdown-item :icon="Rank" @click="handleMove(row)">移动到</el-dropdown-item>
                      <el-dropdown-item :icon="Share" @click="handleShare(row)">分享</el-dropdown-item>
                      <el-dropdown-item divided type="danger" :icon="Delete" @click="handleDelete(row)">删除</el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
              </el-tooltip>
            </div>
            <div class="row-actions" v-else>
              <el-tooltip content="还原" placement="top">
                <el-button link :icon="RefreshLeft" @click="handleRestore(row)" />
              </el-tooltip>
              <el-tooltip content="彻底删除" placement="top">
                <el-button link type="danger" :icon="Delete" @click="handlePermanentDelete(row)" />
              </el-tooltip>
            </div>
          </template>
        </el-table-column>
      </el-table>
      <div v-if="enablePagination" class="table-pagination">
        <el-pagination
          v-model:current-page="pageNum"
          v-model:page-size="pageSize"
          :page-sizes="[20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="handlePageChange"
          @size-change="handlePageSizeChange"
        />
      </div>
    </div>

    <!-- 缩略图模式 -->
    <div class="grid-container pan-card" v-show="viewMode === 'grid'">
      <div class="grid-view" v-loading="loading">
        <div class="empty-tip" v-if="fileList.length === 0">
          <el-empty description="暂无文件" />
        </div>
        <div 
          v-for="file in fileList" 
          :key="file.id" 
          class="grid-item"
          @click="handleRowClick(file)"
          draggable="true"
          @dragstart="handleDragStart($event, file)"
          @dragover="handleDragOver($event, file)"
          @dragleave="handleDragLeave($event)"
          @drop="handleDrop($event, file)"
        >
          <div class="grid-item-inner">
            <div class="grid-preview">
              <el-image 
                v-if="!file.isFolder && isImage(file.name)"
                :src="getThumbnailUrl(file.id)"
                fit="cover"
                class="grid-thumbnail"
              />
              <el-icon v-else :size="44" :class="file.isFolder ? 'folder-icon' : 'file-icon'">
                <FolderOpened v-if="file.isFolder" />
                <component :is="getFileIcon(file.name)" v-else />
              </el-icon>
            </div>
            <div class="grid-name" :title="file.name">{{ file.name }}</div>
            
            <!-- 悬浮操作栏 -->
            <div class="grid-actions" @click.stop>
               <el-button circle size="small" :icon="file.isFavorite ? StarFilled : Star" :type="file.isFavorite ? 'warning' : ''" @click="handleToggleFavorite(file)" />
               <el-button circle size="small" :icon="Download" v-if="!file.isFolder" @click="handleDownload(file)" />
               <el-dropdown trigger="click">
                  <el-button circle size="small" :icon="More" />
                  <template #dropdown>
                    <el-dropdown-menu>
                      <el-dropdown-item :icon="Edit" @click="handleRename(file)">重命名</el-dropdown-item>
                      <el-dropdown-item :icon="Rank" @click="handleMove(file)">移动到</el-dropdown-item>
                      <el-dropdown-item :icon="Share" @click="handleShare(file)">分享</el-dropdown-item>
                      <el-dropdown-item divided type="danger" :icon="Delete" @click="handleDelete(file)">删除</el-dropdown-item>
                    </el-dropdown-menu>
                  </template>
                </el-dropdown>
            </div>
          </div>
        </div>
      </div>
      <div v-if="enablePagination" class="table-pagination">
        <el-pagination
          v-model:current-page="pageNum"
          v-model:page-size="pageSize"
          :page-sizes="[20, 50, 100]"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="handlePageChange"
          @size-change="handlePageSizeChange"
        />
      </div>
    </div>

    <!-- 新建文件夹弹窗 -->
    <el-dialog v-model="showCreateFolder" title="新建文件夹" width="400px">
      <el-input v-model="newFolderName" placeholder="请输入文件夹名称" />
      <template #footer>
        <el-button :icon="Close" @click="showCreateFolder = false">取消</el-button>
        <el-button :icon="Check" type="primary" class="pan-button-primary" @click="confirmCreateFolder">确定</el-button>
      </template>
    </el-dialog>

    <!-- 移动弹窗 -->
    <el-dialog v-model="showMoveDialog" title="移动到" width="450px" class="move-dialog">
      <div class="move-dialog-content">
        <div class="move-path-nav">
          <span class="nav-item" @click="handleBreadcrumbClickInMove(-1)">全部文件</span>
          <template v-for="(item, index) in moveDialogPathStack" :key="item.id">
            <el-icon class="nav-separator"><ArrowRight /></el-icon>
            <span class="nav-item" @click="handleBreadcrumbClickInMove(index)">{{ item.name }}</span>
          </template>
        </div>

        <div class="folder-selector">
          <div 
            v-if="moveDialogCurrentParentId !== null"
            class="folder-item back-item"
            @click="handleBackInMove"
          >
            <div class="folder-info">
              <el-icon><RefreshLeft /></el-icon>
              <span class="folder-name">返回上一级</span>
            </div>
          </div>

          <div 
            class="folder-item root-item" 
            v-if="moveDialogCurrentParentId === null"
            :class="{ active: moveTargetParentId === null }"
            @click="moveTargetParentId = null"
          >
            <div class="folder-info">
              <el-icon><Folder /></el-icon>
              <span class="folder-name">根目录</span>
            </div>
          </div>

          <div 
            v-for="folder in folderTree" 
            :key="folder.id" 
            class="folder-item"
            @click="handleEnterFolderInMove(folder)"
          >
            <div class="folder-info">
              <el-icon><Folder /></el-icon>
              <span class="folder-name">{{ folder.name }}</span>
            </div>
            <div class="enter-icon">
              <el-icon title="进入子目录"><ArrowRight /></el-icon>
            </div>
          </div>

          <div v-if="folderTree.length === 0 && moveDialogCurrentParentId !== null" class="empty-folder">
            暂无子文件夹
          </div>
        </div>
      </div>
      <template #footer>
        <div class="move-footer">
          <span class="selected-target">
            目标位置: <span class="path-text">{{ currentMovePathText }}</span>
          </span>
          <div class="footer-btns">
            <el-button :icon="Close" @click="showMoveDialog = false">取消</el-button>
            <el-button :icon="Rank" type="primary" class="pan-button-primary" @click="confirmMove">移动到此处</el-button>
          </div>
        </div>
      </template>
    </el-dialog>

    <!-- 分享设置弹窗 -->
    <el-dialog v-model="showShareSettingsDialog" title="设置分享有效期" width="420px" append-to-body>
      <el-form label-position="top" class="share-setting-form">
        <el-form-item label="有效期">
          <el-radio-group v-model="shareExpireMode" class="expire-options">
            <el-radio-button label="1d">1天</el-radio-button>
            <el-radio-button label="7d">7天</el-radio-button>
            <el-radio-button label="30d">30天</el-radio-button>
            <el-radio-button label="never">永久</el-radio-button>
            <el-radio-button label="custom">自定义</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="shareExpireMode === 'custom'" label="过期时间">
          <el-date-picker
            v-model="shareCustomExpireTime"
            type="datetime"
            placeholder="请选择过期时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 100%"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="Close" @click="showShareSettingsDialog = false">取消</el-button>
          <el-button :icon="Share" type="primary" class="pan-button-primary" @click="confirmShareCreate">确定分享</el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 分享成功弹窗 -->
    <el-dialog v-model="showShareDialog" title="分享成功" width="450px">
      <div class="share-result" v-if="shareInfo">
        <el-result icon="success" title="文件已成功分享" sub-title="请复制下方链接和提取码发送给好友">
          <template #extra>
            <div class="share-details">
              <div class="detail-item">
                <span class="label">分享链接</span>
                <el-input :value="shareUrl" readonly>
                  <template #append>
                    <el-button :icon="CopyDocument" @click="copyText(shareUrl)">复制</el-button>
                  </template>
                </el-input>
              </div>
              <div class="detail-item">
                <span class="label">提取码</span>
                <el-input :value="shareInfo.shareCode" readonly>
                  <template #append>
                    <el-button :icon="CopyDocument" @click="copyText(shareInfo.shareCode)">复制</el-button>
                  </template>
                </el-input>
              </div>
              <div class="expire-tip" v-if="shareInfo.expireTime">
                有效期至 <span class="mono">{{ formatDate(shareInfo.expireTime) }}</span>
              </div>
            </div>
          </template>
        </el-result>
      </div>
    </el-dialog>
    <!-- 视频播放弹窗 -->
    <el-dialog 
      v-model="showVideoPlayer" 
      :fullscreen="isPreviewFullscreen"
      width="1000px" 
      destroy-on-close
      @closed="videoUrl = ''; isPreviewFullscreen = false"
      :class="['preview-dialog', 'video-preview-dialog', { 'is-fullscreen': isPreviewFullscreen }]"
    >
      <template #header>
        <div class="dialog-header-custom">
          <span class="el-dialog__title">{{ videoName }}</span>
          <div class="header-actions">
            <el-tooltip :content="isPreviewFullscreen ? '退出全屏' : '全屏'" placement="bottom">
              <el-button link :icon="isPreviewFullscreen ? Aim : FullScreen" @click.stop="togglePreviewFullscreen" class="fullscreen-btn" />
            </el-tooltip>
          </div>
        </div>
      </template>
      <div class="video-container">
        <video 
          v-if="showVideoPlayer"
          :src="videoUrl" 
          controls 
          autoplay 
          class="video-player"
        >
          您的浏览器不支持视频播放
        </video>
      </div>
    </el-dialog>

    <!-- 文本/Log 预览弹窗 -->
    <el-dialog
      v-model="showTextPreview"
      :fullscreen="isPreviewFullscreen"
      width="1000px"
      destroy-on-close
      @closed="isPreviewFullscreen = false"
      :class="['preview-dialog', 'text-preview-dialog', { 'is-fullscreen': isPreviewFullscreen }]"
    >
      <template #header>
        <div class="dialog-header-custom">
          <span class="el-dialog__title">{{ textFileName }}</span>
          <div class="header-actions">
            <el-tooltip :content="isPreviewFullscreen ? '退出全屏' : '全屏'" placement="bottom">
              <el-button link :icon="isPreviewFullscreen ? Aim : FullScreen" @click.stop="togglePreviewFullscreen" class="fullscreen-btn" />
            </el-tooltip>
          </div>
        </div>
      </template>
      <div class="text-preview-wrapper">
        <pre class="text-preview-content">{{ textContent }}</pre>
      </div>
    </el-dialog>

    <!-- 统一文件预览弹窗 -->
    <el-dialog
      v-model="previewState.visible"
      :fullscreen="isPreviewFullscreen"
      width="1000px"
      destroy-on-close
      @closed="isPreviewFullscreen = false"
      :class="['preview-dialog', 'file-preview-dialog', { 'is-fullscreen': isPreviewFullscreen }]"
    >
      <template #header>
        <div class="dialog-header-custom">
          <span class="el-dialog__title">{{ previewState.fileName }}</span>
          <div class="header-actions">
            <el-tooltip :content="isPreviewFullscreen ? '退出全屏' : '全屏'" placement="bottom">
              <el-button link :icon="isPreviewFullscreen ? Aim : FullScreen" @click.stop="togglePreviewFullscreen" class="fullscreen-btn" />
            </el-tooltip>
          </div>
        </div>
      </template>
      <FilePreview
        v-if="previewState.visible"
        :file-id="previewState.fileId"
        :file-name="previewState.fileName"
        :file-type="previewState.fileType"
      />
    </el-dialog>
    <!-- 上传任务列表 -->
    <div class="upload-task-panel" v-if="uploadTasks.length > 0">
      <div class="panel-header">
        <span>上传中 ({{ activeUploadCount }}/{{ uploadTasks.length }})</span>
        <div class="task-actions">
          <el-button link :icon="Close" @click="clearFinishedTasks" />
        </div>
      </div>
      <div class="task-list">
        <div v-for="task in uploadTasks" :key="task.guid" class="task-item">
          <div class="task-info">
            <span class="task-name" :title="task.name">{{ task.name }}</span>
            <div class="task-status-row">
              <span class="task-status">{{ task.statusText }}</span>
              <el-button
                v-if="task.status === 'error'"
                link
                size="small"
                :icon="RefreshRight"
                @click="handleRetryTask(task)"
              >
                重试
              </el-button>
            </div>
          </div>
          <el-progress :percentage="task.progress" :status="task.progressStatus" :stroke-width="4" />
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed, defineAsyncComponent } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { 
  Upload, FolderAdd, FolderOpened, Document, 
  Star, StarFilled, Download, More, Edit, Rank, Share, Delete, Folder, RefreshLeft,
  Menu, Grid, Picture, VideoPlay, Notebook, Box, Headset, RefreshRight,
  ArrowRight, FullScreen, Aim, Close, Search, Check, CopyDocument
} from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'
import SparkMD5 from 'spark-md5'

const FilePreview = defineAsyncComponent(() => import('../components/FilePreview/FilePreview.vue'))

const props = defineProps<{
  category?: string
  type?: string
}>()

const route = useRoute()
const router = useRouter()
const loading = ref(true)
const fileList = ref<any[]>([])
type UploadTask = {
  guid: string
  name: string
  progress: number
  status: 'uploading' | 'success' | 'error'
  statusText: string
  progressStatus: '' | 'success' | 'exception' | 'warning'
  file?: File
  parentId?: number | null
  folderPath?: string
}

const uploadTasks = ref<UploadTask[]>([])
const activeUploadCount = computed(() => uploadTasks.value.filter(t => t.status === 'uploading').length)
const pathStack = ref<{ id: number; name: string }[]>(history.state?.pathStack || [])
const selectedIds = ref<number[]>([])
const currentParentId = ref<number | null>(route.query.folderId ? Number(route.query.folderId) : null)
const sortState = ref<{ prop: string, order: string | null } | null>(null)
const pageNum = ref(1)
const pageSize = ref(20)
const total = ref(0)
const enablePagination = computed(() => props.category === 'files' || props.category === 'recycle-bin')
const viewMode = ref<'list' | 'grid'>(localStorage.getItem('viewMode') as 'list' | 'grid' || 'list')

const currentMovePathText = computed(() => {
  if (moveDialogPathStack.value.length === 0) return '全部文件'
  return moveDialogPathStack.value.map(p => p.name).join(' / ')
})

const folderTree = ref<any[]>([])
const showShareDialog = ref(false)
const showShareSettingsDialog = ref(false)
const shareInfo = ref<any>(null)
const shareUrl = computed(() => shareInfo.value ? `${window.location.origin}/share/${shareInfo.value.shareToken}` : '')
const shareExpireMode = ref<'1d' | '7d' | '30d' | 'never' | 'custom'>('7d')
const shareCustomExpireTime = ref('')
const sharingRow = ref<any>(null)

const showCreateFolder = ref(false)
const newFolderName = ref('')
// offline download moved to OfflineDownloads page
const showMoveDialog = ref(false)
const folderInputRef = ref<HTMLInputElement>()

const searchKeyword = ref((route.query.q as string) || '')

// 并发上传控制
const uploadQueue = ref<any[]>([])
const CONCURRENCY_LIMIT = 3
const FILE_CHUNK_SIZE = 8 * 1024 * 1024
const FILE_MD5_CHUNK_SIZE = 10 * 1024 * 1024
const CHUNK_MAX_RETRIES = 3
const MERGE_MAX_RETRIES = 2
const RETRY_BASE_DELAY_MS = 800
const UPLOAD_SESSION_PREFIX = 'pan.upload.session.v1:'
let runningCount = 0

const processQueue = async () => {
  if (runningCount >= CONCURRENCY_LIMIT || uploadQueue.value.length === 0) return
  
  runningCount++
  const { file, parentId } = uploadQueue.value.shift()
  
  try {
    await handleUpload({ file, parentId })
  } catch (err) {
    console.error('任务执行失败:', err)
  } finally {
    runningCount--
    processQueue()
  }
}

const addToUploadQueue = (file: File, parentId: number | null) => {
  uploadQueue.value.push({ file, parentId })
  processQueue()
}

const handleFolderSelect = async (e: any) => {
  const files = e.target.files
  if (!files || files.length === 0) return
  
  // 锁定当前目录ID
  const targetParentId = currentParentId.value
  
  // 1. 收集所有文件夹路径（包括空文件夹无法被检测，但我们会从文件路径中提取）
  const folderPaths = new Set<string>()
  for (let i = 0; i < files.length; i++) {
    const file = files[i]
    if (file.webkitRelativePath) {
      const pathParts = file.webkitRelativePath.split(/[/\\]/)
      if (pathParts.length > 1) {
        // 添加所有层级的文件夹路径
        let currentPath = ''
        for (let j = 0; j < pathParts.length - 1; j++) {
          currentPath = currentPath ? `${currentPath}/${pathParts[j]}` : pathParts[j]
          folderPaths.add(currentPath)
        }
      }
    }
  }
  
  // 2. 先批量创建文件夹结构
  if (folderPaths.size > 0) {
    try {
      await request.post('/file/batch-create-folders', {
        folderPaths: Array.from(folderPaths),
        parentId: targetParentId
      })
    } catch (error) {
      console.error('创建文件夹结构失败:', error)
      ElMessage.error('创建文件夹结构失败')
      e.target.value = ''
      return
    }
  }
  
  // 3. 逐个上传文件
  for (let i = 0; i < files.length; i++) {
    addToUploadQueue(files[i], targetParentId)
  }
  
  // 清洗 input，允许重复上传同一文件夹
  e.target.value = ''
}
const moveTargetParentId = ref<number | null>(null)
const moveDialogCurrentParentId = ref<number | null>(null)
const moveDialogPathStack = ref<{ id: number; name: string }[]>([])
const movingItemIds = ref<number[]>([])

// 拖拽上传相关
const isDragOver = ref(false)

const handleGlobalDragOver = () => {
  if (props.category !== 'files') return
  isDragOver.value = true
}

const handleGlobalDragLeave = () => {
  isDragOver.value = false
}

// 递归遍历目录，包括空文件夹
const traverseDirectory = async (entry: any, path: string = ''): Promise<{ files: File[], folders: string[] }> => {
  const result: { files: File[], folders: string[] } = { files: [], folders: [] }
  
  if (entry.isFile) {
    const file = await new Promise<File>((resolve, reject) => {
      entry.file((f: File) => {
        // 创建一个新的 File 对象，附加 webkitRelativePath
        // 注意：新 File 对象在某些浏览器中可能丢失 webkitRelativePath，所以我们通过 Object.defineProperty 注入
        const newFile = new File([f], f.name, { type: f.type, lastModified: f.lastModified })
        const relativePath = path ? `${path}/${f.name}` : f.name
        Object.defineProperty(newFile, 'webkitRelativePath', {
          value: relativePath,
          writable: false,
          configurable: true
        })
        resolve(newFile)
      }, reject)
    })
    result.files.push(file)
  } else if (entry.isDirectory) {
    const fullPath = path ? `${path}/${entry.name}` : entry.name
    result.folders.push(fullPath)
    
    const reader = entry.createReader()
    
    const readAllEntries = async () => {
      const entries: any[] = []
      let batch: any[]
      do {
        batch = await new Promise<any[]>((resolve, reject) => {
          reader.readEntries(resolve, reject)
        })
        entries.push(...batch)
      } while (batch.length > 0)
      return entries
    }

    try {
      const entries = await readAllEntries()
      for (const childEntry of entries) {
        const childResult = await traverseDirectory(childEntry, fullPath)
        result.files.push(...childResult.files)
        result.folders.push(...childResult.folders)
      }
    } catch (err) {
      console.error('读取目录失败:', err)
    }
  }
  
  return result
}

const handleGlobalDrop = async (e: DragEvent) => {
  isDragOver.value = false
  if (props.category !== 'files') return
  
  // 重要：必须在 drop 事件的同步周期内获取文件/条目，否则 DataTransfer 对象会被清除
  const items = e.dataTransfer?.items
  if (!items) return
  
  const entries: any[] = []
  for (let i = 0; i < items.length; i++) {
    // @ts-ignore
    const entry = items[i].webkitGetAsEntry?.()
    if (entry) entries.push(entry)
  }

  if (entries.length === 0) return

  const targetParentId = currentParentId.value
  const allFolders: string[] = []
  const allFiles: File[] = []
  
  // 处理所有条目
  for (const entry of entries) {
    const result = await traverseDirectory(entry)
    allFolders.push(...result.folders)
    allFiles.push(...result.files)
  }
  
  // 1. 先批量创建所有文件夹（包括空文件夹）
  if (allFolders.length > 0) {
    try {
      await request.post('/file/batch-create-folders', {
        folderPaths: allFolders,
        parentId: targetParentId
      })
      ElMessage.success(`成功创建 ${allFolders.length} 个文件夹`)
    } catch (error) {
      console.error('创建文件夹结构失败:', error)
      ElMessage.error('创建文件夹结构失败')
      return
    }
  }
  
  // 2. 上传所有文件
  for (const file of allFiles) {
    addToUploadQueue(file, targetParentId)
  }
  
  // 如果只有空文件夹，刷新列表
  if (allFiles.length === 0 && allFolders.length > 0) {
    fetchFiles()
  }
}

const rootName = computed(() => {
  if (props.category === 'favorites') return '我的收藏'
  if (props.category === 'recycle-bin') return '回收站'
  return '全部文件'
})

// 拖拽相关
const draggingItem = ref<any>(null)

const handleDragStart = (event: DragEvent, row: any) => {
  draggingItem.value = row
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    // 设置拖拽时显示的文字或图标，这里简单设置
    event.dataTransfer.setData('text/plain', row.id.toString())
  }
}

const handleDragOver = (event: DragEvent, row: any) => {
  // 只有拖拽到文件夹上才允许放置，且不能拖拽到自己身上
  if (row.isFolder && draggingItem.value && draggingItem.value.id !== row.id) {
    event.preventDefault() // 允许 drop
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move'
    }
    // 可以添加高亮样式
    const target = event.currentTarget as HTMLElement
    target.classList.add('drag-over')
  }
}

const handleDrop = async (event: DragEvent, row: any) => {
  // 移除高亮
  const target = event.currentTarget as HTMLElement
  target.classList.remove('drag-over')
  
  if (!draggingItem.value || !row.isFolder || draggingItem.value.id === row.id) return
  
  // 执行移动操作
  try {
    await request.post('/file/move', {
      ids: [draggingItem.value.id],
      targetParentId: row.id
    })
    ElMessage.success(`已移动到 ${row.name}`)
    fetchFiles()
  } catch (error) {
    console.error(error)
  } finally {
    draggingItem.value = null
  }
}

// 添加 dragleave 处理以移除高亮样式 (可选，为了体验更细腻)
const handleDragLeave = (event: DragEvent) => {
    const target = event.currentTarget as HTMLElement
    target.classList.remove('drag-over')
}

const showVideoPlayer = ref(false)
const videoUrl = ref('')
const videoName = ref('')

const showTextPreview = ref(false)
const textContent = ref('')
const textFileName = ref('')

const isPreviewFullscreen = ref(false)
const togglePreviewFullscreen = (e: Event) => {
  e.stopPropagation()
  isPreviewFullscreen.value = !isPreviewFullscreen.value
}

const previewState = ref({
  visible: false,
  fileId: 0,
  fileName: '',
  fileType: 'unknown' as 'doc' | 'docx' | 'pdf' | 'excel' | 'image' | 'ppt' | 'markdown' | 'unknown'
})

const toggleViewMode = () => {
  viewMode.value = viewMode.value === 'list' ? 'grid' : 'list'
  localStorage.setItem('viewMode', viewMode.value)
  // 清空选中状态，避免模式切换导致的问题
  selectedIds.value = []
}

const handleRename = (row: any) => {
  ElMessageBox.prompt('请输入新名称', '重命名', {
    inputValue: row.name,
    confirmButtonText: '确定',
    cancelButtonText: '取消',
  }).then(async ({ value }) => {
    if (!value) return
    try {
      await request.put('/file/rename', { id: row.id, newName: value })
      ElMessage.success('重命名成功')
      fetchFiles()
    } catch (error: any) {
      console.error(error)
      if (error.response && error.response.data) {
        ElMessage.error(error.response.data)
      } else {
        ElMessage.error('重命名失败')
      }
    }
  })
}

const fetchFolderTree = async (parentId: number | null = null) => {
  try {
    const res: any = await request.get('/file/list', { params: { parentId } })
    const list = Array.isArray(res?.items) ? res.items : (Array.isArray(res) ? res : [])
    folderTree.value = list.filter((item: any) => item.isFolder && !movingItemIds.value.includes(item.id))
    moveDialogCurrentParentId.value = parentId
    moveTargetParentId.value = parentId // 目标位置跟随当前进入的目录
  } catch (error) {
    console.error(error)
  }
}

const handleEnterFolderInMove = (folder: any) => {
  moveDialogPathStack.value.push({ id: folder.id, name: folder.name })
  fetchFolderTree(folder.id)
}

const handleBreadcrumbClickInMove = (index: number) => {
  if (index === -1) {
    moveDialogPathStack.value = []
    fetchFolderTree(null)
  } else {
    // Slice first, then get the target item (which is now the last item)
    // Actually, handling should be: trim stack to index, then fetch that folder.
    // The original code was: slice(0, index+1), then fetch stack[index].id.
    // But slice returns a new array. moveDialogPathStack.value assignment happens first.
    // So moveDialogPathStack.value[index] refers to the new array's last item.
    const newStack = moveDialogPathStack.value.slice(0, index + 1)
    moveDialogPathStack.value = newStack
    const targetItem = newStack[index]
    if (targetItem) {
      fetchFolderTree(targetItem.id)
    }
  }
}

const handleBackInMove = () => {
  moveDialogPathStack.value.pop()
  const lastItem = moveDialogPathStack.value[moveDialogPathStack.value.length - 1]
  const parentId = moveDialogPathStack.value.length > 0 && lastItem
    ? lastItem.id 
    : null
  fetchFolderTree(parentId)
}

const handleMove = async (row: any) => {
  movingItemIds.value = [row.id]
  moveTargetParentId.value = currentParentId.value
  moveDialogCurrentParentId.value = null
  moveDialogPathStack.value = []
  showMoveDialog.value = true
  fetchFolderTree(null)
}

const handleBatchMove = () => {
  movingItemIds.value = selectedIds.value
  moveTargetParentId.value = currentParentId.value
  moveDialogCurrentParentId.value = null
  moveDialogPathStack.value = []
  showMoveDialog.value = true
  fetchFolderTree(null)
}

const confirmMove = async () => {
  try {
    await request.post('/file/move', {
      ids: movingItemIds.value,
      targetParentId: moveTargetParentId.value
    })
    ElMessage.success('移动成功')
    showMoveDialog.value = false
    fetchFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleShare = (row: any) => {
  sharingRow.value = row
  shareExpireMode.value = '7d'
  shareCustomExpireTime.value = ''
  showShareSettingsDialog.value = true
}

const confirmShareCreate = async () => {
  if (!sharingRow.value) return

  const payload: any = {
    storageItemId: sharingRow.value.id,
    expireDays: 0
  }

  if (shareExpireMode.value === '1d') payload.expireDays = 1
  if (shareExpireMode.value === '7d') payload.expireDays = 7
  if (shareExpireMode.value === '30d') payload.expireDays = 30
  if (shareExpireMode.value === 'custom') {
    if (!shareCustomExpireTime.value) {
      ElMessage.warning('请选择过期时间')
      return
    }
    const expireDate = new Date(shareCustomExpireTime.value)
    if (Number.isNaN(expireDate.getTime()) || expireDate.getTime() <= Date.now()) {
      ElMessage.warning('过期时间必须晚于当前时间')
      return
    }
    payload.expireTime = shareCustomExpireTime.value
  }

  try {
    const res: any = await request.post('/share/create', payload)
    shareInfo.value = res
    showShareSettingsDialog.value = false
    showShareDialog.value = true
    sharingRow.value.isShared = true
  } catch (error) {
    console.error(error)
  }
}

const copyText = (text: string) => {
  navigator.clipboard.writeText(text)
  ElMessage.success('已复制到剪贴板')
}

let fetchTimer: any = null
const fetchFiles = async () => {
  // 简单的防抖处理，避免批量上传时高频刷新
  if (fetchTimer) clearTimeout(fetchTimer)
  fetchTimer = setTimeout(async () => {
    loading.value = true
    try {
      let url = '/file/list'
      const params: any = {}
      
      if (route.query.q) {
        url = '/file/search'
        params.keyword = route.query.q
      } else if (props.category === 'favorites') {
        url = '/file/favorites'
      } else if (props.category === 'recycle-bin') {
        url = '/file/recycle-bin'
      } else {
        url = '/file/list'
        params.parentId = currentParentId.value
        if (props.type) {
          params.category = props.type
        }
      }

      // 添加排序参数
      if (sortState.value && sortState.value.order) {
        params.sortBy = sortState.value.prop
        params.order = sortState.value.order === 'ascending' ? 'asc' : 'desc'
      }

      if (enablePagination.value) {
        params.page = pageNum.value
        params.pageSize = pageSize.value
      }

      const res: any = await request.get(url, { params })
      if (enablePagination.value) {
        const items = Array.isArray(res?.items) ? res.items : []
        total.value = Number(res?.total ?? 0)
        fileList.value = applyClientSort(items)
      } else {
        const items = Array.isArray(res) ? res : []
        total.value = items.length
        fileList.value = applyClientSort(items)
      }
    } catch (error) {
      console.error(error)
    } finally {
      loading.value = false
    }
  }, 300)
}

watch(() => route.query.t, () => {
  // 监听时间戳变化，重置为根目录
  if (route.path === '/files' && !route.query.q) {
    // pathStack.value = []
    // currentParentId.value = null
    // fetchFiles()
    // t 变化通常伴随 folderId 消失，由 folderId watcher 处理
  }
})

watch(() => route.query.folderId, (newId) => {
  if (props.category === 'recycle-bin') return
  
  const id = newId ? Number(newId) : null
  currentParentId.value = id
  
  if (history.state && history.state.pathStack) {
     pathStack.value = history.state.pathStack
  } else if (!id) {
     pathStack.value = []
  }
  
  pageNum.value = 1
  fetchFiles()
})

const handlePreview = (row: any, type: string) => {
  previewState.value = {
    visible: true,
    fileId: row.id,
    fileName: row.name,
    fileType: type as any
  }
}

const handlePreviewImage = (row: any) => handlePreview(row, 'image')

const handleRowClick = (row: any) => {
  if (row.isFolder) {
    // 回收站中的文件夹不可进入
    if (props.category === 'recycle-bin') return
    
    // Construct new path stack
    const newPathStack = [...pathStack.value, { id: row.id, name: row.name }]
    
    router.push({
      path: route.path,
      query: { ...route.query, folderId: row.id },
      state: { pathStack: JSON.parse(JSON.stringify(newPathStack)) }
    })
  } else if (isImage(row.name)) {
    handlePreviewImage(row)
  } else if (isVideo(row.name)) {
    handlePlayVideo(row)
  } else if (isMarkdown(row.name)) {
    handlePreview(row, 'markdown')
  } else if (isText(row.name)) {
    handlePreviewText(row)
  } else if (isPdf(row.name)) {
    handlePreviewPdf(row)
  } else if (isDoc(row.name)) {
    handlePreview(row, 'doc')
  } else if (isDocx(row.name)) {
    handlePreviewDocx(row)
  } else if (isExcel(row.name)) {
    handlePreviewExcel(row)
  } else if (isPpt(row.name)) {
    handlePreview(row, 'ppt')
  }
}

const handlePlayVideo = (row: any) => {
  videoName.value = row.name
  videoUrl.value = getDownloadUrl(row.id)
  showVideoPlayer.value = true
}

const handlePreviewText = async (row: any) => {
  textFileName.value = row.name
  try {
    const res = await request.get(`/file/download/${row.id}`, { 
      params: { preview: true },
      responseType: 'text' 
    })
    textContent.value = res as unknown as string
    showTextPreview.value = true
  } catch (error) {
    console.error(error)
    ElMessage.error('预览失败')
  }
}

const handlePreviewPdf = (row: any) => handlePreview(row, 'pdf')

const handlePreviewDocx = (row: any) => handlePreview(row, 'docx')

const handlePreviewExcel = (row: any) => handlePreview(row, 'excel')

const handleBreadcrumbClick = (index: number) => {
  const nextQuery = { ...route.query }
  delete nextQuery.q
  if (index === -1) {
    // Root
    searchKeyword.value = ''
    router.push({
      path: route.path,
      query: { ...nextQuery, folderId: undefined },
      state: { pathStack: [] }
    })
  } else if (index < pathStack.value.length - 1) {
    const targetItem = pathStack.value[index]
    const newPathStack = pathStack.value.slice(0, index + 1)
    
    if (targetItem) {
      searchKeyword.value = ''
      router.push({
        path: route.path,
        query: { ...nextQuery, folderId: targetItem.id },
        state: { pathStack: JSON.parse(JSON.stringify(newPathStack)) }
      })
    }
  }
}

const applyClientSort = (list: any[]) => {
  if (!sortState.value || !sortState.value.order) {
    return list
  }

  const direction = sortState.value.order === 'ascending' ? 1 : -1
  const prop = sortState.value.prop

  const getSortValue = (item: any) => {
    if (prop === 'name') return item.name ?? ''
    if (prop === 'size') return item.fileSize ?? -1
    if (prop === 'time') return item.updateTime ? new Date(item.updateTime).getTime() : 0
    if (prop === 'createTime') return item.createTime ? new Date(item.createTime).getTime() : 0
    if (prop === 'favoriteTime') return item.favoriteTime ? new Date(item.favoriteTime).getTime() : 0
    return item[prop]
  }

  return [...list].sort((a, b) => {
    const av = getSortValue(a)
    const bv = getSortValue(b)

    if (typeof av === 'string' || typeof bv === 'string') {
      const result = String(av).localeCompare(String(bv), 'zh-CN')
      if (result !== 0) return result * direction
    } else {
      if (av < bv) return -1 * direction
      if (av > bv) return 1 * direction
    }

    // 次级排序，避免相同值顺序抖动
    return (a.id - b.id) * direction
  })
}

const generateGuid = () => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0
    const v = c === 'x' ? r : (r & 0x3 | 0x8)
    return v.toString(16)
  })
}

const sleep = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

const withRetry = async <T>(
  fn: () => Promise<T>,
  retries: number,
  onRetry?: (attempt: number, error: any) => void
): Promise<T> => {
  let lastError: any
  for (let attempt = 0; attempt <= retries; attempt++) {
    try {
      return await fn()
    } catch (error) {
      lastError = error
      if (attempt >= retries) break
      onRetry?.(attempt + 1, error)
      await sleep(RETRY_BASE_DELAY_MS * (attempt + 1))
    }
  }
  throw lastError
}

const getUploadSessionKey = (fingerprint: string) => `${UPLOAD_SESSION_PREFIX}${fingerprint}`

const saveUploadSession = (fingerprint: string, guid: string) => {
  localStorage.setItem(getUploadSessionKey(fingerprint), JSON.stringify({
    guid,
    updatedAt: Date.now()
  }))
}

const loadUploadSessionGuid = (fingerprint: string): string | null => {
  const raw = localStorage.getItem(getUploadSessionKey(fingerprint))
  if (!raw) return null
  try {
    const parsed = JSON.parse(raw)
    if (parsed?.guid && typeof parsed.guid === 'string') return parsed.guid
  } catch {
    localStorage.removeItem(getUploadSessionKey(fingerprint))
  }
  return null
}

const clearUploadSession = (fingerprint: string) => {
  localStorage.removeItem(getUploadSessionKey(fingerprint))
}

const buildUploadFingerprint = (
  md5: string,
  file: File,
  parentId: number | null,
  folderPath: string
) => {
  return [md5, file.size, file.name, parentId ?? 'root', folderPath || ''].join('|')
}

const calcUploadProgress = (uploadedBytes: number, totalBytes: number, merged: boolean) => {
  if (totalBytes <= 0) return merged ? 100 : 95
  const md5Weight = 10
  const uploadWeight = 85
  const mergeWeight = 5
  const uploadRatio = Math.min(1, uploadedBytes / totalBytes)
  const progress = md5Weight + uploadRatio * uploadWeight + (merged ? mergeWeight : 0)
  return Math.max(0, Math.min(100, Math.floor(progress)))
}

const calculateMd5 = (file: File, onProgress?: (ratio: number) => void): Promise<string> => {
  return new Promise((resolve, reject) => {
    const chunks = Math.ceil(file.size / FILE_MD5_CHUNK_SIZE)
    let currentChunk = 0
    const spark = new SparkMD5.ArrayBuffer()
    const fileReader = new FileReader()

    fileReader.onload = (e: any) => {
      spark.append(e.target.result)
      currentChunk++
      onProgress?.(Math.min(1, currentChunk / Math.max(1, chunks)))
      if (currentChunk < chunks) {
        loadNext()
      } else {
        resolve(spark.end())
      }
    }

    fileReader.onerror = () => {
      reject(new Error('MD5 计算失败'))
    }

    const loadNext = () => {
      const start = currentChunk * FILE_MD5_CHUNK_SIZE
      const end = Math.min(start + FILE_MD5_CHUNK_SIZE, file.size)
      fileReader.readAsArrayBuffer(file.slice(start, end))
    }

    loadNext()
  })
}

const getUploadedChunkSet = async (guid: string, chunksCount: number): Promise<Set<number>> => {
  try {
    const statusRes: any = await request.post('/file/upload-status', { guid }, {
      // @ts-ignore
      _showError: false
    })
    const uploaded = Array.isArray(statusRes?.uploadedChunks) ? statusRes.uploadedChunks : []
    return new Set<number>(uploaded.filter((n: any) => Number.isInteger(n) && n >= 0 && n < chunksCount))
  } catch {
    return new Set<number>()
  }
}

const runUpload = async (
  file: File,
  targetParentId: number | null,
  folderPath: string,
  task: UploadTask
) => {
  const fileSize = file.size
  const fileName = file.name

  try {
    // 1. 计算 MD5
    task.status = 'uploading'
    task.progressStatus = ''
    task.statusText = '正在计算 MD5...'
    const md5 = await calculateMd5(file, ratio => {
      task.progress = Math.floor(Math.min(1, ratio) * 10)
    })
    task.statusText = '正在校验秒传...'

    const fingerprint = buildUploadFingerprint(md5, file, targetParentId, folderPath)
    const guid = loadUploadSessionGuid(fingerprint) || generateGuid()
    task.guid = guid
    saveUploadSession(fingerprint, guid)

    // 2. 秒传校验
    try {
      const checkRes: any = await request.post('/file/check-md5', {
        md5,
        fileName,
        fileSize,
        parentId: targetParentId,
        folderPath
      }, { 
        // @ts-ignore
        _showError: false 
      })
      if (checkRes) {
        task.progress = 100
        task.status = 'success'
        task.statusText = '秒传成功'
        task.progressStatus = 'success'
        clearUploadSession(fingerprint)
        fetchFiles()
        return
      }
    } catch (e: any) {
      if (e.response && e.response.status !== 404) {
        throw e
      }
    }

    // 3. 分片上传（支持断点续传）
    const chunksCount = Math.ceil(fileSize / FILE_CHUNK_SIZE)
    if (chunksCount > 0) {
      task.statusText = '正在查询断点状态...'
      const uploadedChunkSet = await getUploadedChunkSet(guid, chunksCount)

      let committedBytes = Array.from(uploadedChunkSet).reduce((sum, index) => {
        const start = index * FILE_CHUNK_SIZE
        const end = Math.min(start + FILE_CHUNK_SIZE, fileSize)
        return sum + (end - start)
      }, 0)

      task.progress = Math.max(task.progress, calcUploadProgress(committedBytes, fileSize, false))
      task.statusText = uploadedChunkSet.size > 0
        ? `断点续传中，已完成 ${uploadedChunkSet.size}/${chunksCount} 片`
        : '正在上传分片...'

      for (let i = 0; i < chunksCount; i++) {
        if (uploadedChunkSet.has(i)) continue

        const start = i * FILE_CHUNK_SIZE
        const end = Math.min(start + FILE_CHUNK_SIZE, fileSize)
        const chunk = file.slice(start, end)
        const chunkBytes = end - start

        await withRetry(async () => {
          const chunkFormData = new FormData()
          chunkFormData.append('file', chunk)
          chunkFormData.append('guid', guid)
          chunkFormData.append('chunkIndex', i.toString())

          await request.post('/file/upload-chunk', chunkFormData, {
            timeout: 120000,
            onUploadProgress: (evt: any) => {
              const loaded = Math.min(evt?.loaded || 0, chunkBytes)
              task.progress = Math.max(task.progress, calcUploadProgress(committedBytes + loaded, fileSize, false))
            }
          })
        }, CHUNK_MAX_RETRIES, (attempt) => {
          task.statusText = `分片 ${i + 1}/${chunksCount} 上传失败，重试 ${attempt}/${CHUNK_MAX_RETRIES}`
          task.progressStatus = 'warning'
        })

        committedBytes += chunkBytes
        uploadedChunkSet.add(i)
        task.progressStatus = ''
        task.statusText = `正在上传分片 ${uploadedChunkSet.size}/${chunksCount}`
        task.progress = Math.max(task.progress, calcUploadProgress(committedBytes, fileSize, false))
      }
    } else {
      task.progress = 95
    }

    // 4. 合并分片
    task.statusText = '正在合并文件...'
    task.progress = Math.max(task.progress, 95)
    await withRetry(async () => {
      await request.post('/file/merge-chunks', {
        guid,
        fileName,
        totalSize: fileSize,
        parentId: targetParentId,
        md5,
        folderPath
      }, { timeout: 180000 })
    }, MERGE_MAX_RETRIES, (attempt) => {
      task.statusText = `合并失败，重试 ${attempt}/${MERGE_MAX_RETRIES}`
      task.progressStatus = 'warning'
    })

    task.progress = 100
    task.status = 'success'
    task.statusText = '上传成功'
    task.progressStatus = 'success'
    clearUploadSession(fingerprint)
    fetchFiles()
  } catch (error: any) {
    console.error(error)
    task.status = 'error'
    task.statusText = `${error.response?.data || error.message || '上传失败'}（可重试）`
    task.progressStatus = 'exception'
    const msg = error.response?.data || error.message || '上传失败'
    ElMessage.error(msg)
  }
}

const handleUpload = async (options: any) => {
  const file = options.file as File
  const targetParentId = options.parentId !== undefined ? options.parentId : currentParentId.value

  let folderPath = ''
  if (file.webkitRelativePath) {
    const pathParts = file.webkitRelativePath.split(/[/\\]/)
    if (pathParts.length > 1) {
      folderPath = pathParts.slice(0, -1).join('/')
    }
  }

  const task: UploadTask = options.existingTask || {
    guid: generateGuid(),
    name: folderPath ? `${folderPath}/${file.name}` : file.name,
    progress: 0,
    status: 'uploading',
    statusText: '准备上传...',
    progressStatus: '',
    file,
    parentId: targetParentId,
    folderPath
  }

  task.file = file
  task.parentId = targetParentId
  task.folderPath = folderPath

  if (!options.existingTask) {
    uploadTasks.value.unshift(task)
  }

  await runUpload(file, targetParentId, folderPath, task)
}

const handleRetryTask = async (task: UploadTask) => {
  if (!task.file) {
    ElMessage.warning('源文件已不可用，请重新选择文件上传')
    return
  }
  task.status = 'uploading'
  task.progressStatus = ''
  await runUpload(task.file, task.parentId ?? null, task.folderPath || '', task)
}

const clearFinishedTasks = () => {
  uploadTasks.value = uploadTasks.value.filter(t => t.status === 'uploading')
}

const confirmCreateFolder = async () => {
  if (!newFolderName.value) return
  try {
    await request.post('/file/folder', {
      name: newFolderName.value,
      parentId: currentParentId.value
    })
    ElMessage.success('创建成功')
    showCreateFolder.value = false
    newFolderName.value = ''
    fetchFiles()
  } catch (error: any) {
    // 错误已由全局拦截器处理
    console.error(error)
  }
}

const handleSearch = () => {
  if (searchKeyword.value.trim()) {
    router.push({ query: { ...route.query, q: searchKeyword.value.trim(), folderId: undefined } })
  } else {
    clearSearch()
  }
}

const clearSearch = () => {
  searchKeyword.value = ''
  const query = { ...route.query }
  delete query.q
  router.push({ query })
}

const handleToggleFavorite = async (row: any) => {
  try {
    await request.post(`/file/favorite/${row.id}`)
    row.isFavorite = !row.isFavorite
    ElMessage.success(row.isFavorite ? '已收藏' : '已取消收藏')
  } catch (error) {
    console.error(error)
  }
}

const handleRestore = async (row: any) => {
  try {
    await request.post(`/file/restore/${row.id}`)
    ElMessage.success('还原成功')
    fetchFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleBatchRestore = () => {
  ElMessageBox.confirm(
    `确定要还原选中的 ${selectedIds.value.length} 个项目吗？`,
    '提示',
    { type: 'warning' }
  ).then(async () => {
    try {
      await request.post('/file/batch-restore', { ids: selectedIds.value })
      ElMessage.success('批量还原成功')
      fetchFiles()
      selectedIds.value = []
    } catch (error) {
      console.error(error)
    }
  })
}

const handleEmptyRecycleBin = () => {
  ElMessageBox.confirm(
    '确定要清空回收站吗？此操作不可撤销！',
    '提示',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(async () => {
    try {
      await request.post('/file/empty-recycle-bin')
      ElMessage.success('回收站已清空')
      fetchFiles()
    } catch (error) {
      console.error(error)
      ElMessage.error('操作失败')
    }
  })
}

const handlePermanentDelete = (row: any) => {
  ElMessageBox.confirm('确定要彻底删除吗？此操作不可恢复！', '警告', { 
    type: 'error',
    confirmButtonText: '确定彻底删除',
    confirmButtonClass: 'el-button--danger'
  }).then(async () => {
    try {
      await request.delete(`/file/permanent/${row.id}`)
      ElMessage.success('已彻底删除')
      fetchFiles()
    } catch (error) {
      console.error(error)
    }
  })
}

const isImage = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'heic', 'heif'].includes(ext || '')
}

const isVideo = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['mp4', 'webm', 'ogg', 'mov'].includes(ext || '')
}

const isText = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['txt', 'json', 'xml', 'css', 'js', 'html', 'log', 'ini', 'conf'].includes(ext || '')
}

const isMarkdown = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ext === 'md' || ext === 'markdown'
}

const isPdf = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ext === 'pdf'
}

const isDocx = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ext === 'docx'
}

const isDoc = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ext === 'doc'
}

const isExcel = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['xls', 'xlsx', 'csv'].includes(ext || '')
}

const isPpt = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ['ppt', 'pptx'].includes(ext || '')
}

const getFileIcon = (name: string) => {
  if (isImage(name)) return Picture
  if (isVideo(name)) return VideoPlay
  if (isPdf(name)) return Document
  if (isDoc(name)) return Document
  if (isDocx(name)) return Document
  if (isExcel(name)) return Grid
  if (isMarkdown(name) || isText(name)) return Notebook
  
  const ext = name.split('.').pop()?.toLowerCase()
  if (['mp3', 'wav', 'flac'].includes(ext || '')) return Headset
  if (['zip', 'rar', '7z', 'tar', 'gz'].includes(ext || '')) return Box
  
  return Document
}

const getThumbnailUrl = (id: number) => {
  const token = localStorage.getItem('token')
  const baseURL = request.defaults.baseURL || '/api'
  return `${baseURL}/file/thumbnail/${id}?access_token=${token}`
}

const getDownloadUrl = (id: number) => {
  const token = localStorage.getItem('token')
  const baseURL = request.defaults.baseURL || '/api'
  return `${baseURL}/file/download/${id}?access_token=${token}`
}

const handleDownload = async (row: any) => {
  try {
    const res: any = await request.get(`/file/download/${row.id}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([res]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', row.name)
    document.body.appendChild(link)
    link.click()
  } catch (error) {
    console.error(error)
  }
}

const handleDelete = (row: any) => {
  if (props.category === 'favorites') {
    ElMessageBox.confirm('确定要取消收藏吗？', '提示', { type: 'warning' }).then(async () => {
      try {
        await request.post(`/file/favorite/${row.id}`)
        ElMessage.success('已取消收藏')
        fetchFiles()
      } catch (error) {
        console.error(error)
      }
    })
    return
  }

  ElMessageBox.confirm('确定要删除吗？', '提示', { type: 'warning' }).then(async () => {
    try {
      await request.delete(`/file/${row.id}`)
      ElMessage.success('删除成功')
      fetchFiles()
    } catch (error) {
      console.error(error)
    }
  })
}

const handleBatchDelete = () => {
  const isRecycleBin = props.category === 'recycle-bin'
  const isFavorites = props.category === 'favorites'
  const message = isRecycleBin 
    ? `确定要彻底删除选中的 ${selectedIds.value.length} 个项目吗？此操作不可恢复！`
    : isFavorites
      ? `确定要取消收藏选中的 ${selectedIds.value.length} 个项目吗？`
      : `确定要删除选中的 ${selectedIds.value.length} 个项目吗？`

  ElMessageBox.confirm(message, '提示', { 
    type: isRecycleBin ? 'error' : 'warning',
    confirmButtonClass: isRecycleBin ? 'el-button--danger' : ''
  }).then(async () => {
    try {
      const url = isRecycleBin
        ? '/file/batch-delete-permanent'
        : isFavorites
          ? '/file/batch-unfavorite'
          : '/file/batch-delete'
      await request.post(url, { ids: selectedIds.value })
      ElMessage.success(
        isRecycleBin ? '批量彻底删除成功' : isFavorites ? '批量取消收藏成功' : '批量删除成功'
      )
      fetchFiles()
      selectedIds.value = [] // clear selection
    } catch (error) {
      console.error(error)
    }
  })
}

const handleSelectionChange = (val: any[]) => {
  selectedIds.value = val.map(item => item.id)
}

const handleSortChange = ({ prop, order }: { prop: string, order: string | null }) => {
  sortState.value = { prop, order }
  pageNum.value = 1
  fetchFiles()
}

const handlePageChange = (page: number) => {
  pageNum.value = page
  fetchFiles()
}

const handlePageSizeChange = (size: number) => {
  pageSize.value = size
  pageNum.value = 1
  fetchFiles()
}

const formatSize = (bytes: number) => {
  if (!bytes) return '-'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString()
}

watch(() => route.query.q, () => {
  pageNum.value = 1
  fetchFiles()
})
watch(() => props.type, () => {
  pageNum.value = 1
  fetchFiles()
})
watch(() => props.category, () => {
  pageNum.value = 1
  fetchFiles()
})

onMounted(() => {
  pageNum.value = 1
  fetchFiles()
})
</script>

<style scoped lang="scss">
.file-list-view {
  height: 100%;
  display: flex;
  flex-direction: column;
  position: relative;
  background-color: var(--pan-bg);

  &.drag-over::after {
    content: '释放以开始上传';
    position: absolute;
    inset: 0;
    background: rgba(16, 185, 129, 0.15);
    border: 2px dashed var(--pan-primary);
    margin: 8px;
    border-radius: var(--pan-radius-lg);
    backdrop-filter: blur(4px);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--pan-primary);
    font-size: 20px;
    font-weight: 700;
    z-index: 9999;
    pointer-events: none;
  }
}

.action-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 60px;
  padding: 12px 0;
  margin-bottom: 12px;
  flex-shrink: 0;
  gap: 16px;

  .breadcrumb {
    flex: 1;
    min-width: 0;
    .breadcrumb-link {
      color: var(--pan-text-body);
      cursor: pointer;
      transition: var(--pan-transition);
      font-size: 15px;
      &:hover { color: var(--pan-primary); }
      &.is-last { color: var(--pan-text-body); font-weight: 400; cursor: default; }
    }
  }

  .search-wrapper {
    flex: 1;
    max-width: 280px;
    margin: 0 16px;
  }

  .buttons {
    display: flex;
    align-items: center;
    gap: 8px;
    
    .view-switch-wrapper {
      margin-left: 8px;
      padding-left: 12px;
      border-left: 1px solid var(--pan-border);
    }
  }
}

.table-container {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background: var(--pan-surface-elevated);
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-lg);
  padding: 8px;
  overflow: hidden;
}

.table-pagination {
  position: sticky;
  bottom: 0;
  z-index: 4;
  margin-top: 8px;
  padding: 8px 12px 6px;
  display: flex;
  justify-content: flex-end;
  border-top: 1px solid var(--pan-border);
  background: var(--pan-surface-elevated);
}

.file-name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  
  .name {
    font-weight: 400;
    color: var(--pan-text-body);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .folder-icon { color: #facc15; }
  .file-icon { color: var(--pan-primary); }

  .image-preview-wrapper {
    width: 28px;
    height: 28px;
    border-radius: 4px;
    overflow: hidden;
    border: 1px solid var(--pan-border-strong);
    .thumbnail { width: 100%; height: 100%; object-fit: cover; }
  }

  .share-status-icon {
    font-size: 14px;
    color: var(--pan-primary);
  }
}

.row-actions {
  display: flex;
  visibility: hidden;
  gap: 4px;
}

:deep(.el-table__row:hover) {
  .row-actions { visibility: visible; }
}

.grid-container {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background: var(--pan-surface-elevated);
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-lg);
  padding: 8px;
  overflow: hidden;
}

.grid-view {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 8px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  column-gap: 16px;
  row-gap: 10px;
  align-content: start;

  .grid-item {
    .grid-item-inner {
      aspect-ratio: 1;
      padding: 12px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      border-radius: var(--pan-radius-md);
      background: var(--pan-bg);
      border: 1px solid var(--pan-border-strong);
      transition: var(--pan-transition);
      position: relative;
      cursor: pointer;

      &:hover {
        border-color: var(--pan-primary);
        background: var(--pan-primary-dim);
        transform: translateY(-3px);
        .grid-actions { opacity: 1; }
      }
    }

    .grid-preview {
      width: 48px;
      height: 48px;
      margin-bottom: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      .folder-icon { font-size: 40px; color: #facc15; }
      .file-icon { font-size: 40px; color: var(--pan-primary); }
      .grid-thumbnail { width: 100%; height: 100%; object-fit: cover; border-radius: 4px; }
    }

    .grid-name {
      font-size: 12px;
      font-weight: 400;
      color: var(--pan-text-body);
      text-align: center;
      width: 100%;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .grid-actions {
      position: absolute;
      top: 4px;
      right: 4px;
      opacity: 0;
      transition: var(--pan-transition);
    }
  }
}

  .folder-selector {
    max-height: 300px;
    overflow-y: auto;
    border: 1px solid var(--pan-border);
    border-radius: var(--pan-radius-md);
    margin-bottom: 20px;
    background: var(--pan-surface-card);

    .folder-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 14px;
    cursor: pointer;
    transition: var(--pan-transition);
    border-bottom: 1px solid var(--pan-border);
    color: var(--pan-text-body);

    &:last-child { border-bottom: none; }
    &:hover { background: rgba(255, 255, 255, 0.03); color: var(--pan-text-main); }
    &.active { background: var(--pan-primary-dim); color: var(--pan-primary); font-weight: 700; }
    
    .folder-info {
      display: flex;
      align-items: center;
      gap: 10px;
      min-width: 0;
      .folder-name {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    }
    .el-icon { font-size: 18px; }
    &.back-item {
      color: var(--pan-primary);
      font-weight: 600;
      background: rgba(16, 185, 129, 0.06);
    }
    &.back-item:hover {
      background: rgba(16, 185, 129, 0.12);
    }
  }
}

.move-path-nav {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  padding: 8px 12px;
  margin-bottom: 12px;
  background: var(--pan-surface-card);
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-md);

  .nav-item {
    color: var(--pan-text-body);
    font-weight: 600;
    cursor: pointer;
    &:hover { color: var(--pan-primary); }
  }

  .nav-separator {
    color: var(--pan-text-muted);
    font-size: 12px;
  }
}

.empty-folder {
  padding: 12px 14px;
  color: var(--pan-text-muted);
  font-size: 12px;
}

.move-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  .selected-target {
    font-size: 13px;
    color: var(--pan-text-muted);
    .path-text { color: var(--pan-primary); font-weight: 700; }
  }
}

.upload-task-panel {
  position: fixed;
  right: 24px;
  bottom: 24px;
  width: 320px;
  background: var(--pan-surface-elevated);
  border: 1px solid var(--pan-border-strong);
  border-radius: var(--pan-radius-lg);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.6);
  z-index: 1000;
  overflow: hidden;
  animation: slideUp 0.3s ease-out;

  @keyframes slideUp {
    from { transform: translateY(100%); opacity: 0; }
    to { transform: translateY(0); opacity: 1; }
  }

  .panel-header {
    padding: 14px 18px;
    background: rgba(255, 255, 255, 0.03);
    border-bottom: 1px solid var(--pan-border);
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-weight: 700;
    font-size: 13px;
    color: var(--pan-text-main);
  }

  .task-list {
    max-height: 320px;
    overflow-y: auto;
    padding: 8px 18px;
    
    .task-item {
      padding: 12px 0;
      border-bottom: 1px solid var(--pan-border);
      &:last-child { border-bottom: none; }

      .task-info {
        display: flex;
        justify-content: space-between;
        margin-bottom: 8px;
        .task-name { font-size: 12px; font-weight: 600; color: var(--pan-text-main); flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .task-status-row { display: flex; align-items: center; gap: 6px; margin-left: 12px; }
        .task-status { font-size: 11px; color: var(--pan-text-muted); }
      }
    }
  }
}


.dialog-header-custom {
  height: 56px;
  padding: 0 20px 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: var(--pan-bg);

  .el-dialog__title {
    font-size: 16px;
    font-weight: 700;
    color: var(--pan-text-main);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 80%;
  }

  .header-actions {
    display: flex;
    align-items: center;
    gap: 8px;

    .fullscreen-btn {
      width: 32px;
      height: 32px;
      padding: 0 !important;
      font-size: 18px;
      border: none !important;
      color: var(--pan-text-muted) !important;
      
      &:hover {
        background-color: rgba(255, 255, 255, 0.05) !important;
        color: var(--pan-primary) !important;
      }
    }
  }
}

.video-container {
  flex: 1;
  background: #000;
  display: flex;
  justify-content: center;
  align-items: center;
  .video-player { width: 100%; height: 100%; object-fit: contain; }
}

.text-preview-wrapper {
  flex: 1;
  padding: 24px;
  background: #0c0d0d;
  overflow: auto;
  display: flex;
  justify-content: center;

  .text-preview-content {
    margin: 0;
    width: 100%;
    max-width: 900px;
    color: #cbd5e1;
    font-family: var(--font-mono);
    font-size: 13px;
    line-height: 1.6;
    white-space: pre-wrap;
    word-break: break-all;
  }
}

.share-setting-form {
  .expire-options {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
  }
}

.share-result {
  :deep(.el-result) {
    padding: 8px 0 0;
  }
  :deep(.el-result__icon) {
    margin-bottom: 6px;
  }
  :deep(.el-result__title) {
    font-size: 18px;
    font-weight: 700;
    color: var(--pan-text-main);
  }
  :deep(.el-result__subtitle) {
    color: var(--pan-text-muted);
  }

  .share-details {
    margin-top: 18px;
    padding: 16px;
    background: var(--pan-surface-card);
    border: 1px solid var(--pan-border);
    border-radius: var(--pan-radius-md);
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  .detail-item {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .label {
    font-size: 12px;
    font-weight: 700;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    color: var(--pan-text-muted);
  }

  .expire-tip {
    margin-top: 4px;
    font-size: 12px;
    color: var(--pan-text-muted);
    text-align: right;
  }

  :deep(.el-input-group__append .el-button) {
    background-color: var(--pan-primary) !important;
    border-color: var(--pan-primary) !important;
    color: #000 !important;
    font-weight: 700 !important;
    height: 100% !important;
    border-radius: 0 8px 8px 0 !important;
    width: 100% !important;
    min-width: 72px !important;
    padding: 0 12px !important;
    white-space: nowrap;
  }

  :deep(.el-input-group__append .el-button:hover) {
    background-color: var(--pan-primary-light) !important;
  }

  :deep(.el-input-group) {
    align-items: stretch;
  }

  :deep(.el-input-group__append) {
    display: flex;
    align-items: stretch;
    justify-content: center;
    width: 72px !important;
    min-width: 72px !important;
    padding: 0 !important;
    margin-left: -1px !important;
    border-left: 0 !important;
    border-radius: 0 8px 8px 0 !important;
    overflow: hidden;
    background: transparent !important;
    box-shadow: none !important;
  }

  :deep(.el-input-group .el-input__wrapper) {
    border-radius: 8px 0 0 8px !important;
  }
}
</style>
