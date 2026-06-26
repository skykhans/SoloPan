import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/files'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue')
  },
  {
    path: '/files',
    name: 'Files',
    component: () => import('../views/FileList.vue'),
    props: { category: 'files' }
  },
  {
    path: '/favorites',
    name: 'Favorites',
    component: () => import('../views/FileList.vue'),
    props: { category: 'favorites' }
  },
  {
    path: '/recycle-bin',
    name: 'RecycleBin',
    component: () => import('../views/FileList.vue'),
    props: { category: 'recycle-bin' }
  },
  {
    path: '/my-shares',
    name: 'MyShares',
    component: () => import('../views/MyShares.vue')
  },
  {
    path: '/offline-downloads',
    name: 'OfflineDownloads',
    component: () => import('../views/OfflineDownloads.vue')
  },
  {
    path: '/share/:token',
    name: 'Share',
    component: () => import('../views/Share.vue')
  },
  {
    path: '/admin',
    name: 'Admin',
    component: () => import('../views/Admin.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, _, next) => {
  const token = localStorage.getItem('token')
  if (to.name !== 'Login' && to.name !== 'Share' && !token) {
    next({ name: 'Login' })
  } else {
    next()
  }
})

export default router
