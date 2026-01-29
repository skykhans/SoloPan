<template>
  <div class="share-page">
    <div class="share-box" v-if="!fileInfo">
      <h2>请输入提取码</h2>
      <div class="input-area">
        <el-input v-model="shareCode" placeholder="请输入4位提取码" maxlength="4" style="width: 200px" />
        <el-button type="primary" @click="checkCode" :loading="loading">提取文件</el-button>
      </div>
    </div>

    <div class="file-info-box" v-else>
      <div class="header">
        <el-icon :size="48" class="icon"><Document /></el-icon>
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
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { Document, Download } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage } from 'element-plus'

const route = useRoute()
const shareToken = route.params.token as string
const shareCode = ref('')
const loading = ref(false)
const fileInfo = ref<any>(null)

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
  // 实际下载逻辑，可能需要后端提供无需鉴权的下载接口，或者用 shareToken 换取临时下载链接
  ElMessage.info('开始下载...')
  // 这里简化处理，直接跳转到下载链接 (需要后端支持)
  // window.location.href = `http://localhost:5080/api/share/download/${shareToken}?code=${shareCode.value}`
}

const saveToDrive = () => {
  ElMessage.success('保存成功 (模拟)')
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
</script>

<style scoped lang="scss">
.share-page {
  height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #f5f7fa;

  .share-box, .file-info-box {
    background: white;
    padding: 40px;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
    text-align: center;
    width: 500px;
  }

  .input-area {
    display: flex;
    justify-content: center;
    gap: 10px;
    margin-top: 20px;
  }

  .header {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-bottom: 30px;
    text-align: left;

    .icon {
      color: var(--pan-primary);
    }

    h3 {
      margin: 0 0 10px 0;
    }

    p {
      color: #999;
      font-size: 13px;
      margin: 0;
      
      .divider {
        margin: 0 10px;
        color: #eee;
      }
    }
  }
}
</style>
