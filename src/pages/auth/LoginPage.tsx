import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Home, Eye, EyeOff, Loader2, AlertCircle } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import { login as apiLogin } from '../../api/auth'

export default function LoginPage() {
  const navigate = useNavigate()
  const { login } = useAuth()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [tenant, setTenant] = useState('garcia')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const data = await apiLogin({ email, password }, tenant)
      login(
        {
          nombre: data.nombre,
          apellido: data.apellido,
          email: data.email,
          rol: data.roles[0] ?? 'Operador',
          tenantId: data.tenantId,
        },
        data.accessToken,
        tenant
      )
      navigate('/dashboard')
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { errors?: string[] }; status?: number } }
      if (axiosError.response?.status === 401 || axiosError.response?.status === 400) {
        setError('Email o contraseña incorrectos.')
      } else if (axiosError.response?.status === 404) {
        setError('Inmobiliaria no encontrada. Verificá el identificador.')
      } else if (axiosError.response?.data?.errors?.length) {
        setError(axiosError.response.data.errors[0])
      } else {
        setError('No se pudo conectar con el servidor. Verificá que la API esté corriendo.')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 flex">

      {/* PANEL IZQUIERDO — decorativo */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden">
        <img
          src="https://images.unsplash.com/photo-1486325212027-8081e485255e?w=1200&q=90"
          alt="Edificios"
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-br from-blue-950/95 to-blue-900/80" />
        <div className="relative flex flex-col justify-between p-12 text-white">
          <Link to="/" className="flex items-center gap-2">
            <div className="w-8 h-8 bg-yellow-400 rounded-lg flex items-center justify-center">
              <Home className="w-4 h-4 text-blue-900" />
            </div>
            <span className="text-lg font-bold">García Propiedades</span>
          </Link>
          <div>
            <blockquote className="text-2xl font-light leading-relaxed text-blue-100 mb-6">
              "Gestioná tu inmobiliaria de forma <span className="text-yellow-400 font-semibold">simple y profesional</span>."
            </blockquote>
            <div className="flex gap-6 text-sm text-blue-300">
              <div>
                <div className="text-2xl font-bold text-white">500+</div>
                <div>Propiedades activas</div>
              </div>
              <div>
                <div className="text-2xl font-bold text-white">15</div>
                <div>Agentes registrados</div>
              </div>
              <div>
                <div className="text-2xl font-bold text-white">98%</div>
                <div>Clientes satisfechos</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* PANEL DERECHO — formulario */}
      <div className="flex-1 flex flex-col justify-center items-center p-8">
        <div className="w-full max-w-md">

          {/* Logo mobile */}
          <Link to="/" className="flex lg:hidden items-center gap-2 mb-8 justify-center">
            <div className="w-8 h-8 bg-blue-900 rounded-lg flex items-center justify-center">
              <Home className="w-4 h-4 text-yellow-400" />
            </div>
            <span className="text-lg font-bold text-blue-900">García Propiedades</span>
          </Link>

          <h1 className="text-2xl font-bold text-gray-900 mb-1">Bienvenido de vuelta</h1>
          <p className="text-gray-400 text-sm mb-8">Ingresá con tu cuenta para continuar</p>

          {error && (
            <div className="flex items-start gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                Inmobiliaria
              </label>
              <input
                type="text"
                value={tenant}
                onChange={(e) => setTenant(e.target.value)}
                placeholder="garcia"
                required
                className="w-full border border-gray-200 rounded-xl px-4 py-3 text-sm text-gray-700 placeholder-gray-300 outline-none focus:ring-2 focus:ring-blue-900/30 focus:border-blue-900 transition"
              />
              <p className="text-xs text-gray-400 mt-1">Identificador de tu inmobiliaria</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="tu@email.com"
                required
                className="w-full border border-gray-200 rounded-xl px-4 py-3 text-sm text-gray-700 placeholder-gray-300 outline-none focus:ring-2 focus:ring-blue-900/30 focus:border-blue-900 transition"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Contraseña</label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  required
                  className="w-full border border-gray-200 rounded-xl px-4 py-3 pr-11 text-sm text-gray-700 placeholder-gray-300 outline-none focus:ring-2 focus:ring-blue-900/30 focus:border-blue-900 transition"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-900 hover:bg-blue-800 disabled:opacity-60 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-xl transition-colors flex items-center justify-center gap-2"
            >
              {loading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Ingresando...
                </>
              ) : (
                'Ingresar al sistema'
              )}
            </button>
          </form>

          <div className="mt-8 pt-6 border-t border-gray-100 text-center">
            <Link to="/" className="text-sm text-gray-400 hover:text-blue-900 transition-colors">
              ← Volver al sitio
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
