import React, { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Home, Eye, EyeOff, Loader2, AlertCircle, ArrowLeft } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import { login as apiLogin, resolverTenant, type TenantLoginDto } from '../../api/auth'
import { getConfiguracionPublica } from '../../api/configuracion'

type Paso = 'email' | 'selector' | 'password'

export default function LoginPage() {
  const navigate = useNavigate()
  const { login } = useAuth()

  const [paso, setPaso] = useState<Paso>('email')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [tenants, setTenants] = useState<TenantLoginDto[]>([])
  const [tenantSeleccionado, setTenantSeleccionado] = useState<TenantLoginDto | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [nombreEmpresa, setNombreEmpresa] = useState('GestionInmobiliaria')

  useEffect(() => {
    getConfiguracionPublica()
      .then(res => { if (res.success && res.data) setNombreEmpresa(res.data.nombreComercial) })
      .catch(() => {})
  }, [])

  const handleEmailSubmit = async (e: React.SyntheticEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const lista = await resolverTenant(email)

      if (lista.length === 0) {
        setError('No encontramos una cuenta con ese email.')
        return
      }

      if (lista.length === 1) {
        setTenantSeleccionado(lista[0])
        setPaso('password')
      } else {
        setTenants(lista)
        setPaso('selector')
      }
    } catch {
      setError('No se pudo conectar con el servidor.')
    } finally {
      setLoading(false)
    }
  }

  const handleSeleccionarTenant = (t: TenantLoginDto) => {
    setTenantSeleccionado(t)
    setPaso('password')
  }

  const handlePasswordSubmit = async (e: React.SyntheticEvent) => {
    e.preventDefault()
    if (!tenantSeleccionado) return
    setError('')
    setLoading(true)

    try {
      const data = await apiLogin({ email, password }, tenantSeleccionado.slug)
      login(
        {
          nombre: data.nombre,
          apellido: data.apellido,
          email: data.email,
          rol: data.roles[0] ?? 'Operador',
          tenantId: data.tenantId,
          agenteId: data.agenteId ?? null,
        },
        data.accessToken,
        tenantSeleccionado.slug
      )
      navigate('/dashboard')
    } catch (err: unknown) {
      const axiosError = err as { response?: { status?: number } }
      if (axiosError.response?.status === 401 || axiosError.response?.status === 400) {
        setError('Contraseña incorrecta.')
      } else {
        setError('No se pudo conectar con el servidor.')
      }
    } finally {
      setLoading(false)
    }
  }

  const volverAEmail = () => {
    setPaso('email')
    setTenantSeleccionado(null)
    setTenants([])
    setError('')
    setPassword('')
  }

  return (
    <div className="min-h-screen bg-gray-50 flex">

      {/* Panel izquierdo decorativo */}
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
            <span className="text-lg font-bold">{nombreEmpresa}</span>
          </Link>
          <div>
            <blockquote className="text-2xl font-light leading-relaxed text-blue-100 mb-6">
              "Gestioná tu inmobiliaria de forma <span className="text-yellow-400 font-semibold">simple y profesional</span>."
            </blockquote>
            <div className="flex gap-6 text-sm text-blue-300">
              <div><div className="text-2xl font-bold text-white">500+</div><div>Propiedades activas</div></div>
              <div><div className="text-2xl font-bold text-white">15</div><div>Agentes registrados</div></div>
              <div><div className="text-2xl font-bold text-white">98%</div><div>Clientes satisfechos</div></div>
            </div>
          </div>
        </div>
      </div>

      {/* Panel derecho — formulario */}
      <div className="flex-1 flex flex-col justify-center items-center p-8">
        <div className="w-full max-w-md">

          {/* Logo mobile */}
          <Link to="/" className="flex lg:hidden items-center gap-2 mb-8 justify-center">
            <div className="w-8 h-8 bg-blue-900 rounded-lg flex items-center justify-center">
              <Home className="w-4 h-4 text-yellow-400" />
            </div>
            <span className="text-lg font-bold text-blue-900">{nombreEmpresa}</span>
          </Link>

          <h1 className="text-2xl font-bold text-gray-900 mb-1">Bienvenido de vuelta</h1>
          <p className="text-gray-400 text-sm mb-8">Ingresá con tu cuenta para continuar</p>

          {error && (
            <div className="flex items-start gap-3 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-6 text-sm">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* PASO 1 — Email */}
          {paso === 'email' && (
            <form onSubmit={handleEmailSubmit} className="space-y-5">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
                <input
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="tu@email.com"
                  required
                  autoFocus
                  className="w-full border border-gray-200 rounded-xl px-4 py-3 text-sm text-gray-700 placeholder-gray-300 outline-none focus:ring-2 focus:ring-blue-900/30 focus:border-blue-900 transition"
                />
              </div>
              <button
                type="submit"
                disabled={loading}
                className="w-full bg-blue-900 hover:bg-blue-800 disabled:opacity-60 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-xl transition-colors flex items-center justify-center gap-2"
              >
                {loading ? <><Loader2 className="w-4 h-4 animate-spin" />Verificando...</> : 'Siguiente'}
              </button>
            </form>
          )}

          {/* PASO 1.5 — Selector de empresa (solo si hay más de una) */}
          {paso === 'selector' && (
            <div className="space-y-4">
              <p className="text-sm text-gray-500">Tu email está asociado a más de una empresa. Seleccioná con cuál querés ingresar:</p>
              <div className="space-y-2">
                {tenants.map(t => (
                  <button
                    key={t.slug}
                    onClick={() => handleSeleccionarTenant(t)}
                    className="w-full text-left border border-gray-200 hover:border-blue-900 hover:bg-blue-50 rounded-xl px-4 py-3 text-sm font-medium text-gray-700 transition"
                  >
                    {t.nombre}
                  </button>
                ))}
              </div>
              <button onClick={volverAEmail} className="flex items-center gap-1 text-sm text-gray-400 hover:text-blue-900 transition-colors mt-2">
                <ArrowLeft className="w-3.5 h-3.5" /> Cambiar email
              </button>
            </div>
          )}

          {/* PASO 2 — Contraseña */}
          {paso === 'password' && (
            <form onSubmit={handlePasswordSubmit} className="space-y-5">
              <div className="bg-gray-50 rounded-xl px-4 py-3 text-sm text-gray-500 space-y-0.5">
                <div className="font-medium text-gray-700">{tenantSeleccionado?.nombre}</div>
                <div>{email}</div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1.5">Contraseña</label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    placeholder="••••••••"
                    required
                    autoFocus
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
                {loading ? <><Loader2 className="w-4 h-4 animate-spin" />Ingresando...</> : 'Ingresar al sistema'}
              </button>
              <button type="button" onClick={volverAEmail} className="flex items-center gap-1 text-sm text-gray-400 hover:text-blue-900 transition-colors">
                <ArrowLeft className="w-3.5 h-3.5" /> Cambiar email
              </button>
            </form>
          )}

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
