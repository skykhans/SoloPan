<template>
  <div class="login-container">
    <div class="brand">
      <div class="logo-icon">
        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M12 4L4 8L12 12L20 8L12 4Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M4 12L12 16L20 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M4 16L12 20L20 16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <h1 class="logo-text">Trae Pan</h1>
      <p>安全、便捷、现代的文件管理专家</p>
    </div>
    <div class="login-box">
      <div class="box-header">
        <h2>{{ isLogin ? '欢迎回来' : '创建账号' }}</h2>
        <p>{{ isLogin ? '请登录您的账号以继续' : '注册一个新账号开始使用' }}</p>
      </div>
      <el-form :model="form" label-width="0">
        <el-form-item>
          <el-input v-model="form.username" :placeholder="isLogin ? '用户名' : '用户名 *'" :prefix-icon="User" size="large" />
        </el-form-item>
        <el-form-item>
          <el-input 
            v-model="form.password" 
            type="password" 
            :placeholder="isLogin ? '密码' : '密码 *'" 
            :prefix-icon="Lock" 
            show-password
            size="large"
            @keyup.enter="handleSubmit"
          />
          <div v-if="!isLogin" class="input-tip">密码至少8位，需包含字母和数字</div>
        </el-form-item>
        <el-form-item v-if="!isLogin">
          <el-input v-model="form.phone" placeholder="手机号 *" :prefix-icon="Iphone" size="large" />
        </el-form-item>
        <el-form-item v-if="!isLogin">
          <el-input v-model="form.email" placeholder="邮箱 *" :prefix-icon="Message" size="large" autocomplete="off" />
        </el-form-item>
        <el-form-item v-if="!isLogin">
          <div class="verify-code-row">
            <el-input v-model="form.verifyCode" placeholder="验证码 *" :prefix-icon="Checked" size="large" autocomplete="off" />
            <el-button 
              :disabled="!!countDown" 
              @click="sendRegisterCode" 
              size="large"
              class="verify-btn"
            >
              {{ countDown ? `${countDown}s` : '获取验证码' }}
            </el-button>
          </div>
        </el-form-item>
        <el-button type="primary" class="submit-btn" :loading="loading" @click="handleSubmit" size="large">
          {{ isLogin ? '登录' : '立即注册' }}
        </el-button>
        <div class="form-actions">
          <div class="toggle-mode" @click="toggleMode">
            {{ isLogin ? '还没有账号？立即注册' : '已有账号？返回登录' }}
          </div>
          <div v-if="isLogin" class="forgot-pwd" @click="showForgotDialog = true">找回密码</div>
        </div>
      </el-form>
    </div>

    <!-- 找回密码弹窗 -->
    <el-dialog v-model="showForgotDialog" title="找回密码" width="400px" append-to-body>
      <el-form :model="forgotForm" label-width="0" style="padding: 10px 10px">
        <el-form-item>
          <el-input v-model="forgotForm.target" placeholder="手机号或邮箱" :prefix-icon="Message" size="large" autocomplete="off" />
        </el-form-item>
        <el-form-item>
          <div class="verify-code-row">
            <el-input v-model="forgotForm.code" placeholder="验证码" :prefix-icon="Checked" size="large" autocomplete="off" />
            <el-button 
              :disabled="!!countDown" 
              @click="sendVerifyCode" 
              size="large"
              class="verify-btn"
            >
              {{ countDown ? `${countDown}s` : '获取验证码' }}
            </el-button>
          </div>
        </el-form-item>
        <el-form-item>
          <el-input 
            v-model="forgotForm.newPassword" 
            type="password" 
            placeholder="新密码" 
            :prefix-icon="Lock" 
            show-password 
            size="large" 
            autocomplete="new-password"
          />
          <div class="input-tip">密码至少8位，需包含字母和数字</div>
        </el-form-item>
        <el-button type="primary" class="submit-btn" style="width: 100%" :loading="loading" @click="handleResetPassword" size="large">
          确认重置
        </el-button>
      </el-form>
    </el-dialog>

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
import { useRouter } from 'vue-router'
import { User, Lock, Message, Checked, Iphone } from '@element-plus/icons-vue'
import request from '../utils/request'
import { ElMessage } from 'element-plus'

const router = useRouter()
const isLogin = ref(true)
const loading = ref(false)
const showForgotDialog = ref(false)
const countDown = ref(0)
let timer: any = null

const form = reactive({
  username: '',
  password: '',
  email: '',
  phone: '',
  verifyCode: ''
})

const forgotForm = reactive({
  target: '',
  code: '',
  newPassword: ''
})

// 校验工具函数
const validateEmail = (email: string) => /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(email)
const validatePhone = (phone: string) => /^1[3-9]\d{9}$/.test(phone)
const validatePassword = (pwd: string) => {
  if (pwd.length < 8) return '密码长度至少为 8 位'
  if (!/[a-zA-Z]/.test(pwd) || !/[0-9]/.test(pwd)) return '密码必须包含字母和数字'
  return null
}

const toggleMode = () => {
  isLogin.value = !isLogin.value
  form.username = ''
  form.password = ''
  form.email = ''
  form.phone = ''
}

const sendVerifyCode = async () => {
  if (!forgotForm.target) {
    ElMessage.warning('请输入手机号或邮箱')
    return
  }
  
  const isEmail = forgotForm.target.includes('@')
  if (isEmail && !validateEmail(forgotForm.target)) {
    ElMessage.warning('请输入有效的邮箱地址')
    return
  }
  if (!isEmail && !validatePhone(forgotForm.target)) {
    ElMessage.warning('请输入有效的手机号')
    return
  }

  try {
    const res: any = await request.post('/user/send-code', {
      target: forgotForm.target,
      type: isEmail ? 'email' : 'phone',
      scenario: 'reset'
    })
    
    ElMessage.success(`验证码已发送（测试用）：${res.code}`)
    startTimer()
  } catch (error) {
    console.error(error)
  }
}

const sendRegisterCode = async () => {
  // 注册时，优先验证手机号，没有则验证邮箱
  let target = ''
  let type = ''
  
  if (form.phone) {
    if (!validatePhone(form.phone)) return ElMessage.warning('请输入有效的手机号')
    target = form.phone
    type = 'phone'
  } else if (form.email) {
    if (!validateEmail(form.email)) return ElMessage.warning('请输入有效的邮箱地址')
    target = form.email
    type = 'email'
  } else {
    return ElMessage.warning('请输入手机号或邮箱以获取验证码')
  }

  try {
    const res: any = await request.post('/user/send-code', {
      target,
      type,
      scenario: 'register'
    })
    
    ElMessage.success(`验证码已发送（测试用）：${res.code}`)
    startTimer()
  } catch (error) {
    console.error(error)
  }
}

const startTimer = () => {
  countDown.value = 60
  if (timer) clearInterval(timer)
  timer = setInterval(() => {
    countDown.value--
    if (countDown.value <= 0) {
      clearInterval(timer)
      timer = null
    }
  }, 1000)
}

const handleResetPassword = async () => {
  if (!forgotForm.target || !forgotForm.code || !forgotForm.newPassword) {
    ElMessage.warning('请填写完整信息')
    return
  }

  const pwdError = validatePassword(forgotForm.newPassword)
  if (pwdError) {
    ElMessage.warning(pwdError)
    return
  }

  loading.value = true
  try {
    await request.post('/user/reset-password', forgotForm)
    ElMessage.success('密码重置成功，请登录')
    showForgotDialog.value = false
    forgotForm.target = ''
    forgotForm.code = ''
    forgotForm.newPassword = ''
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

const handleSubmit = async () => {
  if (!form.username || !form.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }
  
  if (!isLogin.value) {
    if (!form.phone && !form.email) return ElMessage.warning('手机号或邮箱至少填一项')
    if (!form.verifyCode) return ElMessage.warning('请输入验证码')
    
    const pwdError = validatePassword(form.password)
    if (pwdError) return ElMessage.warning(pwdError)
    
    if (form.phone && !validatePhone(form.phone)) return ElMessage.warning('无效的手机号格式')
    if (form.email && !validateEmail(form.email)) return ElMessage.warning('无效的邮箱格式')
  }

  loading.value = true
  try {
    if (isLogin.value) {
      const res: any = await request.post('/user/login', {
        username: form.username,
        password: form.password
      })
      localStorage.setItem('token', res.token)
      localStorage.setItem('username', res.username)
      ElMessage.success('登录成功')
      router.push('/files')
    } else {
      await request.post('/user/register', {
        username: form.username,
        password: form.password,
        email: form.email,
        phone: form.phone,
        verifyCode: form.verifyCode
      })
      ElMessage.success('注册成功，请登录')
      isLogin.value = true
      // 清空注册相关字段
      form.username = ''
      form.password = ''
      form.verifyCode = ''
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
    display: flex;
    flex-direction: column;
    align-items: center;
    
    .logo-icon {
      width: 64px;
      height: 64px;
      color: var(--pan-primary);
      margin-bottom: 16px;
      filter: drop-shadow(0 0 20px var(--pan-primary-glow));
      
      svg {
        width: 100%;
        height: 100%;
      }
    }
    
    h1.logo-text {
      font-size: 36px;
      font-weight: 800;
      color: var(--pan-text-main);
      margin: 0 0 8px;
      letter-spacing: -1.5px;
      background: linear-gradient(135deg, #ffffff 0%, var(--pan-primary) 100%);
      background-clip: text;
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
  }

  .login-box {
    .form-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: 24px;
      padding: 0 4px;

      .toggle-mode {
        font-size: 14px;
        color: var(--pan-text-body);
        cursor: pointer;
        font-weight: 500;
        transition: var(--pan-transition);
        margin: 0;
        
        &:hover {
          color: var(--pan-primary);
        }
      }

      .forgot-pwd {
        font-size: 14px;
        color: var(--pan-text-muted);
        cursor: pointer;
        transition: var(--pan-transition);

        &:hover {
          color: var(--pan-primary);
        }
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
