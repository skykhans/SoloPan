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
    <div class="management-tabs">
      <el-tabs v-model="activeTab" class="pan-tabs">
        <el-tab-pane label="概览" name="overview">
          <div class="tab-panel">
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
          </div>
        </el-tab-pane>

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
              <el-select v-model="userFilters.isEnabled" placeholder="状态" clearable>
                <el-option label="启用" :value="true" />
                <el-option label="禁用" :value="false" />
              </el-select>
              <div class="filter-actions">
                <el-button type="primary" :icon="Search" class="admin-btn query-btn" @click="handleUserSearch">查询</el-button>
                <el-button :icon="RefreshLeft" class="admin-btn reset-btn" @click="resetUserFilters">重置</el-button>
              </div>
              </div>
              <div class="batch-actions">
                <span class="batch-count">已选 {{ selectedUserIds.length }} 项</span>
                <el-button :icon="Check" class="admin-btn" :disabled="selectedUserIds.length === 0" @click="handleBatchEnable">批量启用</el-button>
                <el-button :icon="Close" class="admin-btn" :disabled="selectedUserIds.length === 0" @click="handleBatchDisable">批量禁用</el-button>
                <el-button :icon="Edit" class="admin-btn" :disabled="selectedUserIds.length === 0" @click="openBatchQuotaDialog">批量修改配额</el-button>
                <el-button :icon="Upload" class="admin-btn" :disabled="selectedUserIds.length === 0" @click="openBatchUploadLimitDialog">批量上传上限</el-button>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table :data="userList" style="width: 100%" height="calc(100% - 52px)" v-loading="loading" @sort-change="handleUserSortChange" @selection-change="handleUserSelectionChange">
                <el-table-column type="selection" width="52" fixed="left" />
                <el-table-column label="ID" prop="id" sortable="custom" width="80" fixed="left">
                  <template #default="{ row }">
                    <span class="mono">{{ row.id }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="userName" label="用户名" width="150" sortable="custom" fixed="left" />
                <el-table-column prop="email" label="邮箱" width="200" sortable="custom" fixed="left" />
                <el-table-column label="手机" prop="phone" width="150" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ row.phone || '-' }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="已用/总空间" prop="usedSpace" min-width="180" sortable="custom">
                  <template #default="{ row }">
                    <span class="usage-cell mono">{{ formatSize(row.usedSpace) }} / {{ formatSize(row.totalSpace) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="注册时间" prop="createTime" width="180" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.createTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="修改时间" prop="updateTime" width="180" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.updateTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="登录时间" prop="lastLoginTime" width="180" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.lastLoginTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="登录IP" prop="lastLoginIp" width="150" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ row.lastLoginIp || '-' }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="单文件上限" prop="maxUploadFileSize" width="150" sortable="custom" align="right" header-align="right">
                  <template #default="{ row }">
                    <span class="mono upload-limit-cell">{{ formatSize(row.maxUploadFileSize || 100 * 1024 * 1024) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="角色" prop="isAdmin" width="100" sortable="custom">
                  <template #default="{ row }">
                    <el-tag :type="row.isAdmin ? 'danger' : 'info'" size="small">
                      {{ row.isAdmin ? 'Admin' : 'User' }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="状态" prop="isEnabled" width="100" sortable="custom">
                  <template #default="{ row }">
                    <el-tag :type="row.isEnabled ? 'success' : 'danger'" size="small">
                      {{ row.isEnabled ? '启用' : '禁用' }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="160" fixed="right">
                  <template #default="{ row }">
                    <div class="admin-icon-actions">
                      <el-tooltip content="修改配额" placement="top">
                        <el-button :icon="Edit" class="admin-btn icon-action-btn" @click="handleEditQuota(row)" />
                      </el-tooltip>
                      <el-tooltip content="修改上传上限" placement="top">
                        <el-button :icon="Upload" class="admin-btn icon-action-btn" @click="handleEditUploadLimit(row)" />
                      </el-tooltip>
                      <el-tooltip content="修改密码" placement="top">
                        <el-button :icon="Key" class="admin-btn icon-action-btn" type="warning" @click="handleEditPassword(row)" />
                      </el-tooltip>
                    </div>
                  </template>
                </el-table-column>
              </el-table>
              <div class="table-pagination">
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
                <el-button type="primary" :icon="Search" class="admin-btn query-btn" @click="handleAuditSearch">查询</el-button>
                <el-button :icon="RefreshLeft" class="admin-btn reset-btn" @click="resetAuditFilters">重置</el-button>
              </div>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table :data="auditLogs" style="width: 100%" height="calc(100% - 52px)" v-loading="loadingLogs" @sort-change="handleAuditSortChange">
                <el-table-column label="时间" prop="createTime" width="180" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.createTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column prop="userName" label="用户" width="120" sortable="custom" />
                <el-table-column label="操作" prop="action" width="120" sortable="custom">
                  <template #default="{ row }">
                     <el-tag :type="getActionType(row.action)" size="small">{{ row.action }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="detail" label="详情" sortable="custom" />
                <el-table-column prop="ipAddress" label="IP 地址" width="150" sortable="custom">
                  <template #default="{ row }">
                    <span class="mono">{{ row.ipAddress }}</span>
                  </template>
                </el-table-column>
              </el-table>
              <div class="table-pagination">
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
          </div>
        </el-tab-pane>

        <el-tab-pane label="登录限制" name="ipLimit">
          <div class="tab-panel">
            <div class="filter-card pan-card">
              <div class="ip-section-title">普通用户登录限制（黑名单）</div>
              <div class="ip-rule-form">
                <el-input
                  v-model="ipRuleForm.ruleText"
                  placeholder="输入IP、CIDR或IP段，例如 192.168.1.10 / 192.168.1.0/24 / 10.0.0.1-10.0.0.100"
                  clearable
                />
                <el-input v-model="ipRuleForm.remark" placeholder="备注（可选）" clearable />
                <el-switch v-model="ipRuleForm.isEnabled" active-text="启用" inactive-text="停用" />
                <el-button type="primary" :icon="Plus" class="admin-btn query-btn" @click="handleCreateIpRule">新增限制</el-button>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table :data="loginIpRules" style="width: 100%" height="100%" v-loading="loadingIpRules">
                <el-table-column label="ID" prop="id" width="90" />
                <el-table-column label="IP规则" prop="ruleText" min-width="260" />
                <el-table-column label="状态" prop="isEnabled" width="180">
                  <template #default="{ row }">
                    <div class="ip-rule-status-cell">
                      <el-switch
                        :model-value="row.isEnabled"
                        @change="(val: boolean) => handleToggleIpRuleStatus(row, val)"
                      />
                      <el-tag size="small" :type="row.isEnabled ? 'success' : 'info'">
                        {{ row.isEnabled ? '启用' : '停用' }}
                      </el-tag>
                    </div>
                  </template>
                </el-table-column>
                <el-table-column label="备注" prop="remark" min-width="180">
                  <template #default="{ row }">
                    <span>{{ row.remark || '-' }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="创建时间" prop="createTime" width="180">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.createTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="120" fixed="right">
                  <template #default="{ row }">
                    <el-button :icon="Delete" class="admin-btn" type="danger" @click="handleDeleteIpRule(row)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <div class="filter-card pan-card">
              <div class="ip-section-title">管理员登录限制（白名单，可多条）</div>
              <div class="ip-rule-form">
                <el-input
                  v-model="adminIpRuleForm.ruleText"
                  placeholder="输入IP、CIDR或IP段，例如 192.168.1.10 / 192.168.1.0/24 / 10.0.0.1-10.0.0.100"
                  clearable
                />
                <el-input v-model="adminIpRuleForm.remark" placeholder="备注（可选）" clearable />
                <el-switch v-model="adminIpRuleForm.isEnabled" active-text="启用" inactive-text="停用" />
                <el-button type="primary" :icon="Plus" class="admin-btn query-btn" @click="handleCreateAdminIpRule">新增白名单</el-button>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table :data="adminLoginIpRules" style="width: 100%" height="100%" v-loading="loadingAdminIpRules">
                <el-table-column label="ID" prop="id" width="90" />
                <el-table-column label="IP规则" prop="ruleText" min-width="260" />
                <el-table-column label="状态" prop="isEnabled" width="180">
                  <template #default="{ row }">
                    <div class="ip-rule-status-cell">
                      <el-switch
                        :model-value="row.isEnabled"
                        @change="(val: boolean) => handleToggleAdminIpRuleStatus(row, val)"
                      />
                      <el-tag size="small" :type="row.isEnabled ? 'success' : 'info'">
                        {{ row.isEnabled ? '启用' : '停用' }}
                      </el-tag>
                    </div>
                  </template>
                </el-table-column>
                <el-table-column label="备注" prop="remark" min-width="180">
                  <template #default="{ row }">
                    <span>{{ row.remark || '-' }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="创建时间" prop="createTime" width="180">
                  <template #default="{ row }">
                    <span class="mono">{{ formatDate(row.createTime) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="120" fixed="right">
                  <template #default="{ row }">
                    <el-button :icon="Delete" class="admin-btn" type="danger" @click="handleDeleteAdminIpRule(row)">删除</el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </div>
        </el-tab-pane>

        <el-tab-pane label="文件管理" name="files">
          <div class="tab-panel">
            <div class="filter-card pan-card">
              <div class="filter-bar admin-file-filter-bar">
                <el-input v-model="fileFilters.userName" placeholder="用户名" clearable @keyup.enter="handleFileSearch" />
                <el-input v-model="fileFilters.name" placeholder="文件名" clearable @keyup.enter="handleFileSearch" />
                <el-select v-model="fileFilters.isFolder" placeholder="类型" clearable>
                  <el-option label="文件夹" :value="true" />
                  <el-option label="文件" :value="false" />
                </el-select>
                <el-select v-model="fileFilters.isDeleted" placeholder="状态" clearable>
                  <el-option label="正常" :value="false" />
                  <el-option label="已删除" :value="true" />
                </el-select>
                <el-select v-model="fileFilters.isShared" placeholder="是否分享" clearable>
                  <el-option label="已分享" :value="true" />
                  <el-option label="未分享" :value="false" />
                </el-select>
                <div class="filter-actions">
                  <el-button type="primary" :icon="Search" class="admin-btn query-btn" @click="handleFileSearch">查询</el-button>
                  <el-button :icon="RefreshLeft" class="admin-btn reset-btn" @click="resetFileFilters">重置</el-button>
                </div>
              </div>
              <div class="batch-actions">
                <span class="batch-count">已选 {{ selectedFileIds.length }} 项</span>
                <el-button :icon="FolderAdd" class="admin-btn" @click="showCreateFolderDialog = true">新增文件夹</el-button>
                <el-button :icon="Delete" class="admin-btn" :disabled="selectedFileIds.length === 0" @click="handleBatchDeleteFiles">批量删除</el-button>
                <el-button :icon="DeleteFilled" class="admin-btn" :disabled="selectedFileIds.length === 0" @click="handleBatchPermanentDeleteFiles">批量彻底删除</el-button>
                <el-button :icon="CircleClose" class="admin-btn" :disabled="selectedFileIds.length === 0" @click="handleBatchCancelShares">批量取消分享</el-button>
              </div>
            </div>
            <div class="table-card pan-card">
              <el-table
                :data="adminFiles"
                style="width: 100%"
                height="calc(100% - 52px)"
                v-loading="loadingFiles"
                @sort-change="handleFileSortChange"
                @selection-change="handleFileSelectionChange"
              >
                <el-table-column type="selection" width="52" fixed="left" />
                <el-table-column label="ID" prop="id" width="90" sortable="custom" fixed="left">
                  <template #default="{ row }"><span class="mono">{{ row.id }}</span></template>
                </el-table-column>
                <el-table-column label="名称" prop="name" min-width="220" sortable="custom" fixed="left">
                  <template #default="{ row }">
                    <div class="admin-file-name-cell">
                      <span>{{ row.name }}</span>
                      <el-tooltip content="已分享" placement="top" v-if="isRowShared(row)">
                        <el-icon class="share-status-icon"><Share /></el-icon>
                      </el-tooltip>
                    </div>
                  </template>
                </el-table-column>
                <el-table-column label="所属用户" prop="userName" width="150" sortable="custom" />
                <el-table-column label="类型" prop="isFolder" width="110" sortable="custom">
                  <template #default="{ row }">{{ row.isFolder ? '文件夹' : '文件' }}</template>
                </el-table-column>
                <el-table-column label="大小" prop="fileSize" width="140" sortable="custom" align="right" header-align="right">
                  <template #default="{ row }"><span class="mono upload-limit-cell">{{ row.isFolder ? '-' : formatSize(row.fileSize || 0) }}</span></template>
                </el-table-column>
                <el-table-column label="状态" prop="isDeleted" width="110" sortable="custom">
                  <template #default="{ row }">
                    <el-tag size="small" :type="row.isDeleted ? 'warning' : 'success'">{{ row.isDeleted ? '已删除' : '正常' }}</el-tag>
                  </template>
                </el-table-column>
                <el-table-column label="分享时间" prop="shareTime" width="180" sortable="custom">
                  <template #default="{ row }"><span class="mono">{{ formatDate(row.shareTime) }}</span></template>
                </el-table-column>
                <el-table-column label="创建时间" prop="createTime" width="180" sortable="custom">
                  <template #default="{ row }"><span class="mono">{{ formatDate(row.createTime) }}</span></template>
                </el-table-column>
                <el-table-column label="修改时间" prop="updateTime" width="180" sortable="custom">
                  <template #default="{ row }"><span class="mono">{{ formatDate(row.updateTime) }}</span></template>
                </el-table-column>
                <el-table-column label="删除时间" prop="deleteTime" width="180" sortable="custom">
                  <template #default="{ row }"><span class="mono">{{ formatDate(row.deleteTime) }}</span></template>
                </el-table-column>
                <el-table-column label="操作" width="280" fixed="right">
                  <template #default="{ row }">
                    <div class="admin-icon-actions">
                      <el-tooltip v-if="isRowShared(row)" content="查看分享链接" placement="top">
                        <el-button :icon="Link" class="admin-btn icon-action-btn" @click="handleViewShareInfo(row)" />
                      </el-tooltip>
                      <el-tooltip v-if="isRowShared(row)" content="取消分享" placement="top">
                        <el-button :icon="CircleClose" class="admin-btn icon-action-btn" @click="handleCancelShare(row)" />
                      </el-tooltip>
                      <el-tooltip content="重命名" placement="top">
                        <el-button :icon="Edit" class="admin-btn icon-action-btn" @click="handleRenameFile(row)" />
                      </el-tooltip>
                      <el-tooltip content="删除" placement="top">
                        <el-button :icon="Delete" class="admin-btn icon-action-btn" @click="handleDeleteFile(row)" />
                      </el-tooltip>
                      <el-tooltip content="彻底删除" placement="top">
                        <el-button :icon="DeleteFilled" class="admin-btn icon-action-btn" type="danger" @click="handlePermanentDeleteFile(row)" />
                      </el-tooltip>
                    </div>
                  </template>
                </el-table-column>
              </el-table>
              <div class="table-pagination">
                <el-pagination
                  v-model:current-page="filePage"
                  v-model:page-size="filePageSize"
                  :page-sizes="[20, 50, 100]"
                  :total="fileTotal"
                  layout="total, sizes, prev, pager, next, jumper"
                  @current-change="fetchAdminFiles"
                  @size-change="handleFilePageSizeChange"
                />
              </div>
            </div>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- 修改配额弹窗 -->
    <el-dialog v-model="showQuotaDialog" :title="isBatchQuotaMode ? '批量修改用户配额' : '修改用户配额'" width="400px" append-to-body class="quota-dialog">
      <el-form label-position="top" class="quota-form">
        <el-form-item label="新的配额 (GB)">
          <el-input-number v-model="newQuotaGB" :min="1" :max="10240" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="Close" @click="showQuotaDialog = false">取消</el-button>
          <el-button :icon="Check" type="primary" @click="confirmEditQuota">{{ isBatchQuotaMode ? '批量确定' : '确定' }}</el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 修改上传上限弹窗 -->
    <el-dialog v-model="showUploadLimitDialog" :title="isBatchUploadLimitMode ? '批量修改单文件上传上限' : '修改单文件上传上限'" width="420px" append-to-body class="quota-dialog">
      <el-form label-position="top" class="quota-form">
        <el-form-item label="新的单文件上传上限 (MB)">
          <el-input-number v-model="newUploadLimitMB" :min="1" :max="10240" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="Close" @click="showUploadLimitDialog = false">取消</el-button>
          <el-button :icon="Check" type="primary" @click="confirmEditUploadLimit">{{ isBatchUploadLimitMode ? '批量确定' : '确定' }}</el-button>
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
          <el-button :icon="Close" @click="showPasswordDialog = false">取消</el-button>
          <el-button :icon="Check" type="primary" :loading="updatingPassword" @click="confirmEditPassword">确定</el-button>
        </div>
      </template>
    </el-dialog>

    <el-dialog v-model="showRenameFileDialog" title="重命名文件" width="420px" append-to-body class="quota-dialog">
      <el-form label-position="top" class="quota-form">
        <el-form-item label="新名称">
          <el-input v-model="renameFileForm.newName" maxlength="255" show-word-limit placeholder="请输入新名称" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="Close" @click="showRenameFileDialog = false">取消</el-button>
          <el-button :icon="Check" type="primary" @click="confirmRenameFile">确定</el-button>
        </div>
      </template>
    </el-dialog>

    <el-dialog v-model="showCreateFolderDialog" title="新增文件夹" width="460px" append-to-body class="quota-dialog">
      <el-form label-position="top" class="quota-form">
        <el-form-item label="用户ID">
          <el-input-number v-model="createFolderForm.userId" :min="1" />
        </el-form-item>
        <el-form-item label="文件夹名称">
          <el-input v-model="createFolderForm.name" maxlength="255" placeholder="请输入文件夹名称" />
        </el-form-item>
        <el-form-item label="父目录ID（可选）">
          <el-input-number v-model="createFolderForm.parentId" :min="1" controls-position="right" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="Close" @click="showCreateFolderDialog = false">取消</el-button>
          <el-button :icon="Check" type="primary" @click="confirmCreateFolder">确定</el-button>
        </div>
      </template>
    </el-dialog>

    <el-dialog v-model="showShareInfoDialog" title="分享信息" width="520px" append-to-body class="quota-dialog">
      <el-form label-position="top" class="quota-form">
        <el-form-item label="分享链接">
          <el-input :model-value="shareInfo.link" readonly />
        </el-form-item>
        <el-form-item label="提取码">
          <el-input :model-value="shareInfo.shareCode" readonly />
        </el-form-item>
        <el-form-item label="有效期至">
          <el-input :model-value="formatDate(shareInfo.expireTime)" readonly />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="CopyDocument" @click="copyText(shareInfo.link)">复制链接</el-button>
          <el-button :icon="CopyDocument" @click="copyText(shareInfo.shareCode)">复制提取码</el-button>
          <el-button :icon="Close" @click="showShareInfoDialog = false">关闭</el-button>
        </div>
      </template>
    </el-dialog>
    </div> 
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import request from '../utils/request'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, RefreshLeft, Check, Close, Edit, Upload, Key, Plus, Delete, FolderAdd, DeleteFilled, Link, CircleClose, CopyDocument, Share } from '@element-plus/icons-vue'

const stats = ref({
  totalUsers: 0,
  totalFiles: 0,
  totalFolders: 0,
  totalStorageUsedGB: 0
})

const userList = ref([])
const auditLogs = ref([])
const loginIpRules = ref<any[]>([])
const adminLoginIpRules = ref<any[]>([])
const adminFiles = ref<any[]>([])
const loading = ref(false)
const loadingLogs = ref(false)
const loadingIpRules = ref(false)
const loadingAdminIpRules = ref(false)
const loadingFiles = ref(false)
const activeTab = ref('overview')
const userPage = ref(1)
const userPageSize = ref(20)
const userTotal = ref(0)
const userSortState = ref<{ prop: string; order: 'ascending' | 'descending' | null } | null>(null)
const auditPage = ref(1)
const auditPageSize = ref(20)
const auditTotal = ref(0)
const auditSortState = ref<{ prop: string; order: 'ascending' | 'descending' | null } | null>(null)
const filePage = ref(1)
const filePageSize = ref(20)
const fileTotal = ref(0)
const fileSortState = ref<{ prop: string; order: 'ascending' | 'descending' | null } | null>(null)
const userFilters = ref<{
  userName: string
  email: string
  phone: string
  isAdmin: boolean | null
  isEnabled: boolean | null
}>({
  userName: '',
  email: '',
  phone: '',
  isAdmin: null,
  isEnabled: null
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
const fileFilters = ref<{
  userName: string
  name: string
  isFolder: boolean | null
  isDeleted: boolean | null
  isShared: boolean | null
}>({
  userName: '',
  name: '',
  isFolder: null,
  isDeleted: false,
  isShared: null
})

const showQuotaDialog = ref(false)
const showUploadLimitDialog = ref(false)
const showPasswordDialog = ref(false)
const showRenameFileDialog = ref(false)
const showCreateFolderDialog = ref(false)
const showShareInfoDialog = ref(false)
const selectedUserIds = ref<number[]>([])
const selectedFileIds = ref<number[]>([])
const isBatchQuotaMode = ref(false)
const isBatchUploadLimitMode = ref(false)
const currentEditUser = ref<any>(null)
const currentPasswordEditUser = ref<any>(null)
const currentEditFile = ref<any>(null)
const newQuotaGB = ref(5)
const newUploadLimitMB = ref(100)
const updatingPassword = ref(false)
const passwordForm = ref({
  newPassword: '',
  confirmPassword: ''
})
const renameFileForm = ref({
  newName: ''
})
const createFolderForm = ref<{
  userId: number
  name: string
  parentId: number | null
}>({
  userId: 1,
  name: '',
  parentId: null
})
const shareInfo = ref<{
  link: string
  shareCode: string
  expireTime: string | null
}>({
  link: '',
  shareCode: '',
  expireTime: null
})
const ipRuleForm = ref({
  ruleText: '',
  remark: '',
  isEnabled: true
})
const adminIpRuleForm = ref({
  ruleText: '',
  remark: '',
  isEnabled: true
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
    if (userFilters.value.isEnabled !== null) params.isEnabled = userFilters.value.isEnabled
    if (userSortState.value?.prop && userSortState.value?.order) {
      params.sortBy = userSortState.value.prop
      params.order = userSortState.value.order === 'ascending' ? 'asc' : 'desc'
    }

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
    if (auditSortState.value?.prop && auditSortState.value?.order) {
      params.sortBy = auditSortState.value.prop
      params.order = auditSortState.value.order === 'ascending' ? 'asc' : 'desc'
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

const fetchLoginIpRules = async () => {
  loadingIpRules.value = true
  try {
    const res: any = await request.get('/admin/login-ip-rules')
    loginIpRules.value = Array.isArray(res) ? res : []
  } catch (error) {
    console.error(error)
  } finally {
    loadingIpRules.value = false
  }
}

const fetchAdminLoginIpRules = async () => {
  loadingAdminIpRules.value = true
  try {
    const res: any = await request.get('/admin/admin-login-ip-rules')
    adminLoginIpRules.value = Array.isArray(res) ? res : []
  } catch (error) {
    console.error(error)
  } finally {
    loadingAdminIpRules.value = false
  }
}

const fetchAdminFiles = async () => {
  loadingFiles.value = true
  try {
    const params: any = {
      page: filePage.value,
      pageSize: filePageSize.value
    }
    if (fileFilters.value.userName) params.userName = fileFilters.value.userName
    if (fileFilters.value.name) params.name = fileFilters.value.name
    if (fileFilters.value.isFolder !== null) params.isFolder = fileFilters.value.isFolder
    if (fileFilters.value.isDeleted !== null) params.isDeleted = fileFilters.value.isDeleted
    if (fileFilters.value.isShared !== null) params.isShared = fileFilters.value.isShared
    if (fileSortState.value?.prop && fileSortState.value?.order) {
      params.sortBy = fileSortState.value.prop
      params.order = fileSortState.value.order === 'ascending' ? 'asc' : 'desc'
    }

    const res: any = await request.get('/admin/files', { params })
    adminFiles.value = Array.isArray(res?.items)
      ? res.items.map((item: any) => ({
          ...item,
          isShared: Boolean(item?.isShared || item?.shareToken)
        }))
      : []
    fileTotal.value = Number(res?.total ?? 0)
  } catch (error) {
    console.error(error)
  } finally {
    loadingFiles.value = false
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

const handleFilePageSizeChange = (size: number) => {
  filePageSize.value = size
  filePage.value = 1
  fetchAdminFiles()
}

const handleUserSearch = () => {
  userPage.value = 1
  fetchUsers()
}

const handleUserSortChange = (payload: { prop: string; order: 'ascending' | 'descending' | null }) => {
  userSortState.value = payload?.order ? payload : null
  userPage.value = 1
  fetchUsers()
}

const resetUserFilters = () => {
  userFilters.value = {
    userName: '',
    email: '',
    phone: '',
    isAdmin: null,
    isEnabled: null
  }
  userPage.value = 1
  fetchUsers()
}

const handleUserSelectionChange = (rows: any[]) => {
  selectedUserIds.value = rows.map(r => Number(r.id)).filter(id => id > 0)
}

const handleAuditSearch = () => {
  auditPage.value = 1
  fetchAuditLogs()
}

const handleAuditSortChange = (payload: { prop: string; order: 'ascending' | 'descending' | null }) => {
  auditSortState.value = payload?.order ? payload : null
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

const handleFileSearch = () => {
  filePage.value = 1
  fetchAdminFiles()
}

const resetFileFilters = () => {
  fileFilters.value = {
    userName: '',
    name: '',
    isFolder: null,
    isDeleted: false,
    isShared: null
  }
  filePage.value = 1
  fetchAdminFiles()
}

const handleFileSortChange = (payload: { prop: string; order: 'ascending' | 'descending' | null }) => {
  fileSortState.value = payload?.order ? payload : null
  filePage.value = 1
  fetchAdminFiles()
}

const handleFileSelectionChange = (rows: any[]) => {
  selectedFileIds.value = rows.map(r => Number(r.id)).filter(id => id > 0)
}

const getActionType = (action: string) => {
  if (action.includes('删除')) return 'danger'
  if (action.includes('上传') || action.includes('合并')) return 'success'
  if (action.includes('移动') || action.includes('重命名')) return 'warning'
  return 'info'
}

const handleEditQuota = (row: any) => {
  isBatchQuotaMode.value = false
  currentEditUser.value = row
  newQuotaGB.value = Math.round(row.totalSpace / 1024 / 1024 / 1024)
  showQuotaDialog.value = true
}

const handleEditUploadLimit = (row: any) => {
  isBatchUploadLimitMode.value = false
  currentEditUser.value = row
  const bytes = Number(row.maxUploadFileSize || 100 * 1024 * 1024)
  newUploadLimitMB.value = Math.max(1, Math.round(bytes / 1024 / 1024))
  showUploadLimitDialog.value = true
}

const openBatchQuotaDialog = () => {
  if (selectedUserIds.value.length === 0) {
    ElMessage.warning('请先选择用户')
    return
  }
  isBatchQuotaMode.value = true
  currentEditUser.value = null
  newQuotaGB.value = 5
  showQuotaDialog.value = true
}

const openBatchUploadLimitDialog = () => {
  if (selectedUserIds.value.length === 0) {
    ElMessage.warning('请先选择用户')
    return
  }
  isBatchUploadLimitMode.value = true
  currentEditUser.value = null
  newUploadLimitMB.value = 100
  showUploadLimitDialog.value = true
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
  if (!isBatchQuotaMode.value && !currentEditUser.value) return
  try {
    const bytes = newQuotaGB.value * 1024 * 1024 * 1024
    if (isBatchQuotaMode.value) {
      await request.put('/admin/users/quota', {
        userIds: selectedUserIds.value,
        newTotalSpaceBytes: bytes
      })
      ElMessage.success('批量配额修改成功')
    } else {
      await request.put(`/admin/user-quota?userId=${currentEditUser.value.id}&newTotalSpaceBytes=${bytes}`)
      ElMessage.success('配额修改成功')
    }
    showQuotaDialog.value = false
    isBatchQuotaMode.value = false
    fetchUsers()
  } catch (error) {
    console.error(error)
  }
}

const confirmEditUploadLimit = async () => {
  if (!isBatchUploadLimitMode.value && !currentEditUser.value) return
  try {
    const bytes = newUploadLimitMB.value * 1024 * 1024
    if (isBatchUploadLimitMode.value) {
      await request.put('/admin/users/upload-limit', {
        userIds: selectedUserIds.value,
        maxUploadFileSizeBytes: bytes
      })
      ElMessage.success('批量上传上限修改成功')
    } else {
      await request.put('/admin/user-upload-limit', {
        userId: currentEditUser.value.id,
        maxUploadFileSizeBytes: bytes
      })
      ElMessage.success('单文件上传上限修改成功')
    }
    showUploadLimitDialog.value = false
    isBatchUploadLimitMode.value = false
    fetchUsers()
  } catch (error) {
    console.error(error)
  }
}

const handleBatchEnable = async () => {
  if (selectedUserIds.value.length === 0) {
    ElMessage.warning('请先选择用户')
    return
  }
  try {
    await request.put('/admin/users/enable', { userIds: selectedUserIds.value })
    ElMessage.success('批量启用成功')
    fetchUsers()
  } catch (error) {
    console.error(error)
  }
}

const handleBatchDisable = async () => {
  if (selectedUserIds.value.length === 0) {
    ElMessage.warning('请先选择用户')
    return
  }
  const selectedUsers = userList.value.filter((u: any) => selectedUserIds.value.includes(Number(u.id)))
  if (selectedUsers.some((u: any) => !!u.isAdmin)) {
    ElMessage.warning('系统管理员不允许被禁用')
    return
  }
  try {
    await ElMessageBox.confirm('禁用后这些用户将无法登录系统，确认继续？', '批量禁用', {
      confirmButtonText: '确认禁用',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.put('/admin/users/disable', { userIds: selectedUserIds.value })
    ElMessage.success('批量禁用成功')
    fetchUsers()
  } catch (error) {
    console.error(error)
  }
}

const handleRenameFile = (row: any) => {
  currentEditFile.value = row
  renameFileForm.value.newName = row?.name || ''
  showRenameFileDialog.value = true
}

const confirmCreateFolder = async () => {
  const name = createFolderForm.value.name.trim()
  if (!createFolderForm.value.userId || createFolderForm.value.userId < 1) {
    ElMessage.warning('请输入正确的用户ID')
    return
  }
  if (!name) {
    ElMessage.warning('请输入文件夹名称')
    return
  }
  try {
    await request.post('/admin/files/folder', {
      userId: createFolderForm.value.userId,
      name,
      parentId: createFolderForm.value.parentId ?? null
    })
    ElMessage.success('文件夹创建成功')
    showCreateFolderDialog.value = false
    createFolderForm.value.name = ''
    createFolderForm.value.parentId = null
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const confirmRenameFile = async () => {
  if (!currentEditFile.value?.id) return
  const newName = renameFileForm.value.newName.trim()
  if (!newName) {
    ElMessage.warning('请输入新名称')
    return
  }
  try {
    await request.put('/admin/files/rename', {
      id: currentEditFile.value.id,
      newName
    })
    ElMessage.success('重命名成功')
    showRenameFileDialog.value = false
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleDeleteFile = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认删除 ${row.name} ?`, '删除确认', {
      confirmButtonText: '确认删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.delete(`/admin/files/${row.id}`)
    ElMessage.success('删除成功')
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const handlePermanentDeleteFile = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认彻底删除 ${row.name} ? 删除后不可恢复`, '彻底删除确认', {
      confirmButtonText: '确认彻底删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.delete(`/admin/files/permanent/${row.id}`)
    ElMessage.success('彻底删除成功')
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleBatchDeleteFiles = async () => {
  if (selectedFileIds.value.length === 0) {
    ElMessage.warning('请先选择文件')
    return
  }
  try {
    await ElMessageBox.confirm(`确认批量删除选中的 ${selectedFileIds.value.length} 项?`, '批量删除确认', {
      confirmButtonText: '确认删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.post('/admin/files/batch-delete', { ids: selectedFileIds.value })
    ElMessage.success('批量删除成功')
    selectedFileIds.value = []
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleBatchPermanentDeleteFiles = async () => {
  if (selectedFileIds.value.length === 0) {
    ElMessage.warning('请先选择文件')
    return
  }
  try {
    await ElMessageBox.confirm(`确认批量彻底删除选中的 ${selectedFileIds.value.length} 项? 删除后不可恢复`, '批量彻底删除确认', {
      confirmButtonText: '确认彻底删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.post('/admin/files/batch-permanent-delete', { ids: selectedFileIds.value })
    ElMessage.success('批量彻底删除成功')
    selectedFileIds.value = []
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleViewShareInfo = (row: any) => {
  if (!row?.shareToken) {
    ElMessage.warning('该项目当前没有分享记录')
    return
  }
  shareInfo.value = {
    link: `${window.location.origin}/share/${row.shareToken}`,
    shareCode: row.shareCode || '-',
    expireTime: row.shareExpireTime || null
  }
  showShareInfoDialog.value = true
}

const handleCancelShare = async (row: any) => {
  if (!isRowShared(row)) {
    ElMessage.warning('该项目当前没有分享记录')
    return
  }
  try {
    await ElMessageBox.confirm(`确认取消 ${row.name} 的分享？`, '取消分享确认', {
      confirmButtonText: '确认取消',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.post(`/admin/files/cancel-share/${row.id}`)
    ElMessage.success('取消分享成功')
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const handleBatchCancelShares = async () => {
  if (selectedFileIds.value.length === 0) {
    ElMessage.warning('请先选择文件')
    return
  }
  try {
    await ElMessageBox.confirm(`确认批量取消选中的 ${selectedFileIds.value.length} 项分享？`, '批量取消分享确认', {
      confirmButtonText: '确认取消',
      cancelButtonText: '取消',
      type: 'warning'
    })
    const res: any = await request.post('/admin/files/batch-cancel-share', { ids: selectedFileIds.value })
    ElMessage.success(res?.message || '批量取消分享成功')
    selectedFileIds.value = []
    fetchAdminFiles()
  } catch (error) {
    console.error(error)
  }
}

const copyText = async (text: string) => {
  if (!text || text === '-') {
    ElMessage.warning('没有可复制的内容')
    return
  }
  try {
    await navigator.clipboard.writeText(text)
    ElMessage.success('复制成功')
  } catch {
    ElMessage.error('复制失败')
  }
}

const isRowShared = (row: any) => Boolean(row?.isShared || row?.shareToken)

const handleCreateIpRule = async () => {
  if (!ipRuleForm.value.ruleText.trim()) {
    ElMessage.warning('请输入IP规则')
    return
  }
  try {
    await request.post('/admin/login-ip-rules', {
      ruleText: ipRuleForm.value.ruleText.trim(),
      remark: ipRuleForm.value.remark.trim(),
      isEnabled: ipRuleForm.value.isEnabled
    })
    ElMessage.success('新增登录限制成功')
    ipRuleForm.value.ruleText = ''
    ipRuleForm.value.remark = ''
    ipRuleForm.value.isEnabled = true
    fetchLoginIpRules()
  } catch (error) {
    console.error(error)
  }
}

const handleToggleIpRuleStatus = async (row: any, isEnabled: boolean) => {
  try {
    await request.put(`/admin/login-ip-rules/${row.id}/status`, { isEnabled })
    row.isEnabled = isEnabled
    ElMessage.success('规则状态更新成功')
  } catch (error) {
    console.error(error)
    fetchLoginIpRules()
  }
}

const handleDeleteIpRule = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认删除登录限制规则：${row.ruleText} ?`, '删除确认', {
      confirmButtonText: '确认删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.delete(`/admin/login-ip-rules/${row.id}`)
    ElMessage.success('规则删除成功')
    fetchLoginIpRules()
  } catch (error) {
    console.error(error)
  }
}

const handleCreateAdminIpRule = async () => {
  if (!adminIpRuleForm.value.ruleText.trim()) {
    ElMessage.warning('请输入IP规则')
    return
  }
  try {
    await request.post('/admin/admin-login-ip-rules', {
      ruleText: adminIpRuleForm.value.ruleText.trim(),
      remark: adminIpRuleForm.value.remark.trim(),
      isEnabled: adminIpRuleForm.value.isEnabled
    })
    ElMessage.success('新增管理员登录白名单成功')
    adminIpRuleForm.value.ruleText = ''
    adminIpRuleForm.value.remark = ''
    adminIpRuleForm.value.isEnabled = true
    fetchAdminLoginIpRules()
  } catch (error) {
    console.error(error)
  }
}

const handleToggleAdminIpRuleStatus = async (row: any, isEnabled: boolean) => {
  try {
    await request.put(`/admin/admin-login-ip-rules/${row.id}/status`, { isEnabled })
    row.isEnabled = isEnabled
    ElMessage.success('规则状态更新成功')
  } catch (error) {
    console.error(error)
    fetchAdminLoginIpRules()
  }
}

const handleDeleteAdminIpRule = async (row: any) => {
  try {
    await ElMessageBox.confirm(`确认删除管理员登录IP规则：${row.ruleText} ?`, '删除确认', {
      confirmButtonText: '确认删除',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await request.delete(`/admin/admin-login-ip-rules/${row.id}`)
    ElMessage.success('规则删除成功')
    fetchAdminLoginIpRules()
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

const formatDate = (dateStr?: string | null) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString()
}

onMounted(() => {
  fetchStats()
  fetchUsers()
  fetchAuditLogs()
  fetchLoginIpRules()
  fetchAdminLoginIpRules()
  fetchAdminFiles()
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

:deep(.admin-panel .pan-card) {
  background: transparent !important;
}

.stats-cards {
  margin-bottom: 0;
  flex-shrink: 0;
  
  :deep(.el-card) {
    background: transparent;
    border: 1px solid var(--pan-border-strong);
    border-radius: var(--pan-radius-lg);
    transition: var(--pan-transition);
    
    &:hover {
      border-color: var(--pan-primary);
      background: transparent;
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

.batch-actions {
  margin-top: 10px;
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.batch-count {
  color: var(--pan-text-muted);
  font-size: 12px;
  margin-right: 4px;
}

.ip-rule-form {
  display: grid;
  grid-template-columns: minmax(320px, 2fr) minmax(180px, 1fr) auto auto;
  gap: 10px;
  align-items: center;
}

.ip-section-title {
  font-size: 13px;
  color: var(--pan-text-muted);
  margin-bottom: 10px;
}

.ip-rule-status-cell {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  white-space: nowrap;
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
  white-space: nowrap;
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
  background: transparent !important;
}

.admin-file-actions {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  flex-wrap: nowrap;
}

.admin-icon-actions {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex-wrap: nowrap;
  white-space: nowrap;
}

.icon-action-btn {
  min-width: 32px !important;
  width: 32px;
  height: 32px;
  padding: 0 !important;
  border-radius: 8px;
}

.admin-file-name-cell {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.share-status-icon {
  color: var(--pan-primary);
  font-size: 14px;
  vertical-align: middle;
}

.admin-file-actions .admin-btn {
  min-width: 72px;
  padding-left: 10px;
  padding-right: 10px;
}

.table-pagination {
  position: sticky;
  bottom: 0;
  z-index: 4;
  margin-top: 8px;
  padding: 8px 12px 6px;
  display: flex;
  justify-content: flex-end;
  border-top: 1px solid var(--pan-border);
  background: var(--pan-bg);
}

.usage-cell {
  color: var(--pan-text-body);
  font-size: 13px;
}

.upload-limit-cell {
  display: block;
  text-align: right;
}

.filter-card {
  background: transparent !important;
}

:deep(.table-card .el-table th.el-table__cell) {
  background-color: transparent !important;
}

:deep(.table-card .el-table),
:deep(.table-card .el-table__inner-wrapper),
:deep(.table-card .el-table__header-wrapper),
:deep(.table-card .el-table__body-wrapper) {
  background-color: transparent !important;
}

:deep(.table-card .el-table__fixed),
:deep(.table-card .el-table__fixed-header-wrapper),
:deep(.table-card .el-table__fixed-body-wrapper),
:deep(.table-card .el-table__fixed-right-patch) {
  background-color: var(--pan-bg) !important;
}

:deep(.table-card .el-table .el-table-fixed-column--left),
:deep(.table-card .el-table .el-table-fixed-column--left.is-last-column),
:deep(.table-card .el-table .el-table-fixed-column--right),
:deep(.table-card .el-table .el-table-fixed-column--right.is-first-column) {
  background-color: var(--pan-bg) !important;
}

:deep(.table-card .el-table .el-table-fixed-column--left.el-table__cell),
:deep(.table-card .el-table .el-table-fixed-column--right.el-table__cell) {
  background-color: var(--pan-bg) !important;
}

:deep(.table-card .el-table__fixed-header-wrapper th.el-table__cell),
:deep(.table-card .el-table__fixed-body-wrapper td.el-table__cell) {
  background-color: var(--pan-bg) !important;
}

:deep(.table-card .el-table .el-table-fixed-column--right::before) {
  background-color: transparent !important;
}

:deep(.table-card .el-table .el-table-fixed-column--left::before) {
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

  .ip-rule-form {
    grid-template-columns: 1fr;
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
