<template>
  <div class="admin-panel">
    <div class="header-actions">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link">系统管理</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>
    </div>
    
    <div class="admin-content">
      <el-row :gutter="20" class="stats-cards">
        <el-col :span="6">
          <el-card shadow="hover">
            <template #header>用户总数</template>
            <div class="card-value">{{ stats.totalUsers }}</div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <template #header>文件总数</template>
            <div class="card-value">{{ stats.totalFiles }}</div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <template #header>文件夹总数</template>
            <div class="card-value">{{ stats.totalFolders }}</div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card shadow="hover">
            <template #header>已用存储 (GB)</template>
            <div class="card-value">{{ stats.totalStorageUsedGB }}</div>
          </el-card>
        </el-col>
      </el-row>

    <div class="management-tabs">
      <el-tabs v-model="activeTab" class="pan-tabs">
        <el-tab-pane label="用户管理" name="users">
          <div class="user-management">
            <el-table :data="userList" style="width: 100%" v-loading="loading">
              <el-table-column label="ID" width="80">
                <template #default="{ row }">
                  <span class="mono">{{ row.id }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="userName" label="用户名" width="150" />
              <el-table-column prop="email" label="邮箱" width="200" />
              <el-table-column label="已用/总空间" min-width="180">
                <template #default="{ row }">
                  <span class="usage-cell mono">{{ formatSize(row.usedSpace) }} / {{ formatSize(row.totalSpace) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="注册时间" width="180">
                <template #default="{ row }">
                  <span class="mono">{{ formatDate(row.createTime) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="角色" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.isAdmin ? 'danger' : 'info'" size="small">
                    {{ row.isAdmin ? 'Admin' : 'User' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="150">
                <template #default="{ row }">
                  <el-button size="small" @click="handleEditQuota(row)">修改配额</el-button>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <el-tab-pane label="审计日志" name="audit">
          <div class="audit-management">
            <el-table :data="auditLogs" style="width: 100%" v-loading="loadingLogs">
              <el-table-column label="时间" width="180">
                <template #default="{ row }">
                  <span class="mono">{{ formatDate(row.createTime) }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="userName" label="用户" width="120" />
              <el-table-column label="操作" width="120">
                <template #default="{ row }">
                   <el-tag :type="getActionType(row.action)" size="small">{{ row.action }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="detail" label="详情" />
              <el-table-column prop="ipAddress" label="IP 地址" width="150">
                <template #default="{ row }">
                  <span class="mono">{{ row.ipAddress }}</span>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- 修改配额弹窗 -->
    <el-dialog v-model="showQuotaDialog" title="修改用户配额" width="400px">
      <el-form label-position="top">
        <el-form-item label="新的配额 (GB)">
          <el-input-number v-model="newQuotaGB" :min="1" :max="10240" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showQuotaDialog = false">取消</el-button>
        <el-button type="primary" @click="confirmEditQuota">确定</el-button>
      </template>
    </el-dialog>
    </div> 
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import request from '../utils/request'
import { ElMessage } from 'element-plus'

const stats = ref({
  totalUsers: 0,
  totalFiles: 0,
  totalFolders: 0,
  totalStorageUsedGB: 0
})

const userList = ref([])
const auditLogs = ref([])
const loading = ref(false)
const loadingLogs = ref(false)
const activeTab = ref('users')

const showQuotaDialog = ref(false)
const currentEditUser = ref<any>(null)
const newQuotaGB = ref(5)

const fetchStats = async () => {
  try {
    const res: any = await request.get('/admin/stats')
    stats.value = res
  } catch (error) {
    console.error(error)
  }
}

const fetchUsers = async () => {
  loading.value = true
  try {
    const res: any = await request.get('/admin/users')
    userList.value = res
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const fetchAuditLogs = async () => {
  loadingLogs.value = true
  try {
    const res: any = await request.get('/admin/audit-logs')
    auditLogs.value = res
  } catch (error) {
    console.error(error)
  } finally {
    loadingLogs.value = false
  }
}

const getActionType = (action: string) => {
  if (action.includes('删除')) return 'danger'
  if (action.includes('上传') || action.includes('合并')) return 'success'
  if (action.includes('移动') || action.includes('重命名')) return 'warning'
  return 'info'
}

const handleEditQuota = (row: any) => {
  currentEditUser.value = row
  newQuotaGB.value = Math.round(row.totalSpace / 1024 / 1024 / 1024)
  showQuotaDialog.value = true
}

const confirmEditQuota = async () => {
  if (!currentEditUser.value) return
  try {
    const bytes = newQuotaGB.value * 1024 * 1024 * 1024
    await request.put(`/admin/user-quota?userId=${currentEditUser.value.id}&newTotalSpaceBytes=${bytes}`)
    ElMessage.success('配额修改成功')
    showQuotaDialog.value = false
    fetchUsers()
  } catch (error) {
    console.error(error)
  }
}

const formatSize = (bytes: number) => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString()
}

onMounted(() => {
  fetchStats()
  fetchUsers()
  fetchAuditLogs()
})
</script>

<style scoped lang="scss">
.admin-panel {
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: var(--pan-bg);
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.header-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 0;
  margin-bottom: 8px;
  flex-shrink: 0;

  .breadcrumb {
    .breadcrumb-link {
      font-weight: 700;
      color: var(--pan-text-main);
      font-size: 14px;
    }
  }
}

.admin-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0; /* Important for flex child with overflow */
  padding-bottom: 24px;
}

.stats-cards {
  margin-bottom: 24px;
  flex-shrink: 0;
  
  :deep(.el-card) {
    background: var(--pan-surface-elevated);
    border: 1px solid var(--pan-border-strong);
    border-radius: var(--pan-radius-lg);
    transition: var(--pan-transition);
    
    &:hover {
      border-color: var(--pan-primary);
      background: rgba(16, 185, 129, 0.02);
      box-shadow: 0 8px 30px rgba(0, 0, 0, 0.4);
    }

    .el-card__header {
      border: none;
      padding: 16px 20px 0;
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--pan-text-muted);
    }

    .el-card__body {
      padding: 12px 20px 20px;
    }
  }

  .card-value {
    font-size: 28px;
    font-weight: 800;
    color: var(--pan-text-main);
    letter-spacing: -0.02em;
    position: relative;
    padding-bottom: 12px;
    
    &::after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 0;
      width: 24px;
      height: 3px;
      background: var(--pan-primary);
      border-radius: 2px;
    }
  }
}

.management-tabs {
  flex: 1;
  min-height: 0;
  background: var(--pan-surface-elevated);
  border: 1px solid var(--pan-border);
  border-radius: var(--pan-radius-lg);
  padding: 8px;
  display: flex;
  flex-direction: column;
  
  :deep(.pan-tabs) {
    height: 100%;
    display: flex;
    flex-direction: column;
    
    .el-tabs__content {
      flex: 1;
      overflow-y: auto;
      min-height: 0;
    }
    
    .el-tabs__header {
      margin: 0 0 16px;
      padding: 4px 12px 0;
      flex-shrink: 0;
    }
    .el-tabs__item {
      font-weight: 600;
      font-size: 14px;
      height: 44px;
      &.is-active { color: var(--pan-primary); }
    }
    .el-tabs__active-bar {
      height: 2px;
      border-radius: 2px;
    }
    .el-tabs__nav-wrap::after { background-color: var(--pan-border); }
  }
}

.user-management, .audit-management {
  padding: 0 12px 12px;
}

.usage-cell {
  color: var(--pan-text-body);
  font-size: 13px;
}
</style>
