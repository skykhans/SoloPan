<template>
  <div class="admin-panel">
    <h2>管理员后台</h2>
    
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
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="userName" label="用户名" width="150" />
        <el-table-column prop="email" label="邮箱" width="200" />
        <el-table-column label="已用/总空间">
          <template #default="{ row }">
            {{ formatSize(row.usedSpace) }} / {{ formatSize(row.totalSpace) }}
          </template>
        </el-table-column>
        <el-table-column label="注册时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column label="角色" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isAdmin ? 'danger' : 'info'">{{ row.isAdmin ? '管理员' : '用户' }}</el-tag>
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
  h2 {
    margin-bottom: 20px;
    padding-left: 35px; // 为左侧绝对定位的折叠按钮留出空间
  }

  .stats-cards {
    margin-bottom: 30px;
    
    .card-value {
      font-size: 24px;
      font-weight: bold;
      color: var(--pan-primary);
    }
  }

  .user-management {
    background: white;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);

    h3 {
      margin-top: 0;
      margin-bottom: 20px;
    }
  }
}
</style>
