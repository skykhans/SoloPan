<template>
  <div class="share-page">
    <div class="top-bar">
      <div class="logo">
        <img src="../assets/vue.svg" alt="logo" />
        <span>我的网盘</span>
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

    <div class="file-info-box" v-else>
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
        <el-button type="primary" size="large" :icon="Download" @click="handleDownload">下载文件</el-button>
        <el-button size="large" @click="saveToDrive">保存到我的网盘</el-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
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
    const res = await request.get(`/share/detail/${shareToken}`)
    fileInfo.value = res
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const handleDownload = () => {
  ElMessage.info('开始下载...')
  const baseURL = request.defaults.baseURL || '/api'
  const link = document.createElement('a')
  link.href = `${baseURL}/share/download/${shareToken}?code=${shareCode.value}`
  link.target = '_blank' // open in new tab to avoid disrupting current page
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
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
    ElMessage.success('保存成功，请到我的网盘查看')
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
      gap: 10px;
      font-size: 14px;
      font-weight: 800;
      color: var(--pan-text-main);
      
      img {
        width: 24px;
        height: 24px;
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
}
</style>
