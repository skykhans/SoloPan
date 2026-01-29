import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import FileList from '../views/FileList.vue'
import Login from '../views/Login.vue'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/files'
  },
  {
    path: '/login',
    name: 'Login',
    component: Login
  },
  {
    path: '/files',
    name: 'Files',
    component: FileList,
    props: { category: 'files' }
  },
  {
    path: '/favorites',
    name: 'Favorites',
    component: FileList,
    props: { category: 'favorites' }
  },
  {
    path: '/recycle-bin',
    name: 'RecycleBin',
    component: FileList,
    props: { category: 'recycle-bin' }
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

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  if (to.name !== 'Login' && to.name !== 'Share' && !token) {
    next({ name: 'Login' })
  } else {
    next()
  }
})

export default router
