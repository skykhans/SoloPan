<template>
  <div class="my-shares-container">
    <div class="header-actions">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link">我的分享</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>
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
import { FolderOpened, Document } from '@element-plus/icons-vue'
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
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  
  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 20px 0 50px;
    border-bottom: 1px solid var(--pan-border);
    height: 60px;
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
        
        /* Default for single item is body color to match 'All Files' root */
        font-weight: 600;
        color: var(--pan-text-body);
        
        &.is-last {
          color: var(--pan-text-main) !important;
          font-weight: 600;
          cursor: default;
          /* Explicit size override if needed, though 13px from parent should apply */
          font-size: 13px;
        }
      }
    }
  }

  .table-container {
    flex: 1;
    min-height: 0;
    margin: 20px;
    background-color: #000000 !important;
    border: 1px solid var(--pan-border);
    border-radius: var(--pan-radius-sm);
    overflow: auto;
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
