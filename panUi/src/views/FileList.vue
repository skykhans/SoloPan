<template>
  <div class="file-list-view">
    <div class="action-bar">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link" @click="handleBreadcrumbClick(-1)">全部文件</span>
          </el-breadcrumb-item>
          <el-breadcrumb-item v-for="(item, index) in pathStack" :key="item.id">
            <span 
              :class="['breadcrumb-link', { 'is-last': index === pathStack.length - 1 }]" 
              @click="handleBreadcrumbClick(index)"
            >
              {{ item.name }}
            </span>
          </el-breadcrumb-item>
        </el-breadcrumb>
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
      </div>
    </div>

    <div class="table-container pan-card" v-if="viewMode === 'list'">
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
                <Document v-else />
              </el-icon>
              <span class="name">{{ row.name }}</span>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="大小" width="120" prop="size" sortable="custom">
          <template #default="{ row }">
            {{ row.isFolder ? '-' : formatSize(row.fileSize) }}
          </template>
        </el-table-column>

        <el-table-column label="修改时间" width="200" prop="time" sortable="custom">
          <template #default="{ row }">
            {{ formatDate(row.createTime) }}
          </template>
        </el-table-column>

        <el-table-column label="操作" width="180" fixed="right">
          <template #header>
            <div class="operation-header">
              <span>操作</span>
              <el-tooltip :content="viewMode === 'list' ? '切换到缩略图模式' : '切换到列表模式'" placement="top">
                <el-button link :icon="viewMode === 'list' ? Grid : Menu" @click="toggleViewMode" class="view-switch-btn" />
              </el-tooltip>
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
    <div class="grid-container" v-else v-loading="loading">
      <!-- 顶部操作栏补充（仅在缩略图模式下显示，因为列表模式已经在表头显示了） -->
      <div class="grid-toolbar">
         <div class="spacer"></div>
         <el-tooltip content="切换到列表模式" placement="top">
            <el-button link :icon="Menu" @click="toggleViewMode" class="view-switch-btn-grid" />
         </el-tooltip>
      </div>

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
            <el-icon v-else :size="64" :class="file.isFolder ? 'folder-icon' : 'file-icon'">
              <FolderOpened v-if="file.isFolder" />
              <Document v-else />
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
        <el-button type="primary" class="pan-button-primary" @click="confirmOfflineDownload">开始下载</el-button>
      </template>
    </el-dialog>

    <!-- 移动弹窗 -->
    <el-dialog v-model="showMoveDialog" title="移动到" width="400px">
      <div class="folder-selector">
        <div 
          class="folder-item" 
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
          :class="{ active: moveTargetParentId === folder.id }"
          @click="moveTargetParentId = folder.id"
        >
          <el-icon><Folder /></el-icon>
          <span>{{ folder.name }}</span>
        </div>
      </div>
      <template #footer>
        <el-button @click="showMoveDialog = false">取消</el-button>
        <el-button type="primary" class="pan-button-primary" @click="confirmMove">确定移动</el-button>
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
      :title="videoName" 
      width="800px" 
      destroy-on-close
      center
      @closed="videoUrl = ''"
      class="preview-dialog video-preview-dialog"
    >
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
      :title="textFileName"
      width="800px"
      destroy-on-close
      center
      class="preview-dialog text-preview-dialog"
    >
      <div class="text-preview-wrapper">
        <pre class="text-preview-content">{{ textContent }}</pre>
      </div>
    </el-dialog>

    <!-- PDF 预览弹窗 -->
    <el-dialog
      v-model="showPdfPreview"
      :title="pdfName"
      width="90%"
      top="5vh"
      destroy-on-close
      center
      class="preview-dialog pdf-preview-dialog"
    >
      <iframe :src="pdfUrl" class="pdf-frame"></iframe>
    </el-dialog>

    <!-- Word (docx) 预览弹窗 -->
    <el-dialog
      v-model="showDocxPreview"
      :title="docxName"
      width="90%"
      top="5vh"
      destroy-on-close
      center
      class="preview-dialog docx-preview-dialog"
    >
      <div class="docx-wrapper">
        <div ref="docxContainer" class="docx-container"></div>
      </div>
    </el-dialog>

    <!-- Excel 预览弹窗 -->
    <el-dialog
      v-model="showExcelPreview"
      :title="excelName"
      width="90%"
      top="5vh"
      destroy-on-close
      center
      class="preview-dialog excel-preview-dialog"
    >
      <div class="excel-preview-container">
        <el-tabs v-model="excelActiveSheet" @tab-click="handleExcelTabClick" class="excel-tabs">
          <el-tab-pane
            v-for="sheet in (excelWorkbook ? excelWorkbook.SheetNames : [])"
            :key="sheet"
            :label="sheet"
            :name="sheet"
          />
        </el-tabs>
        <div class="excel-content-wrapper">
          <div class="excel-content" v-html="excelHtml"></div>
        </div>
      </div>
    </el-dialog>

    <!-- 图片预览组件 -->
    <el-image-viewer
      v-if="showImagePreview"
      :url-list="previewList"
      :initial-index="previewInitialIndex"
      @close="showImagePreview = false"
      teleported
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { 
  Upload, FolderAdd, FolderOpened, Document, 
  Star, StarFilled, Download, More, Edit, Rank, Share, Delete, Link, Folder, RefreshLeft, Monitor,
  Menu, Grid
} from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox, ElImageViewer } from 'element-plus'

import { renderAsync } from 'docx-preview'
import * as XLSX from 'xlsx'

const props = defineProps<{
  category?: string
  type?: string
}>()

const route = useRoute()
const loading = ref(false)
const fileList = ref<any[]>([])
const pathStack = ref<{ id: number; name: string }[]>([])
const selectedIds = ref<number[]>([])
const currentParentId = ref<number | null>(null)
const sortState = ref<{ prop: string, order: string } | null>(null)
const viewMode = ref<'list' | 'grid'>(localStorage.getItem('viewMode') as 'list' | 'grid' || 'list')

const showCreateFolder = ref(false)
const newFolderName = ref('')
const showOfflineDownload = ref(false)
const offlineUrl = ref('')
const showMoveDialog = ref(false)
const moveTargetParentId = ref<number | null>(null)
const movingItemIds = ref<number[]>([])
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
const showPdfPreview = ref(false)
const pdfUrl = ref('')
const pdfName = ref('')
const showDocxPreview = ref(false)
const docxName = ref('')
const showExcelPreview = ref(false)
const excelName = ref('')
const excelWorkbook = ref<any>(null)
const excelActiveSheet = ref('')
const excelHtml = ref('')

const docxContainer = ref<HTMLElement | null>(null)

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
    } catch (error) {
      console.error(error)
    }
  })
}

const handleMove = async (row: any) => {
  movingItemIds.value = [row.id]
  showMoveDialog.value = true
  fetchFolderTree()
}

const handleBatchMove = () => {
  movingItemIds.value = selectedIds.value
  showMoveDialog.value = true
  fetchFolderTree()
}

const fetchFolderTree = async () => {
  try {
    const res: any = await request.get('/file/list') // 获取根目录下的所有文件夹
    // 简单处理，只显示根目录下的文件夹，实际应支持树形结构
    folderTree.value = res.filter((item: any) => item.isFolder)
  } catch (error) {
    console.error(error)
  }
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
    
    if (props.category === 'favorites') {
      url = '/file/favorites'
    } else if (props.category === 'recycle-bin') {
      url = '/file/recycle-bin'
    } else if (props.type) {
      params.category = props.type
    } else {
      params.parentId = currentParentId.value
    }

    if (route.query.q) {
      url = '/file/search'
      params.keyword = route.query.q
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
    pathStack.value = []
    currentParentId.value = null
    fetchFiles()
  }
})

const showImagePreview = ref(false)
const previewInitialIndex = ref(0)
const previewList = ref<string[]>([])

const handlePreviewImage = (row: any) => {
  // 收集当前列表中的所有图片
  const images = fileList.value.filter(item => !item.isFolder && isImage(item.name))
  previewList.value = images.map(item => getDownloadUrl(item.id))
  
  // 找到当前点击的图片在列表中的索引
  const index = images.findIndex(item => item.id === row.id)
  previewInitialIndex.value = index >= 0 ? index : 0
  
  showImagePreview.value = true
}

const handleRowClick = (row: any) => {
  if (row.isFolder) {
    currentParentId.value = row.id
    pathStack.value.push({ id: row.id, name: row.name })
    fetchFiles()
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
    const res = await request.get(`/file/download/${row.id}`, { responseType: 'text' })
    textContent.value = res as unknown as string
    showTextPreview.value = true
  } catch (error) {
    console.error(error)
    ElMessage.error('预览失败')
  }
}

const handlePreviewPdf = (row: any) => {
  pdfName.value = row.name
  // 增加时间戳，防止缓存；增加 preview=true 参数
  pdfUrl.value = getDownloadUrl(row.id) + '&preview=true&t=' + new Date().getTime()
  showPdfPreview.value = true
}

const handlePreviewDocx = async (row: any) => {
  docxName.value = row.name
  showDocxPreview.value = true
  
  try {
    const res = await request.get(`/file/download/${row.id}`, { responseType: 'blob' })
    // 使用 nextTick 确保 DOM 已更新
    await nextTick()
    if (docxContainer.value) {
        renderAsync(res as unknown as Blob, docxContainer.value, docxContainer.value, {
            className: 'docx-preview',
            inWrapper: true,
            ignoreWidth: false,
            ignoreHeight: false,
            experimental: true
        })
    }
  } catch (error) {
    console.error(error)
    ElMessage.error('预览失败')
  }
}

const handlePreviewExcel = async (row: any) => {
  excelName.value = row.name
  showExcelPreview.value = true
  
  try {
    const res = await request.get(`/file/download/${row.id}`, { responseType: 'arraybuffer' })
    const data = new Uint8Array(res as unknown as ArrayBuffer)
    const workbook = XLSX.read(data, { type: 'array' })
    excelWorkbook.value = workbook
    
    if (workbook.SheetNames.length > 0) {
      excelActiveSheet.value = workbook.SheetNames[0]
      renderExcelSheet(workbook.SheetNames[0])
    }
  } catch (error) {
    console.error(error)
    ElMessage.error('预览失败')
  }
}

const handleExcelTabClick = (tab: any) => {
  renderExcelSheet(tab.props.name)
}

const renderExcelSheet = (sheetName: string) => {
  if (!excelWorkbook.value) return
  const worksheet = excelWorkbook.value.Sheets[sheetName]
  excelHtml.value = XLSX.utils.sheet_to_html(worksheet)
}

const handleBreadcrumbClick = (index: number) => {
  if (index === -1) {
    // 点击根目录
    pathStack.value = []
    currentParentId.value = null
  } else if (index < pathStack.value.length - 1) {
    // 点击中间层级 (如果是点击最后一个，即当前目录，则不做任何操作)
    const targetItem = pathStack.value[index]
    currentParentId.value = targetItem.id
    pathStack.value = pathStack.value.slice(0, index + 1)
  } else {
    return
  }
  fetchFiles()
}

const handleUpload = async (options: any) => {
  const formData = new FormData()
  formData.append('file', options.file)
  // 即使 parentId 为空，也最好不要传 undefined，后端模型绑定可能会有问题
  // 这里可以显式判断一下
  if (currentParentId.value !== null && currentParentId.value !== undefined) {
    formData.append('parentId', currentParentId.value.toString())
  }

  try {
    await request.post('/file/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
    ElMessage.success('上传成功')
    fetchFiles()
  } catch (error) {
    console.error(error)
    ElMessage.error('上传失败')
  }
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
  } catch (error) {
    console.error(error)
  }
}

const confirmOfflineDownload = async () => {
  if (!offlineUrl.value) return
  try {
    await request.post('/file/offline-download', {
      url: offlineUrl.value,
      parentId: currentParentId.value
    })
    ElMessage.success('离线下载任务已提交')
    showOfflineDownload.value = false
    offlineUrl.value = ''
  } catch (error) {
    console.error(error)
  }
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

const getThumbnailUrl = (id: number) => {
  const token = localStorage.getItem('token')
  return `http://localhost:5080/api/file/thumbnail/${id}?access_token=${token}`
}

const getDownloadUrl = (id: number) => {
  const token = localStorage.getItem('token')
  return `http://localhost:5080/api/file/download/${id}?access_token=${token}`
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
  ElMessageBox.confirm(`确定要删除选中的 ${selectedIds.value.length} 个项目吗？`, '提示', { type: 'warning' }).then(async () => {
    try {
      await request.post('/file/batch-delete', { ids: selectedIds.value })
      ElMessage.success('批量删除成功')
      fetchFiles()
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
  gap: 20px;
  height: 100%;
  overflow: hidden;
}

.action-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-shrink: 0;

  .buttons {
    display: flex;
    gap: 12px;
  }
}

.table-container {
  padding: 10px;
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.file-name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  
  &:hover .name {
    color: var(--pan-primary);
  }

  .image-preview-wrapper {
    width: 32px;
    height: 32px;
    border-radius: 4px;
    overflow: hidden;
    flex-shrink: 0;

    .thumbnail {
      width: 100%;
      height: 100%;
    }
  }

  .folder-icon {
    color: #ffd04b;
  }

  .file-icon {
    color: #76c893;
  }

  .name {
    transition: color 0.3s;
  }
}

.row-actions {
  display: flex;
  gap: 4px;
}

.folder-selector {
  max-height: 300px;
  overflow-y: auto;
  border: 1px solid var(--pan-border);
  border-radius: 8px;

  .folder-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 15px;
    cursor: pointer;
    transition: all 0.2s;

    &:hover {
      background-color: #f1f4f2;
    }

    &.active {
      background-color: #f0f9f4;
      color: var(--pan-primary);
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
  
  .view-switch-btn {
    padding: 0;
    height: auto;
    font-size: 16px;
    color: var(--el-text-color-regular);
    
    &:hover {
      color: var(--pan-primary);
    }
  }
}

.file-name-cell.drag-over, .grid-item.drag-over .grid-item-inner {
    border: 2px dashed var(--pan-primary);
    background-color: rgba(var(--pan-primary-rgb), 0.1);
  }

  .grid-toolbar {
    width: 100%;
    display: flex;
    justify-content: flex-end;
    padding-bottom: 10px;
    padding-right: 20px;
    border-bottom: 1px solid #ebeef5;
    margin-bottom: 10px;
    
    .view-switch-btn-grid {
      font-size: 18px;
      color: #606266;
      
      &:hover {
        color: var(--pan-primary);
      }
    }
  }

  .grid-container {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
  align-content: flex-start;
  
  .empty-tip {
    width: 100%;
    display: flex;
    justify-content: center;
    padding-top: 100px;
  }
  
  .grid-item {
     width: 120px;
     height: 150px;
     cursor: pointer;
     
     .grid-item-inner {
      width: 100%;
      height: 100%;
      border-radius: 8px;
      padding: 10px;
      display: flex;
      flex-direction: column;
      align-items: center;
      position: relative;
      transition: background-color 0.2s;
      
      &:hover {
        background-color: #f0f4f8;
        
        .grid-actions {
          opacity: 1;
        }
      }
    }
    
    .grid-preview {
      width: 80px;
      height: 80px;
      display: flex;
      justify-content: center;
      align-items: center;
      margin-bottom: 10px;
      
      .grid-thumbnail {
        width: 100%;
        height: 100%;
        border-radius: 6px;
      }
      
      .folder-icon {
        color: #ffd04b;
      }

      .file-icon {
        color: #76c893;
      }
    }
    
    .grid-name {
      font-size: 13px;
      text-align: center;
      width: 100%;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      line-height: 1.4;
      color: #606266;
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
  font-weight: normal;
  cursor: pointer;
  transition: color 0.2s;
  
  &:hover {
    color: var(--pan-primary);
  }

  &.is-last {
    color: var(--el-text-color-regular);
    cursor: default;
    font-weight: bold;
    
    &:hover {
      color: var(--el-text-color-regular);
    }
  }
}

.text-preview-content {
  background-color: #fafafa;
  color: #333;
  padding: 20px;
  border-radius: 8px;
  overflow-x: auto;
  white-space: pre-wrap;
  word-wrap: break-word;
  max-height: 60vh;
  overflow-y: auto;
  font-family: 'JetBrains Mono', 'Fira Code', Consolas, Monaco, monospace;
  font-size: 14px;
  line-height: 1.6;
  border: 1px solid #eee;
}

.docx-container {
  background: #fff;
  padding: 40px;
  min-height: 500px;
  max-height: 80vh;
  overflow-y: auto;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
}

.excel-preview-container {
  height: 80vh;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 4px;
}

.excel-tabs {
  padding: 0 20px;
  background-color: #f5f7fa;
  border-bottom: 1px solid #e4e7ed;
}

.excel-content-wrapper {
  flex: 1;
  overflow: auto;
  padding: 20px;
}

.excel-content {
  background: white;
  padding: 40px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
  margin: 20px;
  border-radius: 8px;
  overflow: auto;
  
  :deep(table) {
    border-collapse: separate;
    border-spacing: 0;
    width: 100%;
    font-size: 13px;
    color: #333;
    font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    
    td, th {
      border-right: 1px solid #e0e0e0;
      border-bottom: 1px solid #e0e0e0;
      padding: 8px 12px;
      min-width: 80px;
      text-align: left;
      position: relative;
      
      &:last-child {
        border-right: none;
      }
    }

    th {
      background-color: #f8f9fa;
      font-weight: 600;
      color: #495057;
      border-bottom: 2px solid #dee2e6;
      text-transform: uppercase;
      font-size: 12px;
      letter-spacing: 0.5px;
      position: sticky;
      top: 0;
      z-index: 10;
    }

    tr:last-child td {
      border-bottom: none;
    }

    tr:nth-child(even) {
      background-color: #fcfcfc;
    }

    tr:hover td {
      background-color: #e8f0fe;
      color: #1967d2;
    }
  }
}

.video-container {
  width: 100%;
  background: #000;
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 8px;
  overflow: hidden;
}

.video-player {
  width: 100%;
  max-height: 60vh;
  outline: none;
}

.pdf-frame {
  width: 100%;
  height: 80vh;
  border: none;
  background-color: #f5f5f5;
}

/* Global Preview Dialog Styles */
:deep(.preview-dialog) {
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.15);

  .el-dialog__header {
    margin: 0;
    padding: 20px 24px;
    border-bottom: 1px solid #f0f0f0;
    
    .el-dialog__title {
      font-weight: 600;
      font-size: 18px;
      color: #303133;
    }
  }

  .el-dialog__body {
    padding: 0;
    background-color: #f9f9f9;
  }
}

:deep(.text-preview-dialog), :deep(.video-preview-dialog) {
  .el-dialog__body {
    padding: 20px;
  }
}

/* Ensure Image Viewer is on top */
:global(.el-image-viewer__wrapper) {
  z-index: 9999 !important;
}
</style>
