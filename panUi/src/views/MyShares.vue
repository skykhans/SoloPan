<template>
  <div class="my-shares-container">
    <div class="header">
      <h2>我的分享</h2>
      <el-button @click="fetchShares" :icon="Refresh">刷新</el-button>
    </div>

    <div class="table-container" v-loading="loading">
      <el-table :data="shareList" style="width: 100%" height="100%">
        <el-table-column label="文件名" min-width="200">
          <template #default="{ row }">
            <div class="file-name">
              <el-icon :size="20" :class="row.isFolder ? 'folder-icon' : 'file-icon'">
                <FolderOpened v-if="row.isFolder" />
                <Document v-else />
              </el-icon>
              <span class="name" :title="row.fileName">{{ row.fileName }}</span>
            </div>
          </template>
        </el-table-column>
        
        <el-table-column label="提取码" width="100" prop="shareCode">
          <template #default="{ row }">
            <el-tag>{{ row.shareCode }}</el-tag>
          </template>
        </el-table-column>
        
        <el-table-column label="浏览/下载" width="120">
          <template #default="{ row }">
            {{ row.viewCount }} / {{ row.downloadCount }}
          </template>
        </el-table-column>

        <el-table-column label="过期时间" width="180">
          <template #default="{ row }">
            <span :class="{ 'expired': isExpired(row.expireTime) }">
              {{ row.expireTime ? formatDate(row.expireTime) : '永久有效' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column label="创建时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createTime) }}
          </template>
        </el-table-column>

        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
             <div class="actions-cell">
               <el-button link type="primary" @click="copyLink(row)">复制链接</el-button>
               <el-button link type="danger" @click="cancelShare(row)">取消分享</el-button>
             </div>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { FolderOpened, Document, Refresh } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const shareList = ref<any[]>([])

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

const cancelShare = (row: any) => {
  ElMessageBox.confirm(
    '确定要取消该分享吗？取消后链接将失效。',
    '提示',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.post(`/share/cancel/${row.id}`)
      ElMessage.success('取消分享成功')
      fetchShares()
    } catch (error) {
      console.error(error)
    }
  })
}

const copyLink = (row: any) => {
  const link = `${window.location.origin}/share/${row.shareToken}`
  const text = `文件分享：${row.fileName}\n链接：${link}\n提取码：${row.shareCode}`
  navigator.clipboard.writeText(text).then(() => {
    ElMessage.success('链接已复制到剪贴板')
  }).catch(() => {
    ElMessage.error('复制失败')
  })
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString()
}

const isExpired = (dateStr: string | null) => {
  if (!dateStr) return false
  return new Date(dateStr) < new Date()
}

onMounted(() => {
  fetchShares()
})
</script>

<style scoped lang="scss">
.my-shares-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  
  .header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    
    h2 {
      margin: 0;
      font-size: 20px;
      color: #303133;
    }
  }

  .table-container {
    flex: 1;
    overflow: hidden;
    background: white;
    border-radius: 4px;
  }

  .file-name {
    display: flex;
    align-items: center;
    gap: 8px;
    
    .folder-icon {
      color: #e6a23c;
    }
    
    .file-icon {
      color: #909399;
    }
    
    .name {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }

  .expired {
    color: #f56c6c;
  }

  .actions-cell {
    white-space: nowrap;
    display: flex;
    gap: 4px;
  }
}
</style>