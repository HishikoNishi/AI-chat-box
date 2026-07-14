<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { UserPlus, Mail, Lock, AlertCircle, Sparkles } from '@lucide/vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref<string | null>(null)

async function handleSubmit(): Promise<void> {
  error.value = null

  if (!email.value.trim() || !password.value) {
    error.value = 'Vui lòng nhập email và mật khẩu.'
    return
  }

  if (password.value !== confirmPassword.value) {
    error.value = 'Mật khẩu xác nhận không khớp.'
    return
  }

  if (password.value.length < 8) {
    error.value = 'Mật khẩu phải có ít nhất 8 ký tự.'
    return
  }

  try {
    await authStore.register(email.value.trim(), password.value)
    await router.push('/chat')
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Đăng ký thất bại.'
  }
}
</script>

<template>
  <main class="auth-page">
    <section class="auth-card">
      <div class="auth-brand">
        <div class="auth-brand-icon">
          <Sparkles :size="30" :stroke-width="2" />
        </div>
        <h1>Tạo tài khoản</h1>
        <p>Bắt đầu hành trình chat cùng AI ngay hôm nay</p>
      </div>

      <p v-if="error" class="error-banner">
        <AlertCircle :size="18" :stroke-width="2" style="flex-shrink:0;margin-top:1px" />
        {{ error }}
      </p>

      <form @submit.prevent="handleSubmit">
        <div class="form-field">
          <label for="register-email">
            <Mail :size="14" /> Email
          </label>
          <div class="input-wrap">
            <Mail class="input-icon" :size="16" />
            <input
              id="register-email"
              v-model="email"
              type="email"
              autocomplete="email"
              placeholder="you@example.com"
              required
            />
          </div>
        </div>

        <div class="form-field">
          <label for="register-password">
            <Lock :size="14" /> Mật khẩu
          </label>
          <div class="input-wrap">
            <Lock class="input-icon" :size="16" />
            <input
              id="register-password"
              v-model="password"
              type="password"
              autocomplete="new-password"
              placeholder="Ít nhất 8 ký tự"
              required
            />
          </div>
        </div>

        <div class="form-field">
          <label for="register-confirm-password">
            <Lock :size="14" /> Xác nhận mật khẩu
          </label>
          <div class="input-wrap">
            <Lock class="input-icon" :size="16" />
            <input
              id="register-confirm-password"
              v-model="confirmPassword"
              type="password"
              autocomplete="new-password"
              placeholder="Nhập lại mật khẩu"
              required
            />
          </div>
        </div>

        <button class="btn btn-primary" type="submit" :disabled="authStore.loading">
          <UserPlus :size="18" />
          {{ authStore.loading ? 'Đang đăng ký...' : 'Đăng ký' }}
        </button>
      </form>

      <p class="auth-footer">
        Đã có tài khoản?
        <RouterLink to="/login">Đăng nhập</RouterLink>
      </p>
    </section>
  </main>
</template>
