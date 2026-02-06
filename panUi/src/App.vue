<template>
  <div class="app-container">
    <div class="sidebar" v-if="showSidebar" :class="{ collapsed: isCollapse }">
      <div class="logo">
        <div class="logo-icon">
          <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 4L4 8L12 12L20 8L12 4Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M4 12L12 16L20 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M4 16L12 20L20 16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </div>
        <span v-show="!isCollapse" class="logo-text">Trae Pan</span>
      </div>
      
      <el-menu
        :default-active="activeMenu"
        class="el-menu-vertical"
        router
        :collapse="isCollapse"
        :collapse-transition="false"
      >
        <el-menu-item index="/files" @click="handleMenuClick('/files')">
          <el-icon><FolderOpened /></el-icon>
          <span>全部文件</span>
        </el-menu-item>
        <el-menu-item index="/favorites">
          <el-icon><Star /></el-icon>
          <span>我的收藏</span>
        </el-menu-item>
        <el-menu-item index="/my-shares">
          <el-icon><Share /></el-icon>
          <span>我的分享</span>
        </el-menu-item>
        <el-menu-item index="/offline-downloads">
          <el-icon><Download /></el-icon>
          <span>离线下载</span>
        </el-menu-item>
        <el-menu-item index="/recycle-bin">
          <el-icon><Delete /></el-icon>
          <span>回收站</span>
        </el-menu-item>
        <el-menu-item index="/admin" v-if="isAdmin">
          <el-icon><Monitor /></el-icon>
          <span>系统管理</span>
        </el-menu-item>
      </el-menu>
      
      <div class="storage-info" v-show="!isCollapse">
        <div class="storage-label">
          <span>存储空间</span>
          <span class="percentage">{{ storageUsage }}%</span>
        </div>
        <el-progress :percentage="storageUsage" :show-text="false" :stroke-width="4" />
        <p class="usage-text mono">{{ usedSpaceStr }} / {{ totalSpaceStr }}</p>
      </div>

      <div class="user-profile">
        <el-dropdown trigger="click">
          <span class="el-dropdown-link">
            <div class="user-avatar">
              <img v-if="avatar" :src="avatar" alt="avatar" class="avatar-img" />
              <span v-else>{{ username.charAt(0).toUpperCase() }}</span>
            </div>
            <span v-show="!isCollapse" class="username-text">{{ username }}</span>
            <el-icon v-show="!isCollapse" class="el-icon--right"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="showProfileDialog = true">个人中心</el-dropdown-item>
              <el-dropdown-item @click="showPasswordDialog = true">修改密码</el-dropdown-item>
              <el-dropdown-item divided @click="logout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>
    
    <div class="main-content main-content-border" :class="{ 'full-screen': !showSidebar, 'has-sidebar': showSidebar }">
      <div class="collapse-btn" v-if="showSidebar" @click="toggleSidebar">
        <el-icon :size="24"><component :is="isCollapse ? Expand : Fold" /></el-icon>
      </div>
      <div class="view-container">
        <router-view />
      </div>
    </div>

    <!-- 个人中心弹窗 -->
    <el-dialog v-model="showProfileDialog" title="个人中心" width="400px" append-to-body>
      <el-form :model="profileForm" label-width="70px" style="padding: 10px 20px">
        <el-form-item label="头像">
          <div class="profile-avatar-setup">
            <div class="user-avatar big">
              <img v-if="profileForm.avatar" :src="profileForm.avatar" alt="avatar" class="avatar-img" />
              <span v-else>{{ profileForm.username?.charAt(0).toUpperCase() }}</span>
            </div>
            <el-upload
              :show-file-list="false"
              accept="image/png,image/jpeg,image/webp,image/gif"
              :before-upload="beforeAvatarUpload"
              :http-request="handleAvatarUpload"
            >
              <el-button size="small" :icon="Upload" :loading="uploadingAvatar">上传头像</el-button>
            </el-upload>
            <p class="avatar-tip">支持 jpg/png/webp/gif，大小不超过 2MB</p>
          </div>
        </el-form-item>
        <el-form-item label="用户名">
          <el-input v-model="profileForm.username" disabled />
        </el-form-item>
        <el-form-item label="手机号" required>
          <el-input v-model="profileForm.phone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="邮箱" required>
          <el-input v-model="profileForm.email" placeholder="请输入邮箱" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button :icon="Close" @click="showProfileDialog = false">取消</el-button>
          <el-button type="primary" :icon="Check" @click="updateProfile" :loading="updating">保存修改</el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 修改密码弹窗 -->
    <el-dialog v-model="showPasswordDialog" title="修改密码" width="400px" append-to-body>
      <el-form :model="passwordForm" label-width="80px" style="padding: 10px 20px">
        <el-form-item label="原密码">
          <el-input v-model="passwordForm.oldPassword" type="password" show-password placeholder="请输入原密码" />
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
          <el-button type="primary" :icon="Check" @click="changePassword" :loading="updating">确认修改</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import request from './utils/request'
import type { UploadRequestOptions } from 'element-plus'
import {
  FolderOpened,
  Star,
  Share,
  Delete,
  Download,
  Monitor,
  ArrowDown,
  Expand,
  Fold,
  Upload,
  Close,
  Check
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const username = ref(localStorage.getItem('username') || '用户')
const avatar = ref('')
const usedSpace = ref(0)
const totalSpace = ref(1024 * 1024 * 1024) // 1GB 默认
const isAdmin = ref(false)
const isCollapse = ref(false)
const showProfileDialog = ref(false)
const showPasswordDialog = ref(false)
const updating = ref(false)
const uploadingAvatar = ref(false)

const profileForm = ref({
  username: '',
  phone: '',
  email: '',
  avatar: ''
})

const passwordForm = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// 校验工具函数
const validateEmail = (email: string) => /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(email)
const validatePhone = (phone: string) => /^1[3-9]\d{9}$/.test(phone)
const validatePassword = (pwd: string) => {
  if (pwd.length < 8) return '密码长度至少为 8 位'
  if (!/[a-zA-Z]/.test(pwd) || !/[0-9]/.test(pwd)) return '密码必须包含字母和数字'
  return null
}

const showSidebar = computed(() => {
  return route.name !== 'Login' && route.name !== 'Share'
})

const toggleSidebar = () => {
  isCollapse.value = !isCollapse.value
}

const handleMenuClick = (path: string) => {
  if (path === '/files' && route.path === '/files') {
    router.push({ path: '/files', query: { t: Date.now() } })
  }
}

const activeMenu = computed(() => route.path)

const storageUsage = computed(() => {
  return Math.min(Math.round((usedSpace.value / totalSpace.value) * 100), 100)
})

const usedSpaceStr = computed(() => formatSize(usedSpace.value))
const totalSpaceStr = computed(() => formatSize(totalSpace.value))

const formatSize = (bytes: number) => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

const logout = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('username')
  router.push('/login')
}

const fetchUserInfo = async () => {
  const token = localStorage.getItem('token')
  if (!showSidebar.value || !token) return
  
  try {
    const res: any = await request.get('/user/info')
    username.value = res.userName || res.username || res.UserName || '用户'
    usedSpace.value = res.usedSpace
    totalSpace.value = res.totalSpace
    isAdmin.value = res.isAdmin
    
    // 同步到个人中心表单
    profileForm.value.username = username.value
    profileForm.value.phone = res.phone || ''
    profileForm.value.email = res.email || ''
    profileForm.value.avatar = res.avatar || ''
    avatar.value = res.avatar || ''
  } catch (error) {
    console.error(error)
  }
}

const beforeAvatarUpload = (file: File) => {
  const isImage = file.type.startsWith('image/')
  if (!isImage) {
    import('element-plus').then(({ ElMessage }) => ElMessage.warning('仅支持上传图片文件'))
    return false
  }

  const isLt2M = file.size / 1024 / 1024 < 2
  if (!isLt2M) {
    import('element-plus').then(({ ElMessage }) => ElMessage.warning('头像文件不能超过 2MB'))
    return false
  }
  return true
}

const handleAvatarUpload = async (options: UploadRequestOptions) => {
  uploadingAvatar.value = true
  try {
    const formData = new FormData()
    formData.append('file', options.file)
    const res: any = await request.post('/user/upload-avatar', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    profileForm.value.avatar = res.avatar || ''
    avatar.value = res.avatar || ''
    import('element-plus').then(({ ElMessage }) => ElMessage.success('头像上传成功'))
  } catch (error) {
    console.error(error)
  } finally {
    uploadingAvatar.value = false
  }
}

const updateProfile = async () => {
  if (profileForm.value.email && !validateEmail(profileForm.value.email)) {
    import('element-plus').then(({ ElMessage }) => ElMessage.warning('请输入有效的邮箱地址'))
    return
  }
  if (profileForm.value.phone && !validatePhone(profileForm.value.phone)) {
    import('element-plus').then(({ ElMessage }) => ElMessage.warning('请输入有效的手机号'))
    return
  }

  updating.value = true
  try {
    await request.post('/user/update-profile', {
      email: profileForm.value.email,
      phone: profileForm.value.phone,
      avatar: profileForm.value.avatar
    })
    import('element-plus').then(({ ElMessage }) => ElMessage.success('资料更新成功'))
    showProfileDialog.value = false
    fetchUserInfo()
  } catch (error) {
    console.error(error)
  } finally {
    updating.value = false
  }
}

const changePassword = async () => {
  const pwdError = validatePassword(passwordForm.value.newPassword)
  if (pwdError) {
    import('element-plus').then(({ ElMessage }) => ElMessage.warning(pwdError))
    return
  }

  if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
    import('element-plus').then(({ ElMessage }) => ElMessage.warning('两次输入的新密码不一致'))
    return
  }
  
  updating.value = true
  try {
    await request.post('/user/change-password', {
      oldPassword: passwordForm.value.oldPassword,
      newPassword: passwordForm.value.newPassword
    })
    import('element-plus').then(({ ElMessage }) => ElMessage.success('密码修改成功，请重新登录'))
    showPasswordDialog.value = false
    logout()
  } catch (error) {
    console.error(error)
  } finally {
    updating.value = false
  }
}

watch(() => showSidebar.value, (val) => {
  if (val) {
    username.value = localStorage.getItem('username') || '用户'
    fetchUserInfo()
  }
})

onMounted(() => {
  fetchUserInfo()
})
</script>

<style scoped lang="scss">
.app-container {
  display: flex;
  height: 100vh;
  width: 100vw;
  overflow: hidden;
  background-color: var(--pan-bg);
}

.sidebar {
  width: 220px;
  background-color: var(--pan-sidebar-bg);
  border-right: 1px solid var(--pan-border);
  display: flex;
  flex-direction: column;
  padding: 20px 0;
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  z-index: 100;

  &.collapsed {
    width: 68px;

    .logo {
      padding: 0;
      justify-content: center;
      .logo-text { display: none; }
    }

    .el-menu {
      padding: 0 10px;
      :deep(.el-menu-item) {
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 0 !important;
        width: 44px;
        margin-left: auto;
        margin-right: auto;
        .el-icon { margin: 0 !important; }
      }
    }

    .storage-info { display: none; }
    .user-profile {
      padding: 12px 0;
      justify-content: center;
      .username-text, .el-icon--right { display: none; }
    }
  }

  .logo {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 0 20px;
    margin-bottom: 28px;
    height: 32px;
    
    .logo-icon {
      width: 26px;
      height: 26px;
      color: var(--pan-primary);
      flex-shrink: 0;
      filter: drop-shadow(0 0 8px rgba(16, 185, 129, 0.3));
    }

    .logo-text {
      font-size: 17px;
      font-weight: 800;
      letter-spacing: -0.02em;
      color: var(--pan-text-main);
      white-space: nowrap;
    }
  }

  .el-menu {
    border-right: none;
    background-color: transparent;
    flex: 1;
    padding: 0 12px;

    :deep(.el-menu-item) {
      height: 44px;
      border-radius: var(--pan-radius-sm);
      margin-bottom: 4px;
      color: var(--pan-text-body);
      font-size: 13.5px;
      font-weight: 500;
      transition: var(--pan-transition);
      
      .el-icon {
        font-size: 18px;
        margin-right: 10px;
        color: var(--pan-text-muted);
      }

      &:hover {
        background-color: rgba(255, 255, 255, 0.04);
        color: var(--pan-text-main);
        .el-icon { color: var(--pan-primary); }
      }

      &.is-active {
        background-color: var(--pan-primary-dim);
        color: var(--pan-primary);
        font-weight: 700;
        .el-icon { color: var(--pan-primary); }
      }
    }
  }

  .storage-info {
    position: relative;
    padding: 14px 14px 12px;
    margin: 16px 12px;
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.04) 0%, rgba(255, 255, 255, 0.015) 100%), var(--pan-surface-card);
    border: 1px solid var(--pan-border-strong);
    border-radius: var(--pan-radius-md);
    box-shadow: 0 12px 30px rgba(0, 0, 0, 0.45);
    overflow: hidden;

    &::after {
      content: '';
      position: absolute;
      left: 0;
      right: 0;
      top: 0;
      height: 1px;
      background: linear-gradient(90deg, transparent, rgba(16, 185, 129, 0.45), transparent);
      opacity: 0.6;
    }

    .storage-label {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 10px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--pan-text-muted);

      .percentage {
        font-size: 12px;
        font-weight: 800;
        color: var(--pan-primary);
      }
    }
    
    :deep(.el-progress) {
      margin-bottom: 10px;
      .el-progress-bar__outer {
        height: 6px !important;
        background: rgba(255, 255, 255, 0.06) !important;
        border-radius: 999px !important;
      }
      .el-progress-bar__inner {
        border-radius: 999px !important;
        background: linear-gradient(90deg, var(--pan-primary) 0%, var(--pan-primary-light) 100%) !important;
        box-shadow: var(--pan-primary-glow) !important;
      }
    }

    .usage-text {
      font-size: 11px;
      color: var(--pan-text-body);
      font-weight: 600;
    }
  }

    .user-profile {
    padding: 12px 16px;
    border-top: 1px solid var(--pan-border);
    display: flex;
    justify-content: center;
    
      .el-dropdown-link {
      display: flex;
      align-items: center;
      gap: 10px;
      cursor: pointer;
      outline: none;
      
        .user-avatar {
        width: 28px;
        height: 28px;
        background: var(--pan-primary);
        color: #000;
        border-radius: 4px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 800;
        font-size: 12px;
        }

        .avatar-img {
          width: 100%;
          height: 100%;
          object-fit: cover;
          border-radius: inherit;
          display: block;
        }

      .username-text {
        flex: 1;
        font-size: 13px;
        font-weight: 600;
        color: var(--pan-text-main);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .el-icon--right { color: var(--pan-text-muted); font-size: 12px; }
    }
  }
}

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  background-color: var(--pan-bg);
  min-width: 0;

  .collapse-btn {
    padding: 16px 24px;
    display: flex;
    align-items: center;
    color: var(--pan-text-muted);
    cursor: pointer;
    transition: var(--pan-transition);
    
    &:hover { color: var(--pan-text-main); }
  }

  .view-container {
    flex: 1;
    padding: 0 24px 24px;
    overflow: hidden;
    display: flex;
    flex-direction: column;
  }

  &.full-screen .view-container { padding: 0; }
}

.profile-avatar-setup {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 12px 0;
  .user-avatar.big {
    width: 56px; height: 56px; font-size: 22px; margin-bottom: 12px;
    background: var(--pan-primary); color: #000; border-radius: 8px;
    display: flex; align-items: center; justify-content: center; font-weight: 800;
  }
  .avatar-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    border-radius: inherit;
    display: block;
  }
  .avatar-tip { font-size: 12px; color: var(--pan-text-muted); }
}

.dialog-footer { display: flex; justify-content: flex-end; gap: 12px; }

.input-tip {
  margin-top: 4px;
  font-size: 12px;
  color: var(--pan-text-muted);
  white-space: nowrap;
}
</style>
