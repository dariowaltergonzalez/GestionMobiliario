import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Home, ChevronLeft, CheckCircle, AlertCircle, User } from 'lucide-react'
import { getConfiguracionPublica, type ConfiguracionPublicaDto } from '../../api/configuracion'
import { solicitarTasacion } from '../../api/tasacion'

const TIPOS_PROPIEDAD = [
  { value: 1, label: 'Departamento' },
  { value: 2, label: 'Casa' },
  { value: 3, label: 'Local' },
  { value: 4, label: 'Oficina' },
  { value: 5, label: 'Terreno' },
  { value: 6, label: 'Galpón' },
  { value: 7, label: 'PH' },
  { value: 8, label: 'Otro' },
]

const ESTADOS_CONSERVACION = [
  { value: 1, label: 'Excelente' },
  { value: 2, label: 'Bueno' },
  { value: 3, label: 'Regular' },
  { value: 4, label: 'A refaccionar' },
]

const TIPOS_CONTACTO = [
  { value: 1, label: 'Llamada telefónica' },
  { value: 2, label: 'Visita presencial' },
  { value: 3, label: 'Videollamada / Online' },
]

const inputCls = 'w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

export default function TasacionPage() {
  const [config, setConfig] = useState<ConfiguracionPublicaDto | null>(null)
  const [enviado, setEnviado] = useState(false)
  const [error, setError] = useState('')
  const [cargando, setCargando] = useState(false)

  const [form, setForm] = useState({
    tipoPropiedad: '',
    direccion: '',
    barrio: '',
    ciudad: '',
    superficieTotal: '',
    superficieCubierta: '',
    ambientes: '',
    banios: '',
    antiguedad: '',
    estadoConservacion: '',
    descripcion: '',
    nombre: '',
    apellido: '',
    telefono: '',
    email: '',
    tipoContactoPreferido: '1',
  })

  useEffect(() => {
    getConfiguracionPublica()
      .then(res => { if (res.success && res.data) setConfig(res.data) })
      .catch(() => {})
    window.scrollTo(0, 0)
  }, [])

  const set = (field: string, value: string) =>
    setForm(f => ({ ...f, [field]: value }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (!form.nombre.trim() || !form.apellido.trim() || !form.telefono.trim() || !form.tipoPropiedad || !form.direccion.trim()) {
      setError('Completá los campos obligatorios: nombre, apellido, teléfono, tipo de propiedad y dirección.')
      return
    }

    setCargando(true)
    try {
      const res = await solicitarTasacion({
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        email: form.email.trim() || undefined,
        telefono: form.telefono.trim(),
        tipoPropiedad: Number(form.tipoPropiedad),
        direccion: form.direccion.trim(),
        barrio: form.barrio.trim() || undefined,
        ciudad: form.ciudad.trim() || undefined,
        superficieTotal: form.superficieTotal ? Number(form.superficieTotal) : undefined,
        superficieCubierta: form.superficieCubierta ? Number(form.superficieCubierta) : undefined,
        ambientes: form.ambientes ? Number(form.ambientes) : undefined,
        banios: form.banios ? Number(form.banios) : undefined,
        antiguedad: form.antiguedad ? Number(form.antiguedad) : undefined,
        estadoConservacion: Number(form.estadoConservacion) || 2,
        descripcion: form.descripcion.trim() || undefined,
        tipoContactoPreferido: Number(form.tipoContactoPreferido),
      }, config?.tenantSlug)

      if (res.success) {
        setEnviado(true)
        window.scrollTo(0, 0)
      } else {
        setError(res.errors?.[0] || 'No se pudo enviar la solicitud.')
      }
    } catch {
      setError('Error de conexión. Intentá de nuevo en unos minutos.')
    } finally {
      setCargando(false)
    }
  }

  const nombreEmpresa = config?.nombreComercial || 'Inmobiliaria'
  const whatsapp = config?.whatsApp
    ? `https://wa.me/${config.whatsApp.replace(/\D/g, '')}`
    : null

  if (enviado) {
    return (
      <div className="min-h-screen bg-gray-50 flex flex-col font-sans">
        <nav className="bg-white border-b border-gray-100 shadow-sm">
          <div className="max-w-4xl mx-auto px-4 h-16 flex items-center">
            <Link to="/" className="flex items-center gap-1 text-blue-900 hover:text-blue-700 text-sm font-medium">
              <ChevronLeft className="w-4 h-4" />
              {nombreEmpresa}
            </Link>
          </div>
        </nav>
        <div className="flex-1 flex items-center justify-center p-6">
          <div className="bg-white rounded-3xl shadow-lg p-10 max-w-md w-full text-center">
            <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-6">
              <CheckCircle className="w-10 h-10 text-green-600" />
            </div>
            <h2 className="text-2xl font-bold text-blue-900 mb-3">¡Solicitud enviada!</h2>
            <p className="text-gray-500 mb-8 leading-relaxed">
              Recibimos los datos de tu propiedad. Un agente te contactará a la brevedad para coordinar la tasación.
            </p>
            <div className="space-y-3">
              {whatsapp && (
                <a
                  href={whatsapp}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 bg-green-500 hover:bg-green-600 text-white font-bold px-6 py-3 rounded-xl transition-colors w-full"
                >
                  <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/>
                  </svg>
                  Consultar por WhatsApp
                </a>
              )}
              <Link
                to="/"
                className="flex items-center justify-center gap-2 border-2 border-blue-900 text-blue-900 font-semibold px-6 py-3 rounded-xl hover:bg-blue-50 transition-colors w-full"
              >
                Volver al inicio
              </Link>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 font-sans">

      {/* Navbar */}
      <nav className="bg-white border-b border-gray-100 shadow-sm sticky top-0 z-50">
        <div className="max-w-4xl mx-auto px-4 h-16 flex items-center">
          <Link to="/" className="flex items-center gap-1 text-blue-900 hover:text-blue-700 text-sm font-medium">
            <ChevronLeft className="w-4 h-4" />
            {nombreEmpresa}
          </Link>
        </div>
      </nav>

      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 py-10 px-4">
        <div className="max-w-4xl mx-auto text-center">
          <span className="inline-block bg-yellow-400 text-blue-900 text-xs font-bold uppercase tracking-widest px-3 py-1 rounded-full mb-4">
            Servicio gratuito
          </span>
          <h1 className="text-3xl font-bold text-white mb-2">Tasación de tu propiedad</h1>
          <p className="text-blue-200 text-sm">
            Completá el formulario y un agente especializado te contactará para coordinar la tasación.
          </p>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="max-w-4xl mx-auto px-4 py-8 space-y-6">

        {/* Propiedad */}
        <div className="bg-white rounded-2xl shadow-sm p-6">
          <h2 className="text-lg font-bold text-blue-900 mb-5 flex items-center gap-2">
            <Home className="w-5 h-5 text-yellow-500" />
            Datos de la propiedad
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Tipo de propiedad <span className="text-red-500">*</span>
              </label>
              <select
                required
                value={form.tipoPropiedad}
                onChange={e => set('tipoPropiedad', e.target.value)}
                className={inputCls}
              >
                <option value="">Seleccioná...</option>
                {TIPOS_PROPIEDAD.map(t => (
                  <option key={t.value} value={t.value}>{t.label}</option>
                ))}
              </select>
            </div>

            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Dirección <span className="text-red-500">*</span>
              </label>
              <input
                required
                type="text"
                placeholder="Ej: Av. Corrientes 1234"
                value={form.direccion}
                onChange={e => set('direccion', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Barrio</label>
              <input
                type="text"
                placeholder="Ej: Palermo"
                value={form.barrio}
                onChange={e => set('barrio', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Ciudad</label>
              <input
                type="text"
                placeholder="Ej: Buenos Aires"
                value={form.ciudad}
                onChange={e => set('ciudad', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Superficie total (m²)</label>
              <input
                type="number"
                min="0"
                placeholder="Ej: 120"
                value={form.superficieTotal}
                onChange={e => set('superficieTotal', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Superficie cubierta (m²)</label>
              <input
                type="number"
                min="0"
                placeholder="Ej: 95"
                value={form.superficieCubierta}
                onChange={e => set('superficieCubierta', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Ambientes</label>
              <input
                type="number"
                min="1"
                max="20"
                placeholder="Ej: 3"
                value={form.ambientes}
                onChange={e => set('ambientes', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Baños</label>
              <input
                type="number"
                min="1"
                max="10"
                placeholder="Ej: 2"
                value={form.banios}
                onChange={e => set('banios', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Antigüedad (años)</label>
              <input
                type="number"
                min="0"
                placeholder="Ej: 15"
                value={form.antiguedad}
                onChange={e => set('antiguedad', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Estado de conservación</label>
              <select
                value={form.estadoConservacion}
                onChange={e => set('estadoConservacion', e.target.value)}
                className={inputCls}
              >
                <option value="">Seleccioná...</option>
                {ESTADOS_CONSERVACION.map(ec => (
                  <option key={ec.value} value={ec.value}>{ec.label}</option>
                ))}
              </select>
            </div>

            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">Descripción adicional</label>
              <textarea
                rows={3}
                placeholder="Características destacadas, reformas realizadas, garage, jardín, etc."
                value={form.descripcion}
                onChange={e => set('descripcion', e.target.value)}
                className={`${inputCls} resize-none`}
              />
            </div>

          </div>
        </div>

        {/* Contacto */}
        <div className="bg-white rounded-2xl shadow-sm p-6">
          <h2 className="text-lg font-bold text-blue-900 mb-5 flex items-center gap-2">
            <User className="w-5 h-5 text-yellow-500" />
            Tus datos de contacto
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Nombre <span className="text-red-500">*</span>
              </label>
              <input
                required
                type="text"
                placeholder="Ej: Juan"
                value={form.nombre}
                onChange={e => set('nombre', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Apellido <span className="text-red-500">*</span>
              </label>
              <input
                required
                type="text"
                placeholder="Ej: García"
                value={form.apellido}
                onChange={e => set('apellido', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Teléfono / WhatsApp <span className="text-red-500">*</span>
              </label>
              <input
                required
                type="tel"
                placeholder="Ej: +54 11 1234-5678"
                value={form.telefono}
                onChange={e => set('telefono', e.target.value)}
                className={inputCls}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input
                type="email"
                placeholder="Ej: juan@gmail.com"
                value={form.email}
                onChange={e => set('email', e.target.value)}
                className={inputCls}
              />
            </div>

            <div className="sm:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                ¿Cómo preferís que te contactemos?
              </label>
              <div className="flex flex-wrap gap-3">
                {TIPOS_CONTACTO.map(t => (
                  <label
                    key={t.value}
                    className={`flex items-center gap-2 px-4 py-2.5 rounded-xl border-2 cursor-pointer transition-colors text-sm font-medium select-none ${
                      form.tipoContactoPreferido === String(t.value)
                        ? 'border-blue-900 bg-blue-50 text-blue-900'
                        : 'border-gray-200 text-gray-600 hover:border-gray-300'
                    }`}
                  >
                    <input
                      type="radio"
                      name="tipoContacto"
                      value={t.value}
                      checked={form.tipoContactoPreferido === String(t.value)}
                      onChange={e => set('tipoContactoPreferido', e.target.value)}
                      className="sr-only"
                    />
                    {t.label}
                  </label>
                ))}
              </div>
            </div>

          </div>
        </div>

        {/* Error */}
        {error && (
          <div className="flex items-start gap-3 bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700">
            <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
            {error}
          </div>
        )}

        {/* Submit */}
        <button
          type="submit"
          disabled={cargando}
          className="w-full bg-yellow-400 hover:bg-yellow-500 disabled:opacity-60 text-blue-900 font-bold py-4 rounded-xl transition-colors text-base"
        >
          {cargando ? 'Enviando solicitud...' : 'Solicitar tasación gratuita'}
        </button>

        <p className="text-center text-xs text-gray-400 pb-6">
          Campos marcados con <span className="text-red-500">*</span> son obligatorios.
          Un agente te contactará en menos de 24 hs hábiles.
        </p>

      </form>
    </div>
  )
}
