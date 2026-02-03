<template>
  <div class="file-list-view">
    <div class="action-bar">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link" @click="handleBreadcrumbClick(-1)">{{ rootName }}</span>
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
          class="upload-btn"
        >
          <el-button type="primary" class="pan-button-primary" :icon="Upload">上传文件</el-button>
        </el-upload>
        <el-button v-if="category === 'files'" :icon="FolderAdd" @click="showCreateFolder = true">新建文件夹</el-button>
        <el-button v-if="category === 'files'" :icon="Link" @click="showOfflineDownload = true">离线下载</el-button>
        <el-button v-if="selectedIds.length > 0 && category === 'files'" :icon="Rank" @click="handleBatchMove">批量移动</el-button>
        <el-button v-if="selectedIds.length > 0" type="danger" plain :icon="Delete" @click="handleBatchDelete">批量删除</el-button>
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

        <el-table-column label="修改时间" width="200" prop="time" sortable="custom">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.createTime) }}</span>
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
    </div>

    <!-- 缩略图模式 -->
    <div class="grid-container" v-show="viewMode === 'grid'" v-loading="loading">
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

    <!-- 新建文件夹弹窗 -->
    <el-dialog v-model="showCreateFolder" title="新建文件夹" width="400px">
      <el-input v-model="newFolderName" placeholder="请输入文件夹名称" />
      <template #footer>
        <el-button @click="showCreateFolder = false">取消</el-button>
        <el-button type="primary" class="pan-button-primary" @click="confirmCreateFolder">确定</el-button>
      </template>
    </el-dialog>

    <!-- 离线下载弹窗 -->
    <el-dialog v-model="showOfflineDownload" title="离线下载" width="500px">
      <el-form label-position="top">
        <el-form-item label="文件链接 (URL)">
          <el-input 
            v-model="offlineUrl" 
            type="textarea" 
            :rows="3" 
            placeholder="请输入文件的直接下载链接..." 
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showOfflineDownload = false">取消</el-button>
        <el-button type="primary" class="pan-button-primary" @click="confirmOfflineDownload" :loading="offlineLoading">开始下载</el-button>
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
            <el-icon><RefreshLeft /></el-icon>
            <span>返回上一级</span>
          </div>

          <div 
            class="folder-item root-item" 
            v-if="moveDialogCurrentParentId === null"
            :class="{ active: moveTargetParentId === null }"
            @click="moveTargetParentId = null"
          >
            <el-icon><Folder /></el-icon>
            <span>根目录</span>
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
            <el-button @click="showMoveDialog = false">取消</el-button>
            <el-button type="primary" class="pan-button-primary" @click="confirmMove">移动到此处</el-button>
          </div>
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
                <span class="label">分享链接：</span>
                <el-input :value="'http://localhost:5173/share/' + shareInfo.shareToken" readonly>
                  <template #append>
                    <el-button @click="copyText('http://localhost:5173/share/' + shareInfo.shareToken)">复制</el-button>
                  </template>
                </el-input>
              </div>
              <div class="detail-item">
                <span class="label">提取码：</span>
                <el-input :value="shareInfo.shareCode" readonly>
                  <template #append>
                    <el-button @click="copyText(shareInfo.shareCode)">复制</el-button>
                  </template>
                </el-input>
              </div>
              <p class="expire-tip" v-if="shareInfo.expireTime">
                有效期至：{{ formatDate(shareInfo.expireTime) }}
              </p>
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
      top="5vh"
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
      top="5vh"
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
      top="5vh"
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
            <span class="task-status">{{ task.statusText }}</span>
          </div>
          <el-progress :percentage="task.progress" :status="task.progressStatus" :stroke-width="4" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { 
  Upload, FolderAdd, FolderOpened, Document, 
  Star, StarFilled, Download, More, Edit, Rank, Share, Delete, Link, Folder, RefreshLeft,
  Menu, Grid, Picture, VideoPlay, Notebook, Box, Headset,
  ArrowRight, FullScreen, Aim, Close, Search
} from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'
import FilePreview from '../components/FilePreview/FilePreview.vue'
import SparkMD5 from 'spark-md5'

const props = defineProps<{
  category?: string
  type?: string
}>()

const route = useRoute()
const router = useRouter()
const loading = ref(true)
const fileList = ref<any[]>([])
const uploadTasks = ref<any[]>([])
const activeUploadCount = computed(() => uploadTasks.value.filter(t => t.status === 'uploading').length)
const pathStack = ref<{ id: number; name: string }[]>(history.state?.pathStack || [])
const selectedIds = ref<number[]>([])
const currentParentId = ref<number | null>(route.query.folderId ? Number(route.query.folderId) : null)
const sortState = ref<{ prop: string, order: string } | null>(null)
const viewMode = ref<'list' | 'grid'>(localStorage.getItem('viewMode') as 'list' | 'grid' || 'list')

const rootName = computed(() => {
  if (props.category === 'favorites') return '我的收藏'
  if (props.category === 'recycle-bin') return '回收站'
  return '全部文件'
})

const showCreateFolder = ref(false)
const newFolderName = ref('')
const showOfflineDownload = ref(false)
const offlineUrl = ref('')
const showMoveDialog = ref(false)
const searchKeyword = ref((route.query.q as string) || '')
const offlineLoading = ref(false)
const moveTargetParentId = ref<number | null>(null)
const moveDialogCurrentParentId = ref<number | null>(null)
const moveDialogPathStack = ref<{ id: number; name: string }[]>([])
const movingItemIds = ref<number[]>([])

const currentMovePathText = computed(() => {
  if (moveDialogPathStack.value.length === 0) return '全部文件'
  return moveDialogPathStack.value.map(p => p.name).join(' / ')
})

const folderTree = ref<any[]>([])
const showShareDialog = ref(false)
const shareInfo = ref<any>(null)
const shareExpireDays = ref(7)

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
  fileType: 'unknown' as 'docx' | 'pdf' | 'excel' | 'image' | 'ppt' | 'unknown'
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
    folderTree.value = res.filter((item: any) => item.isFolder)
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

const handleShare = async (row: any) => {
  try {
    const res: any = await request.post('/share/create', { 
      storageItemId: row.id, 
      expireDays: shareExpireDays.value 
    })
    shareInfo.value = res
    showShareDialog.value = true
    // 更新当前行的分享状态
    row.isShared = true
  } catch (error) {
    console.error(error)
  }
}

const copyText = (text: string) => {
  navigator.clipboard.writeText(text)
  ElMessage.success('已复制到剪贴板')
}

const fetchFiles = async () => {
  loading.value = true
  try {
    let url = '/file/list'
    const params: any = {}
    
    if (route.query.q) {
      url = '/file/search'
      params.keyword = route.query.q
    } else if (currentParentId.value) {
      // 如果进入了文件夹，则作为普通文件列表处理 (支持从收藏夹进入文件夹)
      url = '/file/list'
      params.parentId = currentParentId.value
    } else if (props.category === 'favorites') {
      url = '/file/favorites'
    } else if (props.category === 'recycle-bin') {
      url = '/file/recycle-bin'
    } else if (props.type) {
      params.category = props.type
    } else {
      params.parentId = currentParentId.value
    }

    // 添加排序参数
    if (sortState.value && sortState.value.order) {
      params.sortBy = sortState.value.prop
      params.order = sortState.value.order === 'ascending' ? 'asc' : 'desc'
    }

    const res: any = await request.get(url, { params })
    fileList.value = res
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
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
  } else if (isText(row.name)) {
    handlePreviewText(row)
  } else if (isPdf(row.name)) {
    handlePreviewPdf(row)
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
  if (index === -1) {
    // Root
    router.push({
      path: route.path,
      query: { ...route.query, folderId: undefined },
      state: { pathStack: [] }
    })
  } else if (index < pathStack.value.length - 1) {
    const targetItem = pathStack.value[index]
    const newPathStack = pathStack.value.slice(0, index + 1)
    
    if (targetItem) {
      router.push({
        path: route.path,
        query: { ...route.query, folderId: targetItem.id },
        state: { pathStack: JSON.parse(JSON.stringify(newPathStack)) }
      })
    }
  }
}

const generateGuid = () => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0
    const v = c === 'x' ? r : (r & 0x3 | 0x8)
    return v.toString(16)
  })
}

const calculateMd5 = (file: File): Promise<string> => {
  return new Promise((resolve, reject) => {
    const chunkSize = 10 * 1024 * 1024 // 10MB
    const chunks = Math.ceil(file.size / chunkSize)
    let currentChunk = 0
    const spark = new SparkMD5.ArrayBuffer()
    const fileReader = new FileReader()

    fileReader.onload = (e: any) => {
      spark.append(e.target.result)
      currentChunk++
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
      const start = currentChunk * chunkSize
      const end = ((start + chunkSize) >= file.size) ? file.size : start + chunkSize
      fileReader.readAsArrayBuffer(file.slice(start, end))
    }

    loadNext()
  })
}

const handleUpload = async (options: any) => {
  const file = options.file
  const fileSize = file.size
  const fileName = file.name
  const guid = generateGuid()
  
  const task = ref({
    guid,
    name: fileName,
    progress: 0,
    status: 'uploading',
    statusText: '正在计算 MD5...',
    progressStatus: '' as '' | 'success' | 'exception' | 'warning'
  })
  
  uploadTasks.value.unshift(task.value)
  
  try {
    // 1. 计算 MD5
    const md5 = await calculateMd5(file)
    task.value.statusText = '正在校验秒传...'
    
    // 2. 秒传校验
    try {
      const checkRes: any = await request.post('/file/check-md5', {
        md5: md5,
        fileName: fileName,
        fileSize: fileSize,
        parentId: currentParentId.value
      }, { 
        // @ts-ignore
        _showError: false 
      })
      if (checkRes) {
        task.value.progress = 100
        task.value.status = 'success'
        task.value.statusText = '秒传成功'
        task.value.progressStatus = 'success'
        fetchFiles()
        return
      }
    } catch (e: any) {
      if (e.response && e.response.status !== 404) {
        throw e
      }
    }

    // 3. 分片上传
    task.value.statusText = '正在上传分片...'
    const chunkSize = 5 * 1024 * 1024 // 5MB 
    const chunksCount = Math.ceil(fileSize / chunkSize)
    
    for (let i = 0; i < chunksCount; i++) {
        const start = i * chunkSize
        const end = Math.min(start + chunkSize, fileSize)
        const chunk = file.slice(start, end)
        
        const chunkFormData = new FormData()
        chunkFormData.append('file', chunk)
        chunkFormData.append('guid', guid)
        chunkFormData.append('chunkIndex', i.toString())
        
        await request.post('/file/upload-chunk', chunkFormData)
        
        // 更新进度 (假设合并占 5%)
        task.value.progress = Math.floor(((i + 1) / chunksCount) * 95)
    }
    
    // 4. 合并分片
    task.value.statusText = '正在合并文件...'
    await request.post('/file/merge-chunks', {
        guid: guid,
        fileName: fileName,
        totalSize: fileSize,
        parentId: currentParentId.value,
        md5: md5
    })
    
    task.value.progress = 100
    task.value.status = 'success'
    task.value.statusText = '上传成功'
    task.value.progressStatus = 'success'
    fetchFiles()
  } catch (error: any) {
    console.error(error)
    task.value.status = 'error'
    task.value.statusText = error.response?.data || error.message || '上传失败'
    task.value.progressStatus = 'exception'
    const msg = error.response?.data || error.message || '上传失败'
    ElMessage.error(msg)
  }
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

const confirmOfflineDownload = async () => {
  if (!offlineUrl.value) return
  offlineLoading.value = true
  try {
    await request.post('/file/offline-download', {
      url: offlineUrl.value,
      parentId: currentParentId.value
    })
    ElMessage.success('离线下载完成')
    showOfflineDownload.value = false
    offlineUrl.value = ''
    fetchFiles()
  } catch (error) {
    console.error(error)
  } finally {
    offlineLoading.value = false
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
  return ['txt', 'md', 'json', 'xml', 'css', 'js', 'html', 'log', 'ini', 'conf'].includes(ext || '')
}

const isPdf = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ext === 'pdf'
}

const isDocx = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  return ext === 'docx'
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
  if (isDocx(name)) return Document
  if (isExcel(name)) return Grid
  if (isText(name)) return Notebook
  
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
  const message = isRecycleBin 
    ? `确定要彻底删除选中的 ${selectedIds.value.length} 个项目吗？此操作不可恢复！`
    : `确定要删除选中的 ${selectedIds.value.length} 个项目吗？`

  ElMessageBox.confirm(message, '提示', { 
    type: isRecycleBin ? 'error' : 'warning',
    confirmButtonClass: isRecycleBin ? 'el-button--danger' : ''
  }).then(async () => {
    try {
      const url = isRecycleBin ? '/file/batch-delete-permanent' : '/file/batch-delete'
      await request.post(url, { ids: selectedIds.value })
      ElMessage.success(isRecycleBin ? '批量彻底删除成功' : '批量删除成功')
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

const handleSortChange = ({ prop, order }: { prop: string, order: string }) => {
  sortState.value = { prop, order }
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
  return new Date(dateStr).toLocaleString()
}

watch(() => route.query.q, () => fetchFiles())
watch(() => props.type, () => fetchFiles())
watch(() => props.category, () => fetchFiles())

onMounted(() => {
  fetchFiles()
})
</script>

<style scoped lang="scss">
.file-list-view {
  display: flex;
  flex-direction: column;
  /* gap: 20px; Removed to control spacing manually via margins */
  height: 100%;
  overflow: hidden;
}

.action-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
  flex-wrap: nowrap;
  padding: 0 20px 0 50px;
  border-bottom: 1px solid var(--pan-border);
  height: 60px;
  /* margin-bottom: 20px; Removed to assume content handles its own spacing */
  flex-shrink: 0;

  .breadcrumb {
    display: flex;
    gap: 12px;
    align-items: center;
    flex-shrink: 1;
    min-width: 0;
    overflow: hidden;
    
    :deep(.el-breadcrumb__inner) {
      color: var(--pan-text-muted) !important;
      font-size: 13px;
      font-weight: 400;
      
      &.is-link:hover {
        color: var(--pan-text-main) !important;
      }
    }

    .breadcrumb-link {
      cursor: pointer;
      transition: var(--pan-transition);
      
      &.is-last {
        color: var(--pan-text-main) !important;
        font-weight: 600;
        cursor: default;
      }
    }
  }

  .buttons {
    display: flex;
    gap: 12px;
    align-items: center;
    flex-shrink: 0;

    .view-switch-wrapper {
      margin-left: 8px;
      padding-left: 16px;
      border-left: 1px solid var(--pan-border);
      display: flex;
      align-items: center;
      height: 24px;
    }
  }
}

.table-container {
  flex: 1;
  min-height: 0;
  background-color: #000000 !important;
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-sm);
  overflow: auto;
  animation: fadeIn 0.3s ease-in-out;
  margin: 20px; /* Added consistent spacing */
}

.file-name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  padding: 4px 0;
  
  &:hover .name {
    color: var(--pan-primary);
  }

  .image-preview-wrapper {
    width: 32px;
    height: 32px;
    border-radius: 6px;
    overflow: hidden;
    flex-shrink: 0;
    border: 1px solid var(--pan-border);

    .thumbnail {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  }

  .folder-icon {
    color: #FCD34D; /* Amber 300 */
    filter: drop-shadow(0 0 5px rgba(252, 211, 77, 0.3));
  }

  .file-icon {
    color: var(--pan-accent);
    filter: drop-shadow(0 0 5px rgba(61, 155, 255, 0.3));
  }

  .share-status-icon {
    color: var(--pan-primary);
    font-size: 14px;
    margin-left: 4px;
  }

  .name {
    font-weight: 500;
    color: var(--pan-text-main);
    transition: var(--pan-transition);
  }
}

.row-actions {
  display: flex;
  gap: 4px;
}

.folder-selector {
  max-height: 350px;
  overflow-y: auto;
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-md);
  background: #050505;

  .folder-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 15px;
    cursor: pointer;
    transition: all 0.2s;
    border-bottom: 1px solid var(--pan-border);
    color: var(--pan-text-body);

    &:last-child {
      border-bottom: none;
    }

    &:hover {
      background-color: rgba(255, 255, 255, 0.03);
      color: var(--pan-text-main);
    }

    &.active {
      background-color: rgba(16, 185, 129, 0.1);
      color: var(--pan-primary);
      font-weight: bold;
    }

    .folder-info {
      display: flex;
      align-items: center;
      gap: 10px;
      flex: 1;
      
      .folder-name {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        max-width: 250px;
      }
    }

    .enter-icon {
      padding: 5px;
      border-radius: 4px;
      color: var(--pan-text-muted);
      display: flex;
      align-items: center;
      
      &:hover {
        background-color: rgba(255, 255, 255, 0.05);
        color: var(--pan-primary);
      }
    }
  }

  .back-item {
    color: var(--pan-primary);
    background-color: transparent;
    font-size: 13px;
    
    &:hover {
      background-color: rgba(255, 255, 255, 0.03);
    }
  }

  .empty-folder {
    padding: 40px 0;
    text-align: center;
    color: var(--pan-text-muted);
    font-size: 13px;
  }
}

.move-dialog-content {
  .move-path-nav {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 5px;
    margin-bottom: 15px;
    padding: 8px 12px;
    background: #0a0a0a;
    border: 1px solid var(--pan-border);
    border-radius: var(--pan-radius-sm);
    font-size: 13px;

    .nav-item {
      cursor: pointer;
      color: var(--pan-text-body);
      
      &:hover {
        color: var(--pan-primary);
        text-decoration: underline;
      }

      &:last-child {
        color: var(--pan-text-main);
        font-weight: bold;
        cursor: default;
        &:hover {
          text-decoration: none;
        }
      }
    }

    .nav-separator {
      font-size: 12px;
      color: var(--pan-text-muted);
    }
  }
}

.move-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;

  .selected-target {
    font-size: 13px;
    color: var(--pan-text-body);
    
    .path-text {
      color: var(--pan-primary);
      font-weight: bold;
    }
  }
}

.share-details {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 15px;
  text-align: left;

  .detail-item {
    .label {
      display: block;
      margin-bottom: 5px;
      font-size: 13px;
      color: var(--pan-text-light);
    }
  }

  .expire-tip {
    font-size: 12px;
    color: #f56c6c;
    margin: 0;
  }
}

:deep(.el-table) {
  --el-table-header-bg-color: transparent;
  background-color: transparent;
}

.operation-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.view-switch-btn {
  font-size: 18px;
  color: var(--pan-text-muted);
  padding: 4px;
  transition: var(--pan-transition);
  display: flex;
  align-items: center;
  justify-content: center;
  
  &:hover {
    color: var(--pan-text-main);
    background: rgba(255, 255, 255, 0.05);
    border-radius: var(--pan-radius-sm);
  }
}

.file-name-cell.drag-over, .grid-item.drag-over .grid-item-inner {
    border: 2px dashed var(--pan-primary);
    background-color: rgba(16, 185, 129, 0.1);
  }

  .grid-container {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(100px, 100px));
  grid-auto-rows: max-content;
  gap: 16px 12px;
  justify-content: center;
  align-content: start;
  background-color: #000000 !important;
  border-radius: var(--pan-radius-lg);
  border: 1px solid var(--pan-border-strong);
  box-shadow: var(--pan-shadow-sm);
  animation: fadeIn 0.3s ease-in-out;

  .empty-tip {
    grid-column: 1 / -1;
    display: flex;
    justify-content: center;
    padding-top: 100px;
  }
  
  .grid-item {
     width: 100px;
     height: 100px;
     cursor: pointer;
     
     .grid-item-inner {
      width: 100%;
      height: 100%;
      border-radius: var(--pan-radius-sm);
      padding: 8px;
      display: flex;
      flex-direction: column;
      align-items: center;
      position: relative;
      transition: var(--pan-transition);
      border: 1px solid transparent;
      
      &:hover {
        background: rgba(255, 255, 255, 0.05);
        border-color: var(--pan-border);
        
        .grid-actions {
          opacity: 1;
        }
      }
    }
    
    .grid-preview {
      width: 44px;
      height: 44px;
      display: flex;
      justify-content: center;
      align-items: center;
      margin-bottom: 6px;
      
      .grid-thumbnail {
        width: 100%;
        height: 100%;
        border-radius: var(--pan-radius-sm);
        box-shadow: var(--pan-shadow-sm);
      }
      
      .folder-icon {
        color: #fbbf24;
      }

      .file-icon {
        color: #10b981;
      }
    }
    
    .grid-name {
      font-size: 13px;
      font-weight: 500;
      text-align: center;
      width: 100%;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      line-height: 1.4;
      color: var(--pan-text-main);
    }
    
    .grid-actions {
      position: absolute;
      top: 5px;
      right: 5px;
      opacity: 0;
      transition: opacity 0.2s;
      display: flex;
      gap: 2px;
      transform: scale(0.8);
      transform-origin: top right;
    }
  }
}

.breadcrumb-link {
  font-weight: 600;
  color: var(--pan-text-body);
  cursor: pointer;
  transition: var(--pan-transition);
  
  &:hover {
    color: var(--pan-primary);
  }

  &.is-last {
    color: var(--pan-text-main);
    cursor: default;
    
    &:hover {
      color: var(--pan-text-main);
    }
  }
}

.search-wrapper {
  flex: 1;
  max-width: 400px;
  margin: 0 20px;

  .search-input {
    :deep(.el-input__wrapper) {
      background-color: rgba(255, 255, 255, 0.05) !important;
      border: 1px solid var(--pan-border) !important;
      box-shadow: none !important;
      border-radius: var(--pan-radius-lg) !important;
      height: 36px;
      transition: all 0.3s;

      &:hover, &.is-focus {
        background-color: rgba(255, 255, 255, 0.08) !important;
        border-color: var(--pan-primary) !important;
      }
    }
  }
}

.text-preview-wrapper {
  height: 100%;
  overflow: hidden;
}

.text-preview-content {
  background-color: #050505;
  color: var(--pan-text-body);
  padding: 20px;
  border-radius: var(--pan-radius-sm);
  overflow: auto;
  white-space: pre-wrap;
  word-wrap: break-word;
  height: 100%; /* Fill the container */
  font-family: var(--font-mono);
  font-size: 13px;
  line-height: 1.6;
  border: 1px solid var(--pan-border);
}

.video-container {
  width: 100%;
  height: 100%;
  background: #000;
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: var(--pan-radius-sm);
  overflow: hidden;
}

.video-player {
  width: 100%;
  height: 100%;
  max-height: 100%;
  object-fit: contain;
  outline: none;
}

.pdf-frame {
  width: 100%;
  height: 100%;
  border: none;
  background-color: #333;
}

/* Global Preview Dialog Styles */
:deep(.preview-dialog) {
  background-color: #050505 !important;
  border: 1px solid var(--pan-border) !important;
  border-radius: var(--pan-radius-md);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  height: 85vh; /* Initial state height */
  transition: all 0.3s ease;

  &.is-fullscreen {
    height: 100vh !important;
    width: 100vw !important;
    top: 0 !important;
    margin: 0 !important;
    border-radius: 0;
    border: none !important;
  }

  .el-dialog__header {
    margin: 0 !important;
    padding: 0 8px 0 20px !important; /* Right padding adjusted for buttons */
    background-color: #050505 !important;
    border-bottom: 1px solid var(--pan-border) !important;
    flex-shrink: 0;
    height: 48px !important;
    display: flex !important;
    align-items: center !important;
    justify-content: space-between !important;

    .el-dialog__headerbtn {
      position: static !important; /* Break absolute positioning to participate in flex row */
      margin: 0 !important;
      height: 48px !important;
      width: 48px !important;
      display: flex !important;
      align-items: center !important;
      justify-content: center !important;
      padding: 0 !important;
      order: 10; /* Ensure it is at the very end */

      .el-dialog__close {
        color: var(--pan-text-muted) !important;
        font-size: 18px !important;
        margin: 0 !important;
        
        &:hover {
          color: var(--pan-text-main) !important;
        }
      }
    }
  }
 
  .dialog-header-custom {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 100%;
    margin-right: 0;
 
    .el-dialog__title {
      color: var(--pan-text-main) !important;
      font-size: 14px !important;
      font-weight: 600 !important;
      line-height: normal !important;
      margin: 0;
    }
 
    .header-actions {
      display: flex;
      align-items: center;
      margin-left: auto; /* Push to right */
      
      .el-button.fullscreen-btn {
        padding: 0;
        width: 48px;
        height: 48px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 18px;
        color: var(--pan-text-muted);
        cursor: pointer;
        pointer-events: auto !important; 
        border: none !important;
        background: transparent !important;
        
        &:hover {
          color: var(--pan-text-main);
          background-color: rgba(255, 255, 255, 0.05) !important;
        }
 
        :deep(.el-icon) {
          font-size: 18px;
          margin: 0;
        }
      }
    }
  }

  .el-dialog__body {
    padding: 0;
    flex: 1;
    overflow: hidden;
    background-color: #050505;
    display: flex;
    flex-direction: column;
  }
}

:deep(.text-preview-dialog), :deep(.video-preview-dialog) {
  .el-dialog__body {
    padding: 24px;
  }
}

/* Ensure Image Viewer is on top */
:global(.el-image-viewer__wrapper) {
  z-index: 9999 !important;
}

.upload-task-panel {
  position: fixed;
  right: 24px;
  bottom: 24px;
  width: 320px;
  background: #0a0a0a;
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-md);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.6);
  z-index: 1000;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  max-height: 400px;

  .panel-header {
    padding: 12px 16px;
    background: rgba(255, 255, 255, 0.03);
    border-bottom: 1px solid var(--pan-border);
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 13px;
    font-weight: 600;
    color: var(--pan-text-main);
  }

  .task-list {
    flex: 1;
    overflow-y: auto;
    padding: 8px 16px;
    
    .task-item {
      padding: 12px 0;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
      
      &:last-child {
        border-bottom: none;
      }

      .task-info {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 8px;
        gap: 12px;

        .task-name {
          font-size: 12px;
          color: var(--pan-text-main);
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          flex: 1;
        }

        .task-status {
          font-size: 11px;
          color: var(--pan-text-muted);
        }
      }
      
      :deep(.el-progress-bar__outer) {
        background-color: rgba(255, 255, 255, 0.05);
      }
    }
  }
}
</style>
