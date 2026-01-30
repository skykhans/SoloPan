<template>
  <div class="app-container">
    <div class="sidebar" v-if="showSidebar" :class="{ collapsed: isCollapse }">
      <div class="logo">
        <img src="./assets/vue.svg" alt="logo" />
        <span v-show="!isCollapse">我的网盘</span>
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
            <el-icon class="el-icon--right" :class="{ 'collapsed-icon': isCollapse }"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="logout">退出登录</el-dropdown-item>
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
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import request from './utils/request'
import { 
  Upload, FolderAdd, FolderOpened, Document, 
  Star, StarFilled, Download, More, Edit, Rank, Share, Delete, Link, Folder, RefreshLeft, Monitor, ArrowDown, Expand, Fold
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const username = ref(localStorage.getItem('username') || '用户')
const usedSpace = ref(0)
const totalSpace = ref(1024 * 1024 * 1024) // 1GB 默认
const isAdmin = ref(false)
const isCollapse = ref(false)

const showSidebar = computed(() => {
  return route.name !== 'Login' && route.name !== 'Share'
})

const toggleSidebar = () => {
  isCollapse.value = !isCollapse.value
}

const handleMenuClick = (path: string) => {
  if (path === '/files' && route.path === '/files') {
    // 如果已经在 /files 页面，再次点击则通过 EventBus 或 Query 变化来触发刷新
    // 这里我们可以简单地添加一个时间戳 query 参数来触发组件更新，
    // 或者依赖 FileList.vue 中对 query 的监听（如果后续实现了）
    // 目前最简单的办法是利用 query
    router.push({ path: '/files', query: { t: Date.now() } })
  }
}

const activeMenu = computed(() => route.path)

const storageUsage = computed(() => {
  return Math.min(Math.round((usedSpace.value / totalSpace.value) * 100), 100)
})

const storageFormat = (percentage: number) => (percentage === 100 ? 'Full' : `${percentage}%`)

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
  if (!showSidebar.value) return
  try {
    const res: any = await request.get('/user/info')
    // 兼容后端可能返回的不同大小写 (UserName vs username vs userName)
    username.value = res.userName || res.username || res.UserName || '用户'
    usedSpace.value = res.usedSpace
    totalSpace.value = res.totalSpace
    isAdmin.value = res.isAdmin
  } catch (error) {
    console.error(error)
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
      
      img {
        width: 24px;
        height: 24px;
        filter: none;
      }
    }

  .el-menu {
    border-right: none;
    background-color: transparent;
    flex: 1;
    padding: 0 12px;

    :deep(.el-menu-item) {
      height: 40px;
      line-height: 40px;
      border-radius: var(--pan-radius-sm);
      margin-bottom: 2px;
      color: var(--pan-text-body);
      font-size: 13px;
      
      &:hover {
        background-color: rgba(255, 255, 255, 0.03);
        color: var(--pan-text-main);
      }

      &.is-active {
        background: rgba(255, 255, 255, 0.05);
        color: var(--pan-text-main);
        font-weight: 500;
      }

      .el-icon {
        font-size: 18px;
        margin-right: 10px;
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
    top: 13px;
    z-index: 1000;
    cursor: pointer;
    color: var(--pan-text-muted);
    transition: var(--pan-transition);
    display: flex;
    align-items: center;
    padding: 8px;
    border-radius: var(--pan-radius-sm);
    background: rgba(255, 255, 255, 0.02);
    border: 1px solid var(--pan-border);

    &:hover {
      color: var(--pan-text-main);
      background-color: rgba(255, 255, 255, 0.05);
      border-color: var(--pan-border-strong);
    }
  }

  /* We need a header-like area to keep the button consistent */
  &.has-sidebar::before {
    content: '';
    height: 64px;
    background: transparent;
    width: 100%;
    pointer-events: none;
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
</style>
