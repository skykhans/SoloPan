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
            <div class="user-avatar">{{ username.charAt(0).toUpperCase() }}</div>
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
      <router-view />
    </div>

    <!-- 个人中心弹窗 -->
    <el-dialog v-model="showProfileDialog" title="个人中心" width="400px" append-to-body>
      <el-form :model="profileForm" label-width="70px" style="padding: 10px 20px">
        <el-form-item label="头像">
          <div class="profile-avatar-setup">
            <div class="user-avatar big">{{ profileForm.username?.charAt(0).toUpperCase() }}</div>
            <p class="avatar-tip">系统根据用户名自动生成</p>
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
          <el-button @click="showProfileDialog = false">取消</el-button>
          <el-button type="primary" @click="updateProfile" :loading="updating">保存修改</el-button>
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
          <div class="input-tip">密码至少8位，需包含字母和数字</div>
        </el-form-item>
        <el-form-item label="确认密码">
          <el-input v-model="passwordForm.confirmPassword" type="password" show-password placeholder="请再次输入新密码" />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="showPasswordDialog = false">取消</el-button>
          <el-button type="primary" @click="changePassword" :loading="updating">确认修改</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import request from './utils/request'
import {
  FolderOpened,
  Star,
  Share,
  Delete,
  Monitor,
  ArrowDown,
  Expand,
  Fold
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const username = ref(localStorage.getItem('username') || '用户')
const usedSpace = ref(0)
const totalSpace = ref(1024 * 1024 * 1024) // 1GB 默认
const isAdmin = ref(false)
const isCollapse = ref(false)
const showProfileDialog = ref(false)
const showPasswordDialog = ref(false)
const updating = ref(false)

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
  } catch (error) {
    console.error(error)
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
  background-color: #000000;
}

.sidebar {
    width: 200px; /* Slimmer sidebar like typical Trae toolbars */
    background-color: var(--pan-sidebar-bg);
    border-right: 1px solid var(--pan-border);
    display: flex;
    flex-direction: column;
    padding: 16px 0;
    transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    position: relative;
    z-index: 100;

    &.collapsed {
      width: 60px;

      .logo {
        padding: 0;
        justify-content: center;
        margin-bottom: 24px;
      }

      .el-menu {
        padding: 0 8px;
        
        :deep(.el-menu-item) {
          padding: 0 !important;
          display: flex;
          justify-content: center;
          padding-left: 0 !important;
          
          // 折叠时激活指示器保持可见
          &.is-active::before {
            left: 0;
          }
          
          .el-icon {
            margin: 0;
          }
        }
      }

      .user-profile {
        padding: 12px 0;
        justify-content: center;

        .el-dropdown-link {
          justify-content: center;
          gap: 0;
        }
      }
    }

    .logo {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 0 16px;
      margin-bottom: 24px;
      font-size: 16px;
      font-weight: 800;
      color: var(--pan-text-main);
      letter-spacing: -0.5px;
      white-space: nowrap;
      overflow: hidden;
      
      .logo-icon {
        width: 24px;
        height: 24px;
        color: var(--pan-primary);
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        
        svg {
          width: 100%;
          height: 100%;
        }
      }

      .logo-text {
        background: linear-gradient(135deg, #ffffff 0%, var(--pan-primary) 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        font-weight: 800;
      }
    }

  .el-menu {
    border-right: none;
    background-color: transparent;
    flex: 1;
    padding: 0 12px;

    :deep(.el-menu-item) {
      height: 42px;
      line-height: 42px;
      border-radius: var(--pan-radius-sm);
      margin-bottom: 4px;
      color: var(--pan-text-body);
      font-size: 13px;
      font-weight: 500;
      position: relative;
      transition: all 0.2s ease;
      padding-left: 14px !important;
      
      // 左侧激活指示器
      &::before {
        content: '';
        position: absolute;
        left: 0;
        top: 50%;
        transform: translateY(-50%);
        width: 3px;
        height: 0;
        background: var(--pan-primary);
        border-radius: 0 2px 2px 0;
        transition: height 0.2s ease;
      }
      
      &:hover {
        background-color: rgba(255, 255, 255, 0.04);
        color: var(--pan-text-main);
        
        .el-icon {
          color: var(--pan-primary);
          transform: scale(1.05);
        }
      }

      &.is-active {
        background: linear-gradient(90deg, rgba(16, 185, 129, 0.08) 0%, transparent 100%);
        color: var(--pan-text-main);
        font-weight: 600;
        
        &::before {
          height: 20px;
        }
        
        .el-icon {
          color: var(--pan-primary);
          filter: drop-shadow(0 0 6px rgba(16, 185, 129, 0.4));
        }
      }

      white-space: nowrap;
      overflow: hidden;

      .el-icon {
        font-size: 18px;
        margin-right: 12px;
        transition: all 0.2s ease;
        color: var(--pan-text-muted);
      }
    }
  }

  .storage-info {
    padding: 16px;
    margin: 0 12px 16px;
    background: rgba(255, 255, 255, 0.02);
    border: 1px solid var(--pan-border);
    border-radius: var(--pan-radius-md);
    transition: var(--pan-transition);

    &:hover {
      background: rgba(255, 255, 255, 0.04);
      border-color: var(--pan-border-strong);
    }

    .storage-label {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 10px;
      font-size: 12px;
      color: var(--pan-text-body);
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;

      .percentage {
        color: var(--pan-primary);
        font-family: var(--font-mono);
        font-size: 12px;
      }
    }
    
    :deep(.el-progress) {
      margin-bottom: 10px;

      .el-progress-bar__outer {
        background-color: rgba(255, 255, 255, 0.05) !important;
        border-radius: 2px;
      }
      
      .el-progress-bar__inner {
        background: var(--pan-primary) !important;
        border-radius: 2px;
        transition: width 0.6s cubic-bezier(0.4, 0, 0.2, 1);
      }
    }

    .usage-text {
      font-size: 11px;
      color: var(--pan-text-muted);
      margin: 0;
      text-align: left;
      font-weight: 500;
      letter-spacing: 0.01em;
      white-space: nowrap;
      overflow: hidden;
    }
  }

  .user-profile {
    padding: 12px 16px;
    cursor: pointer;
    border-top: 1px solid var(--pan-border);
    display: flex;
    align-items: center;
    transition: var(--pan-transition);
    margin-top: 4px;
    
    &:hover {
      background: rgba(255, 255, 255, 0.03);
    }

    :deep(.el-dropdown) {
      width: 100%;
    }

    .el-dropdown-link {
      display: flex;
      align-items: center;
      gap: 10px;
      color: var(--pan-text-main);
      font-weight: 500;
      font-size: 13px;
      width: 100%;
      outline: none;
      
      .user-avatar {
        width: 24px;
        height: 24px;
        background: var(--pan-primary);
        color: #000;
        border-radius: 4px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 800;
        font-size: 12px;
        flex-shrink: 0;
      }

      .username-text {
        flex: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        text-align: left;
      }
      
      .collapsed-icon {
        margin-left: auto;
        font-size: 12px;
        color: var(--pan-text-muted);
        
        &.is-collapse {
          margin: 0;
        }
      }
    }
  }
}

.main-content {
  flex: 1;
  overflow: hidden;
  position: relative;
  display: flex;
  flex-direction: column;
  padding: 0;
  background-color: #000000;
  min-height: 0; /* Important for flex children */

  .collapse-btn {
    position: absolute;
    left: 20px;
    top: 10px; /* Center in 60px header */
    z-index: 1000;
    cursor: pointer;
    color: var(--pan-text-muted);
    transition: var(--pan-transition);
    display: flex;
    align-items: center;
    padding: 8px;
    border-radius: var(--pan-radius-sm);
    background: transparent; /* Remove background to blend in */
    border: 1px solid transparent; /* Remove border */

    &:hover {
      color: var(--pan-text-main);
      background-color: rgba(255, 255, 255, 0.05);
    }
  }

  /* Remove the spacer */
  &.has-sidebar::before {
    display: none;
  }

  & > :not(.collapse-btn) {
    flex: 1;
    padding: 0 24px 24px; /* Internal content padding */
    overflow: auto;
  }

  &.full-screen > :not(.collapse-btn) {
    padding: 0;
  }
}

.profile-avatar-setup {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
  padding: 10px 0;

  .user-avatar.big {
    width: 64px;
    height: 64px;
    font-size: 24px;
    margin-bottom: 8px;
  }

  .avatar-tip {
    font-size: 12px;
    color: var(--pan-text-muted);
    margin: 0;
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 10px;
}
</style>
