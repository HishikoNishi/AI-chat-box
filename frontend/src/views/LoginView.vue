<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bot, LogIn, Mail, Lock, AlertCircle } from '@lucide/vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)

const redirectPath = computed(() => {
  const redirect = route.query.redirect
  return typeof redirect === 'string' ? redirect : '/chat'
})

async function handleSubmit(): Promise<void> {
  error.value = null

  if (!email.value.trim() || !password.value) {
    error.value = 'Vui lòng nhập email và mật khẩu.'
    return
  }

  try {
    await authStore.login(email.value.trim(), password.value)
    await router.push(redirectPath.value)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Đăng nhập thất bại.'
  }
}
</script>

<template>
  <main class="auth-page">
    <section class="auth-card">
      <div class="auth-brand">
        <div class="auth-brand-icon">
          <Bot :size="32" :stroke-width="2" />
        </div>
        <h1>Chào mừng trở lại!</h1>
        <p>Đăng nhập để tiếp tục trò chuyện với AI</p>
      </div>

      <p v-if="error" class="error-banner">
        <AlertCircle :size="18" :stroke-width="2" style="flex-shrink:0;margin-top:1px" />
        {{ error }}
      </p>

      <form @submit.prevent="handleSubmit">
        <div class="form-field">
          <label for="login-email">
            <Mail :size="14" /> Email
          </label>
          <div class="input-wrap">
            <Mail class="input-icon" :size="16" />
            <input
              id="login-email"
              v-model="email"
              type="email"
              autocomplete="email"
              placeholder="you@example.com"
              required
            />
          </div>
        </div>

        <div class="form-field">
          <label for="login-password">
            <Lock :size="14" /> Mật khẩu
          </label>
          <div class="input-wrap">
            <Lock class="input-icon" :size="16" />
            <input
              id="login-password"
              v-model="password"
              type="password"
              autocomplete="current-password"
              placeholder="••••••••"
              required
            />
          </div>
        </div>

        <button class="btn btn-primary" type="submit" :disabled="authStore.loading">
          <LogIn :size="18" />
          {{ authStore.loading ? 'Đang đăng nhập...' : 'Đăng nhập' }}
        </button>
      </form>

      <p class="auth-footer">
        Chưa có tài khoản?
        <RouterLink to="/register">Đăng ký ngay</RouterLink>
      </p>
    </section>
  </main>
</template>
