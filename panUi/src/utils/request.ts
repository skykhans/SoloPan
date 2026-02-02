import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '../router'

const service = axios.create({
  baseURL: 'http://localhost:5080/api', // 后端地址
  timeout: 10000
})

// 请求拦截器
service.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  response => {
    return response.data
  },
  error => {
    if (error.response) {
      switch (error.response.status) {
        case 401:
          // 如果当前已经在登录页，或者是登录请求返回的 401，不重复提示
          if (router.currentRoute.value.path !== '/login' && error.config.url !== '/user/login') {
            ElMessage.error('登录过期，请重新登录')
          }
          localStorage.removeItem('token')
          localStorage.removeItem('username')
          if (router.currentRoute.value.path !== '/login') {
            router.push('/login')
          }
          break;
        case 403:
          ElMessage.error('没有权限')
          break
        case 404:
          ElMessage.error('资源不存在')
          break
        default:
          ElMessage.error(error.response.data?.title || error.response.data || '系统错误')
      }
    } else {
      ElMessage.error('网络错误')
    }
    return Promise.reject(error)
  }
)

export default service
