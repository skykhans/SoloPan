<template>
  <div class="share-page">
    <div class="top-bar">
      <div class="logo">
        <div class="logo-icon">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 4L4 8L12 12L20 8L12 4Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M4 12L12 16L20 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M4 16L12 20L20 16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>
        <span class="logo-text">Trae Pan</span>
      </div>
      <div class="user-actions">
         <template v-if="!isLoggedIn">
           <span class="login-tip">登录后可保存文件</span>
           <el-button type="primary" link @click="goToLogin">登录 / 注册</el-button>
         </template>
         <template v-else>
           <span class="username">{{ username }}</span>
         </template>
      </div>
    </div>

    <div class="share-box" v-if="!fileInfo">
      <h2>请输入提取码</h2>
      <div class="input-area">
        <el-input v-model="shareCode" placeholder="请输入4位提取码" maxlength="4" style="width: 200px" />
        <el-button type="primary" @click="checkCode" :loading="loading">提取文件</el-button>
      </div>
    </div>

    <div :class="['file-info-box', { 'is-folder-view': fileInfo.isFolder }]" v-else>
      <div class="header">
        <el-icon :size="48" class="icon" :class="fileInfo.isFolder ? 'folder-icon' : 'file-icon'">
          <FolderOpened v-if="fileInfo.isFolder" />
          <component :is="getFileIcon(fileInfo.name)" v-else />
        </el-icon>
        <div class="info">
          <h3>{{ fileInfo.name }}</h3>
          <p>
            <span>{{ formatSize(fileInfo.fileSize) }}</span>
            <span class="divider">|</span>
            <span>分享者：{{ fileInfo.userName }}</span>
            <span class="divider">|</span>
            <span>有效期至：{{ fileInfo.expireTime ? formatDate(fileInfo.expireTime) : '永久有效' }}</span>
          </p>
        </div>
      </div>
      <div class="actions">
        <template v-if="!fileInfo.isFolder">
          <el-button type="primary" size="large" :icon="Download" @click="handleDownload()">下载文件</el-button>
          <el-button size="large" @click="saveToDrive">保存到我的网盘</el-button>
        </template>
        <template v-else>
           <el-button type="primary" size="large" @click="saveToDrive">保存整站到网盘</el-button>
        </template>
      </div>

      <!-- Folder contents -->
      <div v-if="fileInfo.isFolder" class="share-folder-content">
        <div class="share-breadcrumb">
          <el-breadcrumb separator="/">
            <el-breadcrumb-item @click="handleBreadcrumbClick(-1)">
              <span class="breadcrumb-link">{{ fileInfo.name }}</span>
               </el-breadcrumb-item>
            <el-breadcrumb-item 
              v-for="(item, index) in pathStack" 
              :key="item.id"
              @click="handleBreadcrumbClick(index)"
            >
              <span class="breadcrumb-link">{{ item.name }}</span>
            </el-breadcrumb-item>
          </el-breadcrumb>
        </div>

        <el-table :data="shareFileList" style="width: 100%" v-loading="loadingList">
          <el-table-column label="名称">
            <template #default="{ row }">
              <div class="file-name-cell" @click="handleRowClick(row)">
                <el-icon :size="24" :class="row.isFolder ? 'folder-icon' : 'file-icon'">
                  <FolderOpened v-if="row.isFolder" />
                  <component :is="getFileIcon(row.name)" v-else />
                </el-icon>
                <span class="name">{{ row.name }}</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="大小" width="120">
            <template #default="{ row }">
              <span v-if="!row.isFolder">{{ formatSize(row.fileSize) }}</span>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100">
            <template #default="{ row }">
              <el-button v-if="!row.isFolder" link :icon="Download" @click="handleDownload(row.id)"></el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Document, Download, FolderOpened } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'

const route = useRoute()
const router = useRouter()
const shareToken = route.params.token as string
const shareCode = ref('')
const loading = ref(false)
const fileInfo = ref<any>(null)
const shareFileList = ref<any[]>([])
const loadingList = ref(false)
const pathStack = ref<{id: number, name: string}[]>([])
const currentFolderId = ref<number | null>(null)

const isLoggedIn = computed(() => !!localStorage.getItem('token'))
const username = computed(() => localStorage.getItem('username'))

const goToLogin = () => {
  router.push({
    path: '/login',
    query: { redirect: route.fullPath }
  })
}

const checkCode = async () => {
  if (!shareCode.value) {
    ElMessage.warning('请输入提取码')
    return
  }

  loading.value = true
  try {
    await request.post('/share/check-code', {
      shareToken,
      shareCode: shareCode.value
    })
    
    // 验证通过，获取详情
    const res: any = await request.get(`/share/detail/${shareToken}`)
    fileInfo.value = res
    
    if (res.isFolder) {
      currentFolderId.value = null // Ensure we start at root
      fetchShareFileList()
    }
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const fetchShareFileList = async () => {
  loadingList.value = true
  try {
    const res: any = await request.get(`/share/list/${shareToken}`, {
      params: {
        code: shareCode.value,
        folderId: currentFolderId.value
      }
    })
    shareFileList.value = res
  } catch (error) {
    console.error(error)
  } finally {
    loadingList.value = false
  }
}

const handleRowClick = (row: any) => {
  if (row.isFolder) {
    pathStack.value.push({ id: row.id, name: row.name })
    currentFolderId.value = row.id
    fetchShareFileList()
  }
}

const handleBreadcrumbClick = (index: number) => {
  if (index === -1) {
    pathStack.value = []
    currentFolderId.value = null
  } else {
    const newStack = pathStack.value.slice(0, index + 1)
    pathStack.value = newStack
    currentFolderId.value = newStack[newStack.length - 1]?.id ?? null
  }
  fetchShareFileList()
}

const handleDownload = (id?: number) => {
  const baseURL = request.defaults.baseURL || '/api'
  
  if (!id) {
    // Downloading the root shared item
    window.open(`${baseURL}/share/download/${shareToken}?code=${shareCode.value}`, '_blank')
  } else {
    // Downloading a specific file within the shared folder
    window.open(`${baseURL}/share/download-file/${shareToken}?code=${shareCode.value}&fileId=${id}`, '_blank')
  }
}

const saveToDrive = async () => {
  if (!isLoggedIn.value) {
    ElMessageBox.confirm(
      '保存文件需要先登录，是否前往登录？',
      '提示',
      {
        confirmButtonText: '去登录',
        cancelButtonText: '取消',
        type: 'warning'
      }
    ).then(() => {
      goToLogin()
    }).catch(() => {})
    return
  }

  try {
    await request.post('/share/save', {
      shareToken,
      shareCode: shareCode.value,
      targetParentId: null // 默认保存到根目录
    })
    ElMessage.success('保存成功，正在跳转到我的网盘...')
    setTimeout(() => {
      router.push('/')
    }, 1000)
  } catch (error) {
    console.error(error)
  }
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

const getFileIcon = (name: string) => {
  const ext = name.split('.').pop()?.toLowerCase()
  switch (ext) {
    case 'pdf': return Document
    case 'docx':
    case 'doc': return Document
    case 'xlsx':
    case 'xls': return Document
    case 'ppt':
    case 'pptx': return Document
    case 'jpg':
    case 'jpeg':
    case 'png':
    case 'gif': return Document
    default: return Document
  }
}
</script>

<style scoped lang="scss">
.share-page {
  height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #000000;
  flex-direction: column;

  .top-bar {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 48px;
    box-sizing: border-box;
    background: #000000;
    border-bottom: 1px solid var(--pan-border);
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 24px;
    z-index: 100;

    .logo {
      display: flex;
      align-items: center;
      gap: 12px;
      font-size: 15px;
      font-weight: 800;
      color: var(--pan-text-main);
      
      .logo-icon {
        width: 24px;
        height: 24px;
        color: var(--pan-primary);
        display: flex;
        align-items: center;
        justify-content: center;
        
        svg {
          width: 100%;
          height: 100%;
        }
      }

      .logo-text {
        background: linear-gradient(135deg, #ffffff 0%, var(--pan-primary) 100%);
        background-clip: text;
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }
    }

    .user-actions {
      display: flex;
      align-items: center;
      gap: 15px;
      font-size: 14px;
      
      .login-tip {
        color: var(--pan-text-muted);
      }

      .username {
        color: var(--pan-text-main);
        font-weight: 500;
      }
    }
  }

  .share-box, .file-info-box {
    background: rgba(20, 20, 20, 0.6);
    backdrop-filter: var(--pan-glass-blur);
    padding: 40px;
    border-radius: var(--pan-radius-lg);
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5);
    border: 1px solid var(--pan-border);
    text-align: center;
    width: 500px;
    transition: width 0.3s ease;

    &.is-folder-view {
      width: 800px;
    }
  }

  .input-area {
    display: flex;
    justify-content: center;
    gap: 12px;
    margin-top: 24px;
    
    :deep(.el-input__wrapper) {
      background-color: rgba(255, 255, 255, 0.05) !important;
      border: 1px solid var(--pan-border) !important;
      box-shadow: none !important;
    }
  }

  .header {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-bottom: 30px;
    text-align: left;

    .icon {
      color: var(--pan-primary);
      filter: drop-shadow(0 0 15px var(--pan-primary-glow));
      
      &.folder-icon {
        color: #FCD34D;
        filter: drop-shadow(0 0 10px rgba(252, 211, 77, 0.3));
      }
      
      &.file-icon {
        color: var(--pan-accent);
        filter: drop-shadow(0 0 10px rgba(61, 155, 255, 0.3));
      }
    }

    h3 {
      margin: 0 0 10px 0;
      color: var(--pan-text-main);
      font-size: 20px;
    }

    p {
      color: var(--pan-text-muted);
      font-size: 13px;
      margin: 0;
      
      .divider {
        margin: 0 10px;
        color: var(--pan-border);
      }
    }
  }

  .share-folder-content {
    margin-top: 24px;
    text-align: left;

    .share-breadcrumb {
      margin-bottom: 16px;
      padding: 0 4px;

      .breadcrumb-link {
        cursor: pointer;
        color: var(--pan-text-muted);
        transition: var(--pan-transition);
        
        &:hover {
          color: var(--pan-primary);
        }
      }
    }

    /* Table adjustments */
    :deep(.el-table) {
      background-color: transparent !important;
      --el-table-bg-color: transparent;
      --el-table-tr-bg-color: transparent;
      --el-table-header-bg-color: rgba(255, 255, 255, 0.02);
      border: 1px solid var(--pan-border);
      border-radius: var(--pan-radius-sm);

      td, th {
        border-bottom: 1px solid var(--pan-border);
      }

      .file-name-cell {
        display: flex;
        align-items: center;
        gap: 12px;
        cursor: pointer;
        color: var(--pan-text-main);

        &:hover {
          color: var(--pan-primary);
        }

        .folder-icon { color: #FCD34D; }
        .file-icon { color: var(--pan-accent); }
      }
    }
  }
}
</style>
