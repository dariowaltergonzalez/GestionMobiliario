import { useState, useEffect, useRef } from 'react'
import { X, Loader2, ImagePlus, Star, Trash2, Upload, Video, VideoOff } from 'lucide-react'
import {
  createPropiedad, updatePropiedad, subirFotosPropiedad, setFotoPrincipal, deleteFotoPropiedad,
  subirVideoPropiedad, deleteVideoPropiedad,
  propiedadFormVacio, TIPOS_PROPIEDAD, TIPOS_OPERACION, ESTADOS_PROPIEDAD, ESTADOS_CONSERVACION,
  type PropiedadDto, type PropiedadFormData, type FotoPropiedadDto,
} from '../../../api/propiedades'
import { getPropietariosActivos, type PropietarioComboDto } from '../../../api/propietarios'

const API_URL = 'http://localhost:5005'

interface Props {
  propiedad: PropiedadDto | null
  onGuardado: () => void
  onCerrar: () => void
}

const Input = ({ label, ...props }: { label: string } & React.InputHTMLAttributes<HTMLInputElement>) => (
  <div>
    <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
    <input
      {...props}
      className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition disabled:bg-gray-50"
    />
  </div>
)

const Select = ({ label, children, ...props }: { label: string } & React.SelectHTMLAttributes<HTMLSelectElement> & { children: React.ReactNode }) => (
  <div>
    <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
    <select
      {...props}
      className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition bg-white"
    >
      {children}
    </select>
  </div>
)

const Checkbox = ({ label, ...props }: { label: string } & React.InputHTMLAttributes<HTMLInputElement>) => (
  <label className="flex items-center gap-2 cursor-pointer">
    <input type="checkbox" {...props} className="w-4 h-4 rounded accent-blue-900" />
    <span className="text-sm text-gray-600">{label}</span>
  </label>
)

const CurrencyInput = ({ label, value, onChange, required, placeholder, prefix = '$' }: {
  label: string
  value: string
  onChange: (val: string) => void
  required?: boolean
  placeholder?: string
  prefix?: string
}) => {
  const [focused, setFocused] = useState(false)
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(e.target.value.replace(/\D/g, ''))
  }
  const display = focused ? value : (value ? Number(value).toLocaleString('es-AR') : '')
  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
      <div className="flex items-center border border-gray-200 rounded-lg focus-within:ring-2 focus-within:ring-blue-900/20 focus-within:border-blue-900 transition bg-white overflow-hidden">
        <span className="pl-3 pr-1 text-sm text-gray-400 select-none">{prefix}</span>
        <input
          type="text" inputMode="numeric" required={required} placeholder={placeholder}
          value={display} onChange={handleChange}
          onFocus={() => setFocused(true)} onBlur={() => setFocused(false)}
          className="flex-1 pr-3 py-2 text-sm text-gray-700 outline-none"
        />
      </div>
    </div>
  )
}

// ---------- Sección de fotos ----------

function FotosSection({ propiedadId, fotosIniciales, fotasPendientes, onFotosPendientesChange }: {
  propiedadId: number | null
  fotosIniciales: FotoPropiedadDto[]
  fotasPendientes: File[]
  onFotosPendientesChange: (files: File[]) => void
}) {
  const [fotos, setFotos] = useState<FotoPropiedadDto[]>(fotosIniciales)
  const [accion, setAccion] = useState<string>('')
  const [previews, setPreviews] = useState<string[]>([])
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    setFotos(fotosIniciales)
  }, [fotosIniciales])

  const handleArchivos = (files: FileList | null) => {
    if (!files) return
    const nuevos = Array.from(files).filter(f => f.type.startsWith('image/'))
    const total = [...fotasPendientes, ...nuevos]
    onFotosPendientesChange(total)
    setPreviews(prev => [
      ...prev,
      ...nuevos.map(f => URL.createObjectURL(f))
    ])
  }

  const quitarPendiente = (idx: number) => {
    URL.revokeObjectURL(previews[idx])
    onFotosPendientesChange(fotasPendientes.filter((_, i) => i !== idx))
    setPreviews(prev => prev.filter((_, i) => i !== idx))
  }

  const handleSetPrincipal = async (fotoId: number) => {
    if (!propiedadId) return
    setAccion(`principal-${fotoId}`)
    try {
      await setFotoPrincipal(propiedadId, fotoId)
      setFotos(prev => prev.map(f => ({ ...f, esPrincipal: f.id === fotoId })))
    } finally {
      setAccion('')
    }
  }

  const handleEliminar = async (fotoId: number) => {
    if (!propiedadId) return
    setAccion(`delete-${fotoId}`)
    try {
      await deleteFotoPropiedad(propiedadId, fotoId)
      setFotos(prev => prev.filter(f => f.id !== fotoId))
    } finally {
      setAccion('')
    }
  }

  const isDragging = accion === 'drag'

  return (
    <div>
      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Fotos</p>

      {/* Fotos existentes */}
      {fotos.length > 0 && (
        <div className="grid grid-cols-4 gap-2 mb-3">
          {fotos.map(f => (
            <div key={f.id} className="relative group rounded-lg overflow-hidden border border-gray-200 aspect-square bg-gray-50">
              <img
                src={`${API_URL}${f.url}`}
                alt={f.nombreArchivo}
                className="w-full h-full object-cover"
              />
              {f.esPrincipal && (
                <span className="absolute top-1 left-1 bg-yellow-400 text-blue-900 text-[10px] font-bold px-1.5 py-0.5 rounded-full flex items-center gap-0.5">
                  <Star className="w-2.5 h-2.5" /> Principal
                </span>
              )}
              <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-all flex items-center justify-center gap-1 opacity-0 group-hover:opacity-100">
                {!f.esPrincipal && (
                  <button
                    type="button"
                    onClick={() => handleSetPrincipal(f.id)}
                    disabled={!!accion}
                    title="Marcar como principal"
                    className="p-1.5 bg-yellow-400 text-blue-900 rounded-lg hover:bg-yellow-300 disabled:opacity-50"
                  >
                    {accion === `principal-${f.id}` ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Star className="w-3.5 h-3.5" />}
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => handleEliminar(f.id)}
                  disabled={!!accion}
                  title="Eliminar foto"
                  className="p-1.5 bg-red-500 text-white rounded-lg hover:bg-red-600 disabled:opacity-50"
                >
                  {accion === `delete-${f.id}` ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Trash2 className="w-3.5 h-3.5" />}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Previews de fotos pendientes */}
      {previews.length > 0 && (
        <div className="grid grid-cols-4 gap-2 mb-3">
          {previews.map((src, idx) => (
            <div key={idx} className="relative group rounded-lg overflow-hidden border-2 border-dashed border-blue-300 aspect-square bg-blue-50">
              <img src={src} alt="" className="w-full h-full object-cover opacity-80" />
              <span className="absolute top-1 left-1 bg-blue-600 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full">
                Por subir
              </span>
              <button
                type="button"
                onClick={() => quitarPendiente(idx)}
                className="absolute top-1 right-1 bg-red-500 text-white p-0.5 rounded-full opacity-0 group-hover:opacity-100 transition-opacity"
              >
                <X className="w-3 h-3" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Zona de drop / selector */}
      <div
        onDragOver={e => { e.preventDefault(); setAccion('drag') }}
        onDragLeave={() => setAccion('')}
        onDrop={e => { e.preventDefault(); setAccion(''); handleArchivos(e.dataTransfer.files) }}
        onClick={() => inputRef.current?.click()}
        className={`border-2 border-dashed rounded-xl p-5 flex flex-col items-center gap-2 cursor-pointer transition-colors ${
          isDragging ? 'border-blue-600 bg-blue-50' : 'border-gray-200 hover:border-blue-400 hover:bg-gray-50'
        }`}
      >
        {isDragging ? (
          <Upload className="w-6 h-6 text-blue-600" />
        ) : (
          <ImagePlus className="w-6 h-6 text-gray-400" />
        )}
        <p className="text-sm text-gray-500">
          {isDragging ? 'Soltá las imágenes aquí' : 'Arrastrá fotos o hacé clic para seleccionar'}
        </p>
        <p className="text-xs text-gray-400">JPG, PNG, WebP · máx. 10 MB c/u</p>
        {!propiedadId && (
          <p className="text-xs text-blue-600 font-medium">Las fotos se subirán al guardar la propiedad</p>
        )}
        <input
          ref={inputRef}
          type="file"
          multiple
          accept="image/jpeg,image/png,image/webp"
          className="hidden"
          onChange={e => handleArchivos(e.target.files)}
        />
      </div>
    </div>
  )
}

// ---------- Sección de video ----------

function VideoSection({ propiedadId, videoUrlInicial, videoPendiente, onVideoPendienteChange }: {
  propiedadId: number | null
  videoUrlInicial: string | null
  videoPendiente: File | null
  onVideoPendienteChange: (file: File | null) => void
}) {
  const [videoUrl, setVideoUrl] = useState<string | null>(videoUrlInicial)
  const [eliminando, setEliminando] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const handleArchivo = (file: File | null) => {
    if (!file) return
    if (!['video/mp4', 'video/quicktime', 'video/webm'].includes(file.type)) {
      alert('Formato no permitido. Use MP4, MOV o WebM.')
      return
    }
    if (file.size > 200 * 1024 * 1024) {
      alert('El video supera el límite de 200 MB.')
      return
    }
    onVideoPendienteChange(file)
  }

  const handleEliminar = async () => {
    if (!propiedadId || !videoUrl) return
    setEliminando(true)
    try {
      await deleteVideoPropiedad(propiedadId)
      setVideoUrl(null)
      onVideoPendienteChange(null)
    } finally {
      setEliminando(false)
    }
  }

  const previewUrl = videoPendiente ? URL.createObjectURL(videoPendiente) : null

  return (
    <div>
      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Video</p>

      {/* Video existente */}
      {videoUrl && !videoPendiente && (
        <div className="mb-3 rounded-xl overflow-hidden border border-gray-200 bg-gray-50">
          <video src={`${API_URL}${videoUrl}`} controls className="w-full max-h-48" preload="metadata" />
          <div className="flex items-center justify-between px-3 py-2">
            <span className="text-xs text-gray-500 flex items-center gap-1.5"><Video className="w-3.5 h-3.5" /> Video cargado</span>
            <button type="button" onClick={handleEliminar} disabled={eliminando}
              className="flex items-center gap-1 text-xs text-red-500 hover:text-red-700 disabled:opacity-50">
              {eliminando ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Trash2 className="w-3.5 h-3.5" />}
              Eliminar
            </button>
          </div>
        </div>
      )}

      {/* Preview del video pendiente */}
      {videoPendiente && previewUrl && (
        <div className="mb-3 rounded-xl overflow-hidden border-2 border-dashed border-blue-300 bg-blue-50">
          <video src={previewUrl} controls className="w-full max-h-48" preload="metadata" />
          <div className="flex items-center justify-between px-3 py-2">
            <span className="text-xs text-blue-600 flex items-center gap-1.5 font-medium"><Video className="w-3.5 h-3.5" /> {videoPendiente.name} · Por subir</span>
            <button type="button" onClick={() => onVideoPendienteChange(null)}
              className="flex items-center gap-1 text-xs text-red-500 hover:text-red-700">
              <X className="w-3.5 h-3.5" /> Quitar
            </button>
          </div>
        </div>
      )}

      {/* Selector */}
      {!videoPendiente && (
        <div
          onClick={() => inputRef.current?.click()}
          className="border-2 border-dashed border-gray-200 hover:border-blue-400 hover:bg-gray-50 rounded-xl p-5 flex flex-col items-center gap-2 cursor-pointer transition-colors"
        >
          {videoUrl ? <VideoOff className="w-6 h-6 text-gray-400" /> : <Video className="w-6 h-6 text-gray-400" />}
          <p className="text-sm text-gray-500">{videoUrl ? 'Reemplazar video' : 'Seleccionar video'}</p>
          <p className="text-xs text-gray-400">MP4, MOV, WebM · máx. 200 MB</p>
          {!propiedadId && <p className="text-xs text-blue-600 font-medium">El video se subirá al guardar la propiedad</p>}
          <input ref={inputRef} type="file" accept="video/mp4,video/quicktime,video/webm" className="hidden"
            onChange={e => handleArchivo(e.target.files?.[0] ?? null)} />
        </div>
      )}
    </div>
  )
}

// ---------- Formulario principal ----------

export default function PropiedadForm({ propiedad, onGuardado, onCerrar }: Props) {
  const [form, setForm] = useState<PropiedadFormData>(propiedadFormVacio)
  const [propietarios, setPropietarios] = useState<PropietarioComboDto[]>([])
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')
  const [fotosPendientes, setFotosPendientes] = useState<File[]>([])
  const [videoPendiente, setVideoPendiente] = useState<File | null>(null)

  useEffect(() => {
    getPropietariosActivos().then(res => {
      if (res.success) setPropietarios(res.data)
    }).catch(() => {})
  }, [])

  useEffect(() => {
    if (propiedad) {
      setForm({
        tipo: propiedad.tipo,
        operacion: propiedad.operacion,
        direccion: propiedad.direccion,
        barrio: propiedad.barrio ?? '',
        ciudad: propiedad.ciudad ?? '',
        provincia: propiedad.provincia ?? '',
        piso: propiedad.piso ?? '',
        numeroDepartamento: propiedad.numeroDepartamento ?? '',
        ambientes: propiedad.ambientes?.toString() ?? '',
        dormitorios: propiedad.dormitorios?.toString() ?? '',
        banios: propiedad.banios?.toString() ?? '',
        superficieTotal: propiedad.superficieTotal?.toString() ?? '',
        superficieCubierta: propiedad.superficieCubierta?.toString() ?? '',
        antiguedad: propiedad.antiguedad?.toString() ?? '',
        precioAlquiler: propiedad.precioAlquiler?.toString() ?? '',
        precioVenta: propiedad.precioVenta?.toString() ?? '',
        expensas: propiedad.expensas?.toString() ?? '',
        estado: propiedad.estado,
        estadoConservacion: propiedad.estadoConservacion ?? '',
        cochera: propiedad.cochera,
        tieneCalefaccion: propiedad.tieneCalefaccion,
        aceptaMascotas: propiedad.aceptaMascotas,
        nroCatastro: propiedad.nroCatastro ?? '',
        descripcion: propiedad.descripcion ?? '',
        notas: propiedad.notas ?? '',
        propietarioId: propiedad.propietarioId.toString(),
      })
    }
  }, [propiedad])

  const set = (campo: keyof PropiedadFormData, valor: PropiedadFormData[typeof campo]) =>
    setForm(f => ({ ...f, [campo]: valor }))

  const mostrarAlquiler = form.operacion === 1 || form.operacion === 3
  const mostrarVenta = form.operacion === 2 || form.operacion === 3

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.propietarioId) { setError('Seleccioná un propietario.'); return }

    if (mostrarAlquiler && !mostrarVenta && (!form.precioAlquiler || Number(form.precioAlquiler) <= 0)) {
      setError('El precio de alquiler debe ser mayor a cero.'); return
    }
    if (mostrarVenta && !mostrarAlquiler && (!form.precioVenta || Number(form.precioVenta) <= 0)) {
      setError('El precio de venta debe ser mayor a cero.'); return
    }
    if (mostrarAlquiler && mostrarVenta && (!form.precioAlquiler || Number(form.precioAlquiler) <= 0) && (!form.precioVenta || Number(form.precioVenta) <= 0)) {
      setError('Ingresá al menos un precio (alquiler o venta).'); return
    }

    setError('')
    setGuardando(true)
    try {
      let propiedadId: number
      if (propiedad) {
        await updatePropiedad(propiedad.id, form)
        propiedadId = propiedad.id
      } else {
        const res = await createPropiedad(form)
        propiedadId = (res.data as { id: number }).id
      }

      if (fotosPendientes.length > 0) {
        await subirFotosPropiedad(propiedadId, fotosPendientes)
      }
      if (videoPendiente) {
        await subirVideoPropiedad(propiedadId, videoPendiente)
      }

      onGuardado()
    } catch (err: unknown) {
      const axErr = err as { response?: { status?: number; data?: { errors?: string[]; message?: string; title?: string } } }
      const msg = axErr.response?.data?.errors?.[0]
        ?? axErr.response?.data?.message
        ?? axErr.response?.data?.title
        ?? `Error ${axErr.response?.status ?? ''} al guardar la propiedad.`
      setError(msg)
    } finally {
      setGuardando(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl w-full max-w-2xl max-h-[90vh] flex flex-col shadow-2xl">

        {/* HEADER */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-bold text-gray-800 text-lg">
            {propiedad ? 'Editar propiedad' : 'Nueva propiedad'}
          </h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        {/* FORM */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto">
          <div className="px-6 py-5 space-y-5">

            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">{error}</div>
            )}

            {/* Propietario */}
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Propietario *</label>
              <select
                value={form.propietarioId}
                onChange={e => set('propietarioId', e.target.value)}
                required
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition bg-white"
              >
                <option value="">Seleccioná un propietario...</option>
                {propietarios.map(p => (
                  <option key={p.id} value={p.id}>{p.nombreCompleto}</option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-3 gap-4">
              <Select label="Tipo de propiedad *" value={form.tipo} onChange={e => set('tipo', Number(e.target.value) as PropiedadFormData['tipo'])}>
                {Object.entries(TIPOS_PROPIEDAD).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </Select>
              <Select label="Operación *" value={form.operacion} onChange={e => set('operacion', Number(e.target.value) as PropiedadFormData['operacion'])}>
                {Object.entries(TIPOS_OPERACION).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </Select>
              <Select label="Estado *" value={form.estado} onChange={e => set('estado', Number(e.target.value) as PropiedadFormData['estado'])}>
                {Object.entries(ESTADOS_PROPIEDAD).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </Select>
            </div>

            {/* Ubicación */}
            <div>
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Ubicación</p>
              <div className="space-y-3">
                <Input label="Dirección *" value={form.direccion} onChange={e => set('direccion', e.target.value)} required placeholder="Av. Corrientes 1234" />
                <div className="grid grid-cols-3 gap-3">
                  <Input label="Barrio" value={form.barrio} onChange={e => set('barrio', e.target.value)} placeholder="Palermo" />
                  <Input label="Ciudad" value={form.ciudad} onChange={e => set('ciudad', e.target.value)} placeholder="Buenos Aires" />
                  <Input label="Provincia" value={form.provincia} onChange={e => set('provincia', e.target.value)} placeholder="CABA" />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <Input label="Piso" value={form.piso} onChange={e => set('piso', e.target.value)} placeholder="3" />
                  <Input label="Departamento" value={form.numeroDepartamento} onChange={e => set('numeroDepartamento', e.target.value)} placeholder="A" />
                </div>
              </div>
            </div>

            {/* Características */}
            <div>
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Características</p>
              <div className="grid grid-cols-3 gap-3 mb-3">
                <Input label="Ambientes" type="number" min="0" value={form.ambientes} onChange={e => set('ambientes', e.target.value)} />
                <Input label="Dormitorios" type="number" min="0" value={form.dormitorios} onChange={e => set('dormitorios', e.target.value)} />
                <Input label="Baños" type="number" min="0" value={form.banios} onChange={e => set('banios', e.target.value)} />
                <Input label="Sup. total (m²)" type="number" min="0" value={form.superficieTotal} onChange={e => set('superficieTotal', e.target.value)} />
                <Input label="Sup. cubierta (m²)" type="number" min="0" value={form.superficieCubierta} onChange={e => set('superficieCubierta', e.target.value)} />
                <Input label="Antigüedad (años)" type="number" min="0" value={form.antiguedad} onChange={e => set('antiguedad', e.target.value)} />
              </div>
              <div className="flex gap-6 flex-wrap">
                <Checkbox label="Cochera" checked={form.cochera} onChange={e => set('cochera', e.target.checked)} />
                <Checkbox label="Calefacción" checked={form.tieneCalefaccion} onChange={e => set('tieneCalefaccion', e.target.checked)} />
                <Checkbox label="Acepta mascotas" checked={form.aceptaMascotas} onChange={e => set('aceptaMascotas', e.target.checked)} />
              </div>
            </div>

            {/* Precio */}
            <div>
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Precio</p>
              <div className={`grid gap-3 ${mostrarAlquiler && mostrarVenta ? 'grid-cols-3' : 'grid-cols-2'}`}>
                {mostrarAlquiler && (
                  <CurrencyInput
                    label={`Alquiler (ARS)${!mostrarVenta ? ' *' : ''}`}
                    value={form.precioAlquiler}
                    onChange={val => set('precioAlquiler', val)}
                    required={!mostrarVenta}
                    placeholder="0"
                    prefix="$"
                  />
                )}
                {mostrarVenta && (
                  <CurrencyInput
                    label={`Venta (USD)${!mostrarAlquiler ? ' *' : ''}`}
                    value={form.precioVenta}
                    onChange={val => set('precioVenta', val)}
                    required={!mostrarAlquiler}
                    placeholder="0"
                    prefix="U$S"
                  />
                )}
                {mostrarAlquiler && (
                  <CurrencyInput
                    label="Expensas (ARS)"
                    value={form.expensas}
                    onChange={val => set('expensas', val)}
                    placeholder="0"
                    prefix="$"
                  />
                )}
              </div>
            </div>

            {/* Estado conservación y catastro */}
            <div className="grid grid-cols-2 gap-3">
              <Select label="Estado de conservación" value={form.estadoConservacion} onChange={e => set('estadoConservacion', e.target.value as PropiedadFormData['estadoConservacion'])}>
                <option value="">Sin especificar</option>
                {Object.entries(ESTADOS_CONSERVACION).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </Select>
              <Input label="Nro. catastro" value={form.nroCatastro} onChange={e => set('nroCatastro', e.target.value)} />
            </div>

            {/* Descripción y notas */}
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Descripción</label>
              <textarea
                value={form.descripcion}
                onChange={e => set('descripcion', e.target.value)}
                rows={3}
                placeholder="Descripción pública de la propiedad..."
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition resize-none"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Notas internas</label>
              <textarea
                value={form.notas}
                onChange={e => set('notas', e.target.value)}
                rows={2}
                placeholder="Notas solo visibles para el equipo..."
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition resize-none"
              />
            </div>

            {/* Fotos */}
            <FotosSection
              propiedadId={propiedad?.id ?? null}
              fotosIniciales={propiedad?.fotos ?? []}
              fotasPendientes={fotosPendientes}
              onFotosPendientesChange={setFotosPendientes}
            />

            {/* Video */}
            <VideoSection
              propiedadId={propiedad?.id ?? null}
              videoUrlInicial={propiedad?.videoUrl ?? null}
              videoPendiente={videoPendiente}
              onVideoPendienteChange={setVideoPendiente}
            />

          </div>

          {/* FOOTER */}
          <div className="px-6 py-4 border-t border-gray-100 flex gap-3 sticky bottom-0 bg-white">
            <button
              type="button"
              onClick={onCerrar}
              className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={guardando}
              className="flex-1 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors flex items-center justify-center gap-2"
            >
              {guardando
                ? <><Loader2 className="w-4 h-4 animate-spin" /> {fotosPendientes.length > 0 ? 'Guardando y subiendo fotos...' : 'Guardando...'}</>
                : (propiedad ? 'Guardar cambios' : 'Crear propiedad')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
