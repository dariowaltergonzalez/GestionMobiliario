import { useState, useEffect } from 'react'
import { X, Loader2 } from 'lucide-react'
import {
  createPropiedad, updatePropiedad,
  propiedadFormVacio, TIPOS_PROPIEDAD, TIPOS_OPERACION, ESTADOS_PROPIEDAD, ESTADOS_CONSERVACION,
  type PropiedadDto, type PropiedadFormData,
} from '../../../api/propiedades'
import { getPropietariosActivos, type PropietarioComboDto } from '../../../api/propietarios'

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

function formatARS(value: string) {
  const num = Number(value.replace(/\D/g, ''))
  if (isNaN(num) || value === '') return ''
  return num.toLocaleString('es-AR')
}

function formatUSD(value: string) {
  const num = Number(value.replace(/\D/g, ''))
  if (isNaN(num) || value === '') return ''
  return num.toLocaleString('es-AR')
}

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
    const raw = e.target.value.replace(/\D/g, '')
    onChange(raw)
  }

  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
      <div className="flex items-center border border-gray-200 rounded-lg focus-within:ring-2 focus-within:ring-blue-900/20 focus-within:border-blue-900 transition bg-white overflow-hidden">
        <span className="pl-3 pr-1 text-sm text-gray-400 select-none">{prefix}</span>
        <input
          type="text"
          inputMode="numeric"
          required={required}
          placeholder={placeholder}
          value={focused ? value : formatARS(value)}
          onChange={handleChange}
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
          className="flex-1 pr-3 py-2 text-sm text-gray-700 outline-none"
        />
      </div>
    </div>
  )
}

export default function PropiedadForm({ propiedad, onGuardado, onCerrar }: Props) {
  const [form, setForm] = useState<PropiedadFormData>(propiedadFormVacio)
  const [propietarios, setPropietarios] = useState<PropietarioComboDto[]>([])
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

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
      if (propiedad) {
        await updatePropiedad(propiedad.id, form)
      } else {
        await createPropiedad(form)
      }
      onGuardado()
    } catch (err: unknown) {
      const axErr = err as { response?: { status?: number; data?: { errors?: string[]; message?: string; title?: string } } }
      console.error('Error al guardar:', axErr.response?.status, axErr.response?.data)
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
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">
                {error}
              </div>
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

            {/* Precio — condicional según operación */}
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
              {guardando ? <><Loader2 className="w-4 h-4 animate-spin" /> Guardando...</> : (propiedad ? 'Guardar cambios' : 'Crear propiedad')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
