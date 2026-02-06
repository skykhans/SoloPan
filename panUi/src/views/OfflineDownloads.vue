<template>
  <div class="offline-view">
    <div class="action-bar">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link is-last">离线下载</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="buttons">
        <el-button type="primary" class="pan-button-primary" :icon="Plus" @click="openCreate">新增任务</el-button>
        <el-button :icon="Refresh" @click="fetchTasks">刷新</el-button>
      </div>
    </div>

    <div class="table-container pan-card">
      <el-table :data="sortedTasks" style="width: 100%" height="100%" v-loading="loading" @sort-change="handleSortChange">
        <el-table-column label="链接" prop="url" min-width="280" sortable="custom">
          <template #default="{ row }">
            <span class="url-text" :title="row.url">{{ row.url }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" prop="status" width="120" sortable="custom">
          <template #default="{ row }">
            <span class="status-text">{{ statusText(row.status, row.message) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="进度" prop="progress" width="180" sortable="custom">
          <template #default="{ row }">
            <el-progress :percentage="row.progress || 0" :status="progressStatus(row.status)" :stroke-width="4" />
          </template>
        </el-table-column>
        <el-table-column label="创建时间" prop="createTime" width="180" sortable="custom">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.createTime) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="更新时间" prop="updateTime" width="180" sortable="custom">
          <template #default="{ row }">
            <span class="mono">{{ formatDate(row.updateTime) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <div class="row-actions">
              <el-tooltip content="编辑" placement="top">
                <el-button link :icon="Edit" @click="openEdit(row)" :disabled="isRunning(row.status)" />
              </el-tooltip>
              <el-tooltip content="重试" placement="top">
                <el-button link :icon="Refresh" @click="retryTask(row)" :disabled="isRunning(row.status)" />
              </el-tooltip>
              <el-tooltip content="删除" placement="top">
                <el-button link type="danger" :icon="Delete" @click="deleteTask(row)" :disabled="isRunning(row.status)" />
              </el-tooltip>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog v-model="showCreate" title="新建离线下载" width="520px">
      <el-form label-position="top">
        <el-form-item label="链接 (URL)">
          <el-input v-model="createForm.url" type="textarea" :rows="3" placeholder="支持 HTTP/HTTPS、magnet、ed2k、torrent" />
        </el-form-item>
        <el-form-item label="保存到目录">
          <div class="folder-picker-row">
            <el-input v-model="createParentLabel" disabled />
            <el-button :icon="FolderOpened" @click="openPicker('create')">选择目录</el-button>
            <el-button :icon="HomeFilled" @click="setParent(null, '根目录')">根目录</el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :icon="Close" @click="showCreate = false">取消</el-button>
        <el-button :icon="Plus" type="primary" class="pan-button-primary" @click="createTask" :loading="saving">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="showEdit" title="编辑离线下载" width="520px">
      <el-form label-position="top">
        <el-form-item label="链接 (URL)">
          <el-input v-model="editForm.url" type="textarea" :rows="3" placeholder="支持 HTTP/HTTPS、magnet、ed2k、torrent" />
        </el-form-item>
        <el-form-item label="保存到目录">
          <div class="folder-picker-row">
            <el-input v-model="editParentLabel" disabled />
            <el-button :icon="FolderOpened" @click="openPicker('edit')">选择目录</el-button>
            <el-button :icon="HomeFilled" @click="setParent(null, '根目录')">根目录</el-button>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button :icon="Close" @click="showEdit = false">取消</el-button>
        <el-button :icon="Refresh" type="primary" class="pan-button-primary" @click="updateTask" :loading="saving">保存并重试</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="pickerVisible" title="选择目录" width="520px">
      <div class="picker-breadcrumb">
        <span
          v-for="(item, idx) in pickerPathStack"
          :key="item.id ?? 'root'"
          class="crumb"
          @click="backPickerTo(idx)"
        >
          {{ item.name }}
        </span>
      </div>
      <div class="picker-list">
        <div class="picker-item root-item" @click="setParent(null, '根目录'); pickerVisible = false">
          <span>根目录</span>
        </div>
        <div
          v-for="folder in pickerFolders"
          :key="folder.id"
          class="picker-item"
        >
          <span class="name" @click="enterPickerFolder(folder)">{{ folder.name }}</span>
          <el-button link :icon="Check" @click="setParent(folder.id, folder.name); pickerVisible = false">选择</el-button>
        </div>
        <div v-if="pickerFolders.length === 0" class="empty-tip">暂无子文件夹</div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Refresh, Edit, Delete, FolderOpened, HomeFilled, Close, Check } from '@element-plus/icons-vue'
import request from '../utils/request'

const tasks = ref<any[]>([])
const sortState = ref<{ prop: string; order: 'ascending' | 'descending' | null } | null>(null)
const loading = ref(false)
const saving = ref(false)
const showCreate = ref(false)
const showEdit = ref(false)
let timer: any = null

const createForm = ref({ url: '', parentId: null as number | null })
const editForm = ref({ id: 0, url: '', parentId: null as number | null })
const createParentLabel = ref('根目录')
const editParentLabel = ref('根目录')
const pickerVisible = ref(false)
const pickerMode = ref<'create' | 'edit'>('create')
const pickerParentId = ref<number | null>(null)
const pickerFolders = ref<any[]>([])
const pickerPathStack = ref<{ id: number | null; name: string }[]>([])

const isRunning = (status: string) => status === 'downloading' || status === 'importing'

const statusText = (status: string, message?: string) => {
  switch (status) {
    case 'queued': return '排队中'
    case 'downloading': return '下载中'
    case 'importing': return '导入中'
    case 'completed': return '已完成'
    case 'failed': return message ? `失败: ${message}` : '失败'
    default: return status || '未知'
  }
}

const progressStatus = (status: string) => {
  if (status === 'failed') return 'exception'
  if (status === 'completed') return 'success'
  return ''
}

const sortedTasks = computed(() => {
  const list = [...tasks.value]
  if (!sortState.value?.order || !sortState.value.prop) return list

  const direction = sortState.value.order === 'ascending' ? 1 : -1
  const prop = sortState.value.prop

  const getSortValue = (item: any) => {
    if (prop === 'url') return item.url ?? ''
    if (prop === 'status') return item.status ?? ''
    if (prop === 'progress') return Number(item.progress ?? 0)
    if (prop === 'createTime') return item.createTime ? new Date(item.createTime).getTime() : 0
    if (prop === 'updateTime') return item.updateTime ? new Date(item.updateTime).getTime() : 0
    return item[prop]
  }

  return list.sort((a, b) => {
    const av = getSortValue(a)
    const bv = getSortValue(b)
    if (typeof av === 'string' || typeof bv === 'string') {
      const result = String(av).localeCompare(String(bv), 'zh-CN')
      if (result !== 0) return result * direction
    } else {
      if (av < bv) return -1 * direction
      if (av > bv) return 1 * direction
    }
    return (a.id - b.id) * direction
  })
})

const handleSortChange = (payload: { prop: string; order: 'ascending' | 'descending' | null }) => {
  sortState.value = payload?.order ? payload : null
}

const formatDate = (dateStr: string) => {
  return dateStr ? new Date(dateStr).toLocaleString() : '-'
}

const setParent = (id: number | null, label: string) => {
  if (pickerMode.value === 'create') {
    createForm.value.parentId = id
    createParentLabel.value = label
  } else {
    editForm.value.parentId = id
    editParentLabel.value = label
  }
}

const openPicker = async (mode: 'create' | 'edit') => {
  pickerMode.value = mode
  pickerParentId.value = null
  pickerPathStack.value = [{ id: null, name: '根目录' }]
  pickerVisible.value = true
  await fetchPickerFolders(null)
}

const fetchPickerFolders = async (parentId: number | null) => {
  try {
    const res: any = await request.get('/file/list', {
      params: { parentId },
      _showError: false
    })
    const list = Array.isArray(res?.items) ? res.items : (Array.isArray(res) ? res : [])
    pickerFolders.value = list.filter((i: any) => i.isFolder)
  } catch (error) {
    console.error(error)
  }
}

const enterPickerFolder = async (folder: any) => {
  pickerParentId.value = folder.id
  pickerPathStack.value.push({ id: folder.id, name: folder.name })
  await fetchPickerFolders(folder.id)
}

const backPickerTo = async (index: number) => {
  const target = pickerPathStack.value[index]
  if (!target) return
  pickerPathStack.value = pickerPathStack.value.slice(0, index + 1)
  pickerParentId.value = target.id
  await fetchPickerFolders(target.id)
}

const fetchTasks = async () => {
  loading.value = true
  try {
    const res: any = await request.get('/file/offline-tasks', { _showError: false })
    tasks.value = Array.isArray(res) ? res : []
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const openCreate = () => {
  createForm.value = { url: '', parentId: null }
  createParentLabel.value = '根目录'
  showCreate.value = true
}

const createTask = async () => {
  if (!createForm.value.url.trim()) {
    ElMessage.warning('请输入链接')
    return
  }
  saving.value = true
  try {
    await request.post('/file/offline-download', {
      url: createForm.value.url.trim(),
      parentId: createForm.value.parentId
    })
    ElMessage.success('已加入离线下载队列')
    showCreate.value = false
    fetchTasks()
  } catch (error) {
    console.error(error)
  } finally {
    saving.value = false
  }
}

const openEdit = (row: any) => {
  editForm.value = {
    id: row.id,
    url: row.url || '',
    parentId: row.parentId ?? null
  }
  editParentLabel.value = row.parentId ? `目录 #${row.parentId}` : '根目录'
  showEdit.value = true
}

const updateTask = async () => {
  if (!editForm.value.url.trim()) {
    ElMessage.warning('请输入链接')
    return
  }
  saving.value = true
  try {
    await request.put(`/file/offline-tasks/${editForm.value.id}`, {
      url: editForm.value.url.trim(),
      parentId: editForm.value.parentId
    })
    ElMessage.success('已更新并重新入队')
    showEdit.value = false
    fetchTasks()
  } catch (error) {
    console.error(error)
  } finally {
    saving.value = false
  }
}

const retryTask = async (row: any) => {
  try {
    await request.post(`/file/offline-tasks/${row.id}/retry`)
    ElMessage.success('已重新入队')
    fetchTasks()
  } catch (error) {
    console.error(error)
  }
}

const deleteTask = async (row: any) => {
  try {
    await ElMessageBox.confirm('确定要删除该任务吗？', '提示', { type: 'warning' })
    await request.delete(`/file/offline-tasks/${row.id}`)
    ElMessage.success('已删除')
    fetchTasks()
  } catch (error) {
    console.error(error)
  }
}

onMounted(() => {
  fetchTasks()
  timer = setInterval(fetchTasks, 3000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped lang="scss">
.offline-view {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 0;
  background-color: var(--pan-bg);
}

.action-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 60px;
  padding: 12px 0;
  margin-bottom: 12px;
  gap: 16px;

  .breadcrumb {
    flex: 1;
    min-width: 0;
    .breadcrumb-link {
      color: var(--pan-text-body);
      font-size: 15px;
      cursor: pointer;
      transition: var(--pan-transition);
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
  min-height: 0;
  background: var(--pan-surface-elevated);
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-lg);
  padding: 8px;
  overflow: hidden;
}

.row-actions {
  display: flex;
  gap: 12px;
  align-items: center;
}

:deep(.row-actions .el-button.is-disabled) {
  cursor: not-allowed !important;
  opacity: 0.4;
}

:deep(.row-actions .el-button.is-disabled:hover) {
  background-color: transparent !important;
  color: var(--pan-text-muted) !important;
}

.url-text {
  color: var(--pan-text-body);
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status-text {
  color: var(--pan-text-body);
}

.folder-picker-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.picker-breadcrumb {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
  color: var(--pan-text-muted);
  .crumb {
    cursor: pointer;
    &:hover { color: var(--pan-primary); }
  }
}

.picker-list {
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-md);
  padding: 8px 12px;
  max-height: 320px;
  overflow-y: auto;
  background: var(--pan-surface-elevated);
}

.picker-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 4px;
  border-bottom: 1px solid var(--pan-border);
  .name { cursor: pointer; color: var(--pan-text-main); }
  &:last-child { border-bottom: none; }
}

.picker-item.root-item {
  color: var(--pan-primary);
  font-weight: 600;
}

.empty-tip {
  padding: 12px 4px;
  color: var(--pan-text-muted);
}
</style>
