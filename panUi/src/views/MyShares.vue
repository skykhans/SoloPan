<template>
  <div class="my-shares-container">
    <div class="header-actions">
      <h2>我的分享</h2>
    </div>

    <div class="table-container">
      <el-table :data="shareList" v-loading="loading" style="width: 100%">
        <el-table-column label="分享文件" min-width="200">
          <template #default="{ row }">
            <div class="file-name-cell">
              <el-icon :size="24" :class="row.isFolder ? 'folder-icon' : 'file-icon'">
                <FolderOpened v-if="row.isFolder" />
                <component :is="getFileIcon(row.fileName)" v-else />
              </el-icon>
              <span class="name">{{ row.fileName }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="提取码" width="100">
          <template #default="{ row }">
            <span class="mono">{{ row.shareCode }}</span>
          </template>
        </el-table-column>
        <el-table-column label="分享时间" width="180">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.createTime) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="过期时间" width="180">
          <template #default="{ row }">
            <span class="mono">{{ row.expireTime ? formatDate(row.expireTime) : '永久有效' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="浏览" width="80">
          <template #default="{ row }">
            <span class="mono">{{ row.viewCount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="下载" width="80">
          <template #default="{ row }">
            <span class="mono">{{ row.downloadCount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="copyShareLink(row)">复制链接</el-button>
            <el-button link type="danger" @click="handleCancelShare(row)">取消分享</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { FolderOpened, Document, Share, Delete, Link } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const shareList = ref([])

const fetchShares = async () => {
  loading.value = true
  try {
    const res: any = await request.get('/share/my-shares')
    shareList.value = res
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '-'
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

const copyShareLink = (row: any) => {
  const url = `${window.location.origin}/share/${row.shareToken}`
  const text = `链接：${url}\n提取码：${row.shareCode}`
  navigator.clipboard.writeText(text)
  ElMessage.success('分享信息已复制')
}

const handleCancelShare = (row: any) => {
  ElMessageBox.confirm('确定要取消该分享吗？取消后链接将失效。', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await request.post(`/share/cancel/${row.id}`)
      ElMessage.success('已取消分享')
      fetchShares()
    } catch (error) {
      console.error(error)
    }
  }).catch(() => {})
}

onMounted(fetchShares)
</script>

<style scoped lang="scss">
.my-shares-container {
  padding: 0;
  
  .header-actions {
    margin-bottom: 24px;
    display: flex;
    justify-content: space-between;
    align-items: center;

    h2 {
      margin: 0;
      font-size: 24px;
      font-weight: 700;
      color: var(--pan-text-main);
    }
  }

  .table-container {
    background: rgba(255, 255, 255, 0.01);
    border: 1px solid var(--pan-border);
    border-radius: var(--pan-radius-lg);
    overflow: hidden;

    :deep(.el-table) {
      background: transparent;
      
      th.el-table__cell {
        background: rgba(255, 255, 255, 0.02);
        color: var(--pan-text-main);
        font-weight: 600;
        text-transform: uppercase;
        font-size: 12px;
        letter-spacing: 0.05em;
        padding: 16px 0;
      }

      td.el-table__cell {
        padding: 12px 0;
      }
    }
  }

  .file-name-cell {
    display: flex;
    align-items: center;
    gap: 12px;
    
    .folder-icon {
      color: #FCD34D;
      filter: drop-shadow(0 0 5px rgba(252, 211, 77, 0.3));
    }
    
    .file-icon {
      color: #3D9BFF;
      filter: drop-shadow(0 0 5px rgba(61, 155, 255, 0.3));
    }
    
    .name {
      color: var(--pan-text-main);
      font-weight: 500;
    }
  }
}
</style>
