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
  width: 100vw;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background-color: var(--pan-bg);
  position: relative;
  overflow: hidden;

  /* Decorative Background Elements */
  &::before, &::after {
    content: '';
    position: absolute;
    width: 400px;
    height: 400px;
    background-color: var(--pan-primary);
    filter: blur(150px);
    opacity: 0.08;
    border-radius: 50%;
    z-index: 0;
  }
  
  &::before { top: -100px; left: -100px; }
  &::after { bottom: -100px; right: -100px; }
  
  .brand {
    text-align: center;
    margin-bottom: 40px;
    z-index: 10;
    
    .logo-icon {
      width: 56px;
      height: 56px;
      color: var(--pan-primary);
      margin: 0 auto 16px;
      filter: drop-shadow(0 0 15px rgba(16, 185, 129, 0.4));
      
      svg { width: 100%; height: 100%; }
    }
    
    .logo-text {
      font-size: 32px;
      font-weight: 800;
      letter-spacing: -0.04em;
      margin-bottom: 12px;
      background: linear-gradient(135deg, #ffffff 0%, var(--pan-primary) 100%);
      -webkit-background-clip: text;
      background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    
    p { color: var(--pan-text-muted); font-size: 15px; font-weight: 500; }
  }

  .login-box {
    width: 420px;
    padding: 40px;
    background: rgba(255, 255, 255, 0.015);
    backdrop-filter: blur(20px);
    border: 1px solid var(--pan-border-strong);
    border-radius: var(--pan-radius-lg);
    box-shadow: 0 40px 100px -20px rgba(0, 0, 0, 0.8);
    position: relative;
    z-index: 10;
    animation: slideUp 0.6s cubic-bezier(0.16, 1, 0.3, 1);

    .box-header {
      text-align: center;
      margin-bottom: 32px;
      h2 { font-size: 24px; font-weight: 700; color: var(--pan-text-main); margin-bottom: 8px; }
      p { font-size: 14px; color: var(--pan-text-muted); }
    }

    .el-form-item { margin-bottom: 20px; }
    
    .input-tip { font-size: 11px; color: var(--pan-text-muted); margin-top: 4px; font-weight: 500; }
    
    .submit-btn {
      width: 100%;
      height: 48px !important;
      margin-top: 12px;
      font-size: 15px !important;
      font-weight: 700 !important;
      letter-spacing: 0.02em;
    }

    .form-actions {
      display: flex;
      justify-content: space-between;
      margin-top: 24px;
      font-size: 13px;
      
      .toggle-mode {
        color: var(--pan-text-body);
        cursor: pointer;
        font-weight: 600;
        transition: var(--pan-transition);
        &:hover { color: var(--pan-primary); }
      }

      .forgot-pwd {
        color: var(--pan-text-muted);
        cursor: pointer;
        transition: var(--pan-transition);
        &:hover { color: var(--pan-primary); }
      }
    }
  }

  .footer-links {
    margin-top: 60px;
    display: flex;
    gap: 12px;
    font-size: 12px;
    color: var(--pan-text-muted);
    font-weight: 500;
    z-index: 10;
    
    span:not(.divider) {
      cursor: pointer;
      &:hover { color: var(--pan-text-body); }
    }
  }

  .verify-code-row {
    display: flex;
    gap: 12px;
    .verify-btn { width: 120px; flex-shrink: 0; }
  }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}

:deep(.el-dialog) {
  .submit-btn { margin-top: 16px; }
}
</style>

