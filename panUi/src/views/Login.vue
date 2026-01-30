<template>
  <div class="login-container">
    <div class="brand">
      <img src="../assets/vue.svg" alt="logo" />
      <h1>我的网盘</h1>
      <p>安全、便捷、现代的文件管理专家</p>
    </div>
    <div class="login-box">
      <div class="box-header">
        <h2>{{ isLogin ? '欢迎回来' : '创建账号' }}</h2>
        <p>{{ isLogin ? '请登录您的账号以继续' : '注册一个新账号开始使用' }}</p>
      </div>
      <el-form :model="form" label-width="0">
        <el-form-item>
          <el-input v-model="form.username" placeholder="用户名" :prefix-icon="User" size="large" />
        </el-form-item>
        <el-form-item>
          <el-input 
            v-model="form.password" 
            type="password" 
            placeholder="密码" 
            :prefix-icon="Lock" 
            show-password
            size="large"
            @keyup.enter="handleSubmit"
          />
        </el-form-item>
        <el-button type="primary" class="submit-btn" :loading="loading" @click="handleSubmit" size="large">
          {{ isLogin ? '登录' : '立即注册' }}
        </el-button>
        <div class="toggle-mode" @click="toggleMode">
          {{ isLogin ? '还没有账号？立即注册' : '已有账号？返回登录' }}
        </div>
      </el-form>
    </div>
    <div class="footer-links">
      <span>隐私政策</span>
      <span class="divider">·</span>
      <span>服务条款</span>
      <span class="divider">·</span>
      <span>帮助中心</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { User, Lock } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage } from 'element-plus'

const router = useRouter()
const route = useRoute()
const isLogin = ref(true)
const loading = ref(false)
const form = reactive({
  username: '',
  password: ''
})

const toggleMode = () => {
  isLogin.value = !isLogin.value
  form.username = ''
  form.password = ''
}

const handleSubmit = async () => {
  if (!form.username || !form.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }

  loading.value = true
  try {
    const url = isLogin.value ? '/user/login' : '/user/register'
    const res: any = await request.post(url, form)
    
    if (isLogin.value) {
      localStorage.setItem('token', res.token)
      localStorage.setItem('username', res.username)
      ElMessage.success('登录成功')
      const redirect = route.query.redirect as string
      router.push(redirect || '/')
    } else {
      ElMessage.success('注册成功，请登录')
      isLogin.value = true
    }
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped lang="scss">
.login-container {
  height: 100vh;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background-color: var(--pan-bg);
  
  .brand {
    text-align: center;
    margin-bottom: 40px;
    
    img {
      width: 80px;
      height: 80px;
      margin-bottom: 16px;
      filter: drop-shadow(0 0 20px var(--pan-primary-glow));
    }
    
    h1 {
      font-size: 36px;
      font-weight: 800;
      color: var(--pan-text-main);
      margin: 0 0 8px;
      letter-spacing: -1.5px;
      background: linear-gradient(to bottom, #fff, #a1a1aa);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    
    p {
      color: var(--pan-text-muted);
      font-size: 16px;
      margin: 0;
    }
  }

  .login-box {
    width: 420px;
    padding: 48px;
    background: rgba(20, 20, 20, 0.4);
    backdrop-filter: var(--pan-glass-blur);
    border-radius: var(--pan-radius-lg);
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5);
    border: 1px solid var(--pan-border);

    .box-header {
      margin-bottom: 32px;
      text-align: center;
      
      h2 {
        font-size: 26px;
        font-weight: 700;
        color: var(--pan-text-main);
        margin: 0 0 8px;
      }
      
      p {
        font-size: 14px;
        color: var(--pan-text-muted);
        margin: 0;
      }
    }

    .el-form-item {
      margin-bottom: 24px;
    }

    :deep(.el-input__wrapper) {
      background-color: rgba(255, 255, 255, 0.03) !important;
      border: 1px solid var(--pan-border) !important;
      box-shadow: none !important;
      height: 48px;
      
      &.is-focus {
        border-color: var(--pan-primary) !important;
        box-shadow: 0 0 10px var(--pan-primary-glow) !important;
      }
    }

    .submit-btn {
      width: 100%;
      margin-top: 12px;
      height: 40px !important;
      font-size: 14px;
      font-weight: 600;
      border-radius: var(--pan-radius-sm) !important;
      background: var(--pan-primary) !important;
      border: none !important;
      color: #000000 !important;
      box-shadow: none !important;
      
      &:hover {
        background: var(--pan-primary-hover) !important;
        transform: none !important;
      }
    }

    .toggle-mode {
      margin-top: 24px;
      font-size: 14px;
      color: var(--pan-text-body);
      cursor: pointer;
      text-align: center;
      font-weight: 500;
      transition: var(--pan-transition);
      
      &:hover {
        color: var(--pan-primary);
      }
    }
  }

  .footer-links {
    margin-top: 60px;
    font-size: 13px;
    color: var(--pan-text-muted);
    display: flex;
    gap: 12px;
    align-items: center;
    
    span {
      cursor: pointer;
      &:hover {
        color: var(--pan-text-body);
      }
    }
    
    .divider {
      cursor: default;
      &:hover {
        color: var(--pan-text-muted);
      }
    }
  }
}
</style>
