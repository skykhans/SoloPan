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

    <div class="user-management">
      <h3>用户管理</h3>
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
            <span class="status-badge" :style="row.isAdmin ? 'color: #ef4444; background: rgba(239, 68, 68, 0.1); border-color: rgba(239, 68, 68, 0.2)' : ''">
              {{ row.isAdmin ? 'ADMIN' : 'USER' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button size="small" @click="handleEditQuota(row)">修改配额</el-button>
          </template>
        </el-table-column>
      </el-table>
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
const loading = ref(false)

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
})
</script>

<style scoped lang="scss">
.admin-panel {
  padding-top: 0; /* Removed padding */

  .header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 20px 0 50px; /* Match FileList */
    border-bottom: 1px solid var(--pan-border);
    height: 60px; /* Fixed height for stability */
    margin-bottom: 20px;

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
        }
      }
    }
  }

  .admin-content {
    padding: 0 20px; /* Add some padding for content */
  }

  .stats-cards {
    margin-bottom: 32px;
    
    .card-value {
      font-size: 28px;
      font-weight: 800;
      color: var(--pan-primary);
      margin-top: 8px;
      text-shadow: 0 0 10px var(--pan-primary-glow);
    }

    :deep(.el-card__header) {
      font-weight: 600;
      color: var(--pan-text-body);
      font-size: 14px;
      border-bottom: none;
      padding-bottom: 0;
    }
  }

  .user-management {
    padding: 32px;
    border-radius: var(--pan-radius-sm);
    border: 1px solid var(--pan-border);
    background: #050505;

    h3 {
       margin-top: 0;
       margin-bottom: 24px;
       font-size: 18px;
       font-weight: 700;
       color: var(--pan-text-main);
    }

    .usage-cell {
      white-space: nowrap;
    }
  }
}
</style>
