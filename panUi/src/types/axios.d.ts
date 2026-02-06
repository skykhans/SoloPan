import 'axios'

declare module 'axios' {
  interface AxiosRequestConfig {
    _showError?: boolean
  }
}

