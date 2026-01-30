<template>
  <div class="app-container">
    <div class="sidebar" v-if="showSidebar" :class="{ collapsed: isCollapse }">
      <div class="logo">
        <img src="./assets/vue.svg" alt="logo" />
        <span v-show="!isCollapse">我的网盘</span>
      </div>
      
      <div class="collapse-btn" @click="toggleSidebar">
        <el-icon><component :is="isCollapse ? Expand : Fold" /></el-icon>
      </div>

      <el-menu
        :default-active="activeMenu"
        class="el-menu-vertical"
        router
        :collapse="isCollapse"
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
      
      <div class="storage-info">
        <el-progress :percentage="storageUsage" :format="storageFormat" />
        <p class="usage-text">已用 {{ usedSpaceStr }} / {{ totalSpaceStr }}</p>
      </div>

      <div class="user-profile">
         <el-dropdown>
          <span class="el-dropdown-link">
            {{ username }}
            <el-icon class="el-icon--right"><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="logout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </div>
    
    <div class="main-content" :class="{ 'full-screen': !showSidebar }">
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
}

.sidebar {
  width: 240px;
  background-color: #f5f7fa;
  border-right: 1px solid #e4e7ed;
  display: flex;
  flex-direction: column;
  padding: 20px 0;

  .logo {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 0 20px 20px;
    font-size: 18px;
    font-weight: bold;
    color: var(--pan-primary);
    
    img {
      width: 32px;
      height: 32px;
    }
  }

  .el-menu {
    border-right: none;
    background-color: transparent;
  }

  .storage-info {
    margin-top: auto;
    padding: 20px;
    
    p {
      font-size: 12px;
      color: #909399;
      margin-top: 5px;
      text-align: center;
    }

    .usage-text {
      white-space: nowrap;
    }
  }

  .user-profile {
    padding: 0 20px;
    cursor: pointer;
    text-align: center;
    border-top: 1px solid #e4e7ed;
    padding-top: 20px;
  }
}

.main-content {
  flex: 1;
  padding: 20px;
  overflow: auto;
}
</style>
