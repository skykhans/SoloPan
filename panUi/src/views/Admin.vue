<template>
  <div class="admin-panel">
    <div class="action-bar">
      <div class="breadcrumb">
        <el-breadcrumb separator="/">
          <el-breadcrumb-item>
            <span class="breadcrumb-link is-last">系统管理</span>
          </el-breadcrumb-item>
        </el-breadcrumb>
      </div>
      <div class="buttons"></div>
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
          <div class="tab-panel">
            <div class="filter-card pan-card">
              <div class="filter-bar">
              <el-input v-model="userFilters.userName" placeholder="用户名" clearable @keyup.enter="handleUserSearch" />
              <el-input v-model="userFilters.email" placeholder="邮箱" clearable @keyup.enter="handleUserSearch" />
              <el-input v-model="userFilters.phone" placeholder="手机" clearable @keyup.enter="handleUserSearch" />
              <el-select v-model="userFilters.isAdmin" placeholder="角色" clearable>
                <el-option label="Admin" :value="true" />
                <el-option label="User" :value="false" />
              </el-select>
              <div class="filter-actions">
                <el-button type="primary" class="admin-btn query-btn" @click="handleUserSearch">查询</el-button>
                <el-button class="admin-btn reset-btn" @click="resetUserFilters">重置</el-button>
              </div>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table :data="userList" style="width: 100%" height="100%" v-loading="loading">
                <el-table-column label="ID" width="80">
                  <template #default="{ row }">
                    <span class="mono">{{ row.id }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="userName" label="用户名" width="150" />
                <el-table-column prop="email" label="邮箱" width="200" />
                <el-table-column label="手机" width="150">
                  <template #default="{ row }">
                    <span class="mono">{{ row.phone || '-' }}</span>
                  </template>
                </el-table-column>
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
                <el-table-column label="修改时间" width="180">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.updateTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="登录时间" width="180">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.lastLoginTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="角色" width="100">
                  <template #default="{ row }">
                    <el-tag :type="row.isAdmin ? 'danger' : 'info'" size="small">
                      {{ row.isAdmin ? 'Admin' : 'User' }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="220">
                  <template #default="{ row }">
                    <el-button class="admin-btn" @click="handleEditQuota(row)">修改配额</el-button>
                    <el-button class="admin-btn" type="warning" @click="handleEditPassword(row)">修改密码</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
            <div class="pagination-wrapper pan-card">
              <el-pagination
                v-model:current-page="userPage"
                v-model:page-size="userPageSize"
                :page-sizes="[20, 50, 100]"
                :total="userTotal"
                layout="total, sizes, prev, pager, next, jumper"
                @current-change="fetchUsers"
                @size-change="handleUserPageSizeChange"
              />
            </div>
          </div>
        </el-tab-pane>

        <el-tab-pane label="审计日志" name="audit">
          <div class="tab-panel">
            <div class="filter-card pan-card">
              <div class="filter-bar audit-filter-bar">
              <el-input v-model="auditFilters.userName" placeholder="用户" clearable @keyup.enter="handleAuditSearch" />
              <el-input v-model="auditFilters.action" placeholder="操作" clearable @keyup.enter="handleAuditSearch" />
              <el-input v-model="auditFilters.ipAddress" placeholder="IP 地址" clearable @keyup.enter="handleAuditSearch" />
              <el-input v-model="auditFilters.keyword" placeholder="详情关键词" clearable @keyup.enter="handleAuditSearch" />
              <el-date-picker
                v-model="auditFilters.timeRange"
                class="audit-time-range"
                type="datetimerange"
                range-separator="至"
                start-placeholder="开始时间"
                end-placeholder="结束时间"
                value-format="YYYY-MM-DDTHH:mm:ss"
              />
              <div class="filter-actions">
                <el-button type="primary" class="admin-btn query-btn" @click="handleAuditSearch">查询</el-button>
                <el-button class="admin-btn reset-btn" @click="resetAuditFilters">重置</el-button>
              </div>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table :data="auditLogs" style="width: 100%" height="100%" v-loading="loadingLogs">
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
            <div class="pagination-wrapper pan-card">
              <el-pagination
                v-model:current-page="auditPage"
                v-model:page-size="auditPageSize"
                :page-sizes="[20, 50, 100]"
                :total="auditTotal"
                layout="total, sizes, prev, pager, next, jumper"
                @current-change="fetchAuditLogs"
                @size-change="handleAuditPageSizeChange"
              />
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- 修改配额弹窗 -->
    <el-dialog v-model="showQuotaDialog" title="修改用户配额" width="400px" append-to-body class="quota-dialog">
      <el-form label-position="top" class="quota-form">
        <el-form-item label="新的配额 (GB)">
          <el-input-number v-model="newQuotaGB" :min="1" :max="10240" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="showQuotaDialog = false">取消</el-button>
          <el-button type="primary" @click="confirmEditQuota">确定</el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 修改密码弹窗 -->
    <el-dialog v-model="showPasswordDialog" title="修改用户密码" width="400px" append-to-body>
      <el-form label-width="80px" style="padding: 10px 20px">
        <el-form-item label="用户名">
          <el-input :model-value="currentPasswordEditUser?.userName || ''" disabled />
        </el-form-item>
        <el-form-item label="新密码">
          <el-input v-model="passwordForm.newPassword" type="password" show-password placeholder="请输入新密码" />
          <div class="input-tip">至少8位，需含字母和数字</div>
        </el-form-item>
        <el-form-item label="确认密码">
          <el-input v-model="passwordForm.confirmPassword" type="password" show-password placeholder="请再次输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="showPasswordDialog = false">取消</el-button>
          <el-button type="primary" :loading="updatingPassword" @click="confirmEditPassword">确定</el-button>
        </div>
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
const userPage = ref(1)
const userPageSize = ref(20)
const userTotal = ref(0)
const auditPage = ref(1)
const auditPageSize = ref(20)
const auditTotal = ref(0)
const userFilters = ref<{
  userName: string
  email: string
  phone: string
  isAdmin: boolean | null
}>({
  userName: '',
  email: '',
  phone: '',
  isAdmin: null
})
const auditFilters = ref<{
  userName: string
  action: string
  ipAddress: string
  keyword: string
  timeRange: [string, string] | []
}>({
  userName: '',
  action: '',
  ipAddress: '',
  keyword: '',
  timeRange: []
})

const showQuotaDialog = ref(false)
const showPasswordDialog = ref(false)
const currentEditUser = ref<any>(null)
const currentPasswordEditUser = ref<any>(null)
const newQuotaGB = ref(5)
const updatingPassword = ref(false)
const passwordForm = ref({
  newPassword: '',
  confirmPassword: ''
})

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
    const params: any = {
      page: userPage.value,
      pageSize: userPageSize.value
    }
    if (userFilters.value.userName) params.userName = userFilters.value.userName
    if (userFilters.value.email) params.email = userFilters.value.email
    if (userFilters.value.phone) params.phone = userFilters.value.phone
    if (userFilters.value.isAdmin !== null) params.isAdmin = userFilters.value.isAdmin

    const res: any = await request.get('/admin/users', {
      params
    })
    userList.value = Array.isArray(res?.items) ? res.items : []
    userTotal.value = Number(res?.total ?? 0)
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const fetchAuditLogs = async () => {
  loadingLogs.value = true
  try {
    const params: any = {
      page: auditPage.value,
      pageSize: auditPageSize.value
    }
    if (auditFilters.value.userName) params.userName = auditFilters.value.userName
    if (auditFilters.value.action) params.action = auditFilters.value.action
    if (auditFilters.value.ipAddress) params.ipAddress = auditFilters.value.ipAddress
    if (auditFilters.value.keyword) params.keyword = auditFilters.value.keyword
    if (auditFilters.value.timeRange.length === 2) {
      params.startTime = auditFilters.value.timeRange[0]
      params.endTime = auditFilters.value.timeRange[1]
    }

    const res: any = await request.get('/admin/audit-logs', {
      params
    })
    auditLogs.value = Array.isArray(res?.items) ? res.items : []
    auditTotal.value = Number(res?.total ?? 0)
  } catch (error) {
    console.error(error)
  } finally {
    loadingLogs.value = false
  }
}

const handleUserPageSizeChange = (size: number) => {
  userPageSize.value = size
  userPage.value = 1
  fetchUsers()
}

const handleAuditPageSizeChange = (size: number) => {
  auditPageSize.value = size
  auditPage.value = 1
  fetchAuditLogs()
}

const handleUserSearch = () => {
  userPage.value = 1
  fetchUsers()
}

const resetUserFilters = () => {
  userFilters.value = {
    userName: '',
    email: '',
    phone: '',
    isAdmin: null
  }
  userPage.value = 1
  fetchUsers()
}

const handleAuditSearch = () => {
  auditPage.value = 1
  fetchAuditLogs()
}

const resetAuditFilters = () => {
  auditFilters.value = {
    userName: '',
    action: '',
    ipAddress: '',
    keyword: '',
    timeRange: []
  }
  auditPage.value = 1
  fetchAuditLogs()
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

const handleEditPassword = (row: any) => {
  currentPasswordEditUser.value = row
  passwordForm.value.newPassword = ''
  passwordForm.value.confirmPassword = ''
  showPasswordDialog.value = true
}

const validatePassword = (pwd: string) => {
  if (pwd.length < 8) return '密码长度至少为 8 位'
  if (!/[a-zA-Z]/.test(pwd) || !/[0-9]/.test(pwd)) return '密码必须包含字母和数字'
  return ''
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

const confirmEditPassword = async () => {
  if (!currentPasswordEditUser.value) return
  if (!passwordForm.value.newPassword || !passwordForm.value.confirmPassword) {
    ElMessage.warning('请填写完整密码信息')
    return
  }

  const pwdError = validatePassword(passwordForm.value.newPassword)
  if (pwdError) {
    ElMessage.warning(pwdError)
    return
  }

  if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
    ElMessage.warning('两次输入的新密码不一致')
    return
  }

  updatingPassword.value = true
  try {
    await request.put('/admin/user-password', {
      userId: currentPasswordEditUser.value.id,
      newPassword: passwordForm.value.newPassword
    })
    ElMessage.success('用户密码修改成功')
    showPasswordDialog.value = false
  } catch (error) {
    console.error(error)
  } finally {
    updatingPassword.value = false
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
  if (!dateStr) return '-'
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

.admin-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0; /* Important for flex child with overflow */
  padding-bottom: 24px;
}

.stats-cards {
  margin-bottom: 14px;
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
      padding: 10px 14px 0;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--pan-text-muted);
    }

    .el-card__body {
      padding: 8px 14px 12px;
    }
  }

  .card-value {
    font-size: 22px;
    font-weight: 800;
    color: var(--pan-text-main);
    letter-spacing: -0.02em;
    position: relative;
    padding-bottom: 8px;
    
    &::after {
      content: '';
      position: absolute;
      bottom: 0;
      left: 0;
      width: 18px;
      height: 2px;
      background: var(--pan-primary);
      border-radius: 2px;
    }
  }
}

.management-tabs {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  
  :deep(.pan-tabs) {
    height: 100%;
    display: flex;
    flex-direction: column;
    
    .el-tabs__content {
      flex: 1;
      overflow: hidden;
      min-height: 0;
    }

    .el-tab-pane {
      height: 100%;
      min-height: 0;
    }
    
    .el-tabs__header {
      margin: 0 0 10px;
      padding: 0 2px;
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

.tab-panel {
  display: flex;
  flex-direction: column;
  gap: 10px;
  height: 100%;
  min-height: 0;
  overflow: hidden;
}

.filter-card {
  padding: 12px;
  flex-shrink: 0;
}

.filter-bar {
  display: grid;
  grid-template-columns: repeat(6, minmax(120px, 1fr));
  gap: 10px;
}

.filter-bar :deep(.el-input),
.filter-bar :deep(.el-select),
.filter-bar :deep(.el-date-editor) {
  width: 100%;
}

.filter-bar :deep(.el-input__wrapper),
.filter-bar :deep(.el-select__wrapper),
.filter-bar :deep(.el-date-editor.el-input__wrapper) {
  height: 32px;
  min-height: 32px;
  box-sizing: border-box;
}

.filter-bar :deep(.el-range-input),
.filter-bar :deep(.el-range-separator) {
  line-height: 30px;
}

.audit-filter-bar {
  grid-template-columns: repeat(4, minmax(120px, 1fr)) minmax(320px, 2fr) auto;
}

.audit-time-range {
  width: 100%;
}

.filter-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  align-items: center;
}

.admin-btn {
  min-width: 86px;
  height: 32px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
}

.query-btn,
.reset-btn {
  min-width: 86px;
}

.table-card {
  padding: 8px;
  flex: 1;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.usage-cell {
  color: var(--pan-text-body);
  font-size: 13px;
}

.pagination-wrapper {
  padding: 10px 12px;
  display: flex;
  justify-content: flex-end;
  margin-top: 6px;
  flex-shrink: 0;
}

:deep(.table-card .el-table th.el-table__cell) {
  background-color: transparent !important;
}

@media (max-width: 1280px) {
  .filter-bar {
    grid-template-columns: repeat(3, minmax(120px, 1fr));
  }

  .audit-filter-bar {
    grid-template-columns: repeat(3, minmax(120px, 1fr));
  }

  .audit-time-range {
    grid-column: span 2;
  }

  .filter-actions {
    justify-content: flex-start;
  }
}

@media (max-width: 768px) {
  .filter-bar {
    grid-template-columns: 1fr;
  }
}

.input-tip {
  font-size: 12px;
  color: var(--pan-text-muted);
  margin-top: 4px;
  white-space: nowrap;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

:deep(.quota-dialog .el-dialog__body) {
  padding: 18px 20px 8px !important;
}

:deep(.quota-dialog .el-form-item__label) {
  color: var(--pan-text-body);
  font-size: 13px;
}

:deep(.quota-dialog .el-input-number) {
  width: 170px;
}

:deep(.quota-dialog .el-input-number .el-input__wrapper) {
  background: rgba(255, 255, 255, 0.03) !important;
  border: 1px solid var(--pan-border) !important;
  box-shadow: none !important;
}

:deep(.quota-dialog .el-input-number .el-input__inner) {
  color: var(--pan-text-main) !important;
  font-family: var(--font-mono);
}

:deep(.quota-dialog .el-input-number__increase),
:deep(.quota-dialog .el-input-number__decrease) {
  background: rgba(255, 255, 255, 0.03) !important;
  border-color: var(--pan-border) !important;
  color: var(--pan-text-body) !important;
}

:deep(.quota-dialog .el-input-number__increase:hover),
:deep(.quota-dialog .el-input-number__decrease:hover) {
  color: var(--pan-primary) !important;
}
</style>
