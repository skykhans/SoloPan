<template>
  <div class="my-shares-container">
    <div class="action-bar">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link is-last">我的分享</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="buttons">
        <el-button
          type="danger"
          class="pan-button-danger"
          :icon="Delete"
          :disabled="selectedShareIds.length === 0"
          @click="handleBatchCancel"
        >
          批量取消分享
        </el-button>
      </div>
    </div>

    <div class="table-container">
      <el-table
        :data="sortedShareList"
        v-loading="loading"
        style="width: 100%"
        row-key="id"
        @selection-change="handleSelectionChange"
        @sort-change="handleSortChange"
      >
        <el-table-column type="selection" width="52" />
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
        <el-table-column label="分享时间" prop="createTime" sortable="custom" width="180">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.createTime) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="过期时间" prop="expireTime" sortable="custom" width="180">
          <template #default="{ row }">
            <span class="mono">{{ row.expireTime ? formatDate(row.expireTime) : '永久有效' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="浏览" prop="viewCount" sortable="custom" width="80">
          <template #default="{ row }">
            <span class="mono">{{ row.viewCount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="下载" prop="downloadCount" sortable="custom" width="80">
          <template #default="{ row }">
            <span class="mono">{{ row.downloadCount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <div class="row-actions">
              <el-tooltip content="复制链接" placement="top">
                <el-button link :icon="Link" @click="copyShareLink(row)" />
              </el-tooltip>
              <el-tooltip content="取消分享" placement="top">
                <el-button link type="danger" :icon="Delete" @click="handleCancelShare(row)" />
              </el-tooltip>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { FolderOpened, Document, Link, Delete } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const shareList = ref<any[]>([])
const selectedShareIds = ref<number[]>([])
const sortState = ref<{ prop: string; order: 'ascending' | 'descending' | null } | null>(null)

const sortedShareList = computed(() => {
  const list = [...shareList.value]
  if (!sortState.value?.order || !sortState.value.prop) return list

  const direction = sortState.value.order === 'ascending' ? 1 : -1
  const prop = sortState.value.prop

  const getSortValue = (item: any) => {
    if (prop === 'createTime') return item.createTime ? new Date(item.createTime).getTime() : 0
    if (prop === 'expireTime') return item.expireTime ? new Date(item.expireTime).getTime() : -1
    if (prop === 'viewCount') return Number(item.viewCount ?? 0)
    if (prop === 'downloadCount') return Number(item.downloadCount ?? 0)
    return item[prop]
  }

  return list.sort((a, b) => {
    const av = getSortValue(a)
    const bv = getSortValue(b)
    if (av < bv) return -1 * direction
    if (av > bv) return 1 * direction
    return (a.id - b.id) * direction
  })
})

const fetchShares = async () => {
  loading.value = true
  try {
    const res: any = await request.get('/share/my-shares')
    shareList.value = res
    selectedShareIds.value = []
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

const handleSelectionChange = (rows: any[]) => {
  selectedShareIds.value = rows.map(item => item.id).filter((id: number) => Number.isFinite(id))
}

const handleSortChange = (payload: { prop: string; order: 'ascending' | 'descending' | null }) => {
  sortState.value = payload?.order ? payload : null
}

const handleBatchCancel = async () => {
  if (selectedShareIds.value.length === 0) {
    ElMessage.warning('请先选择要取消的分享')
    return
  }

  try {
    await ElMessageBox.confirm(
      `确定要批量取消选中的 ${selectedShareIds.value.length} 条分享吗？取消后链接将失效。`,
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    const res: any = await request.post('/share/batch-cancel', { ids: selectedShareIds.value })
    ElMessage.success(res?.message || '批量取消成功')
    selectedShareIds.value = []
    fetchShares()
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error(error)
    }
  }
}

onMounted(fetchShares)
</script>

<style scoped lang="scss">
.my-shares-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: var(--pan-bg);
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

  .buttons {
    display: flex;
    align-items: center;
    gap: 8px;
  }
}

.table-container {
  flex: 1;
  overflow: hidden;
  background: var(--pan-surface-elevated);
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-lg);
  padding: 8px;
}

.file-name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
  
  .name {
    font-weight: 400;
    color: var(--pan-text-body);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .folder-icon { color: #facc15; }
  .file-icon { color: var(--pan-primary); }
}

.mono {
  font-family: var(--font-mono);
  font-size: 13px;
  color: var(--pan-text-body);
}

.row-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

:deep(.row-actions .el-button) {
  padding: 0 !important;
  height: 28px !important;
  width: 28px !important;
}
</style>
