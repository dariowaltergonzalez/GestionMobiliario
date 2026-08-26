import axios from 'axios'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5005'

// Los archivos subidos pueden venir como URL relativa (disco local, en desarrollo) o absoluta
// (Cloudinary, en producción) — esta función arma el src correcto para cualquiera de los dos casos.
export const resolveMediaUrl = (url: string) => url.startsWith('http') ? url : `${API_URL}${url}`

const client = axios.create({ baseURL: API_URL })

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  const tenant = localStorage.getItem('tenant')

  if (token) config.headers.Authorization = `Bearer ${token}`
  if (tenant) config.headers['X-Tenant'] = tenant

  return config
})

client.interceptors.response.use(
  (res) => res,
  (error) => {
    // Solo redirigir al login si ya había una sesión activa (token guardado)
    // No redirigir si el 401 viene del propio intento de login
    if (error.response?.status === 401 && localStorage.getItem('token')) {
      localStorage.removeItem('token')
      localStorage.removeItem('tenant')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default client
