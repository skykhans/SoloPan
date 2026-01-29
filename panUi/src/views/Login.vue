<template>
  <div class="login-container">
    <div class="login-box">
      <h2>{{ isLogin ? '登录网盘' : '注册账号' }}</h2>
      <el-form :model="form" label-width="0">
        <el-form-item>
          <el-input v-model="form.username" placeholder="用户名" :prefix-icon="User" />
        </el-form-item>
        <el-form-item>
          <el-input 
            v-model="form.password" 
            type="password" 
            placeholder="密码" 
            :prefix-icon="Lock" 
            show-password
            @keyup.enter="handleSubmit"
          />
        </el-form-item>
        <el-button type="primary" class="submit-btn" :loading="loading" @click="handleSubmit">
          {{ isLogin ? '登录' : '注册' }}
        </el-button>
        <div class="toggle-mode">
          <span @click="toggleMode">{{ isLogin ? '没有账号？去注册' : '已有账号？去登录' }}</span>
        </div>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { User, Lock } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage } from 'element-plus'

const router = useRouter()
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
      router.push('/')
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
  justify-content: center;
  align-items: center;
  background-color: #f0f2f5;
  background-image: url('https://gw.alipayobjects.com/zos/rmsportal/TVYTbAXWheQpRcWDaDMu.svg');
  
  .login-box {
    width: 360px;
    padding: 40px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    text-align: center;

    h2 {
      margin-bottom: 30px;
      color: #333;
    }

    .submit-btn {
      width: 100%;
      margin-top: 10px;
      height: 40px;
    }

    .toggle-mode {
      margin-top: 20px;
      font-size: 14px;
      color: #666;
      cursor: pointer;
      
      &:hover {
        color: var(--pan-primary);
      }
    }
  }
}
</style>
