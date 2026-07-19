import { useState, useEffect } from 'react'
import { X, Loader2 } from 'lucide-react'
import {
  createPropietario, updatePropietario,
  propietarioFormVacio,
  type PropietarioDto, type PropietarioFormData,
} from '../../../api/propietarios'

interface Props {
  propietario: PropietarioDto | null
  onGuardado: () => void
  onCerrar: () => void
}

const Input = ({ label, required, ...props }: { label: string; required?: boolean } & React.InputHTMLAttributes<HTMLInputElement>) => (
  <div>
    <label className="block text-xs font-medium text-gray-600 mb-1">{label}{required && ' *'}</label>
    <input
      {...props}
      required={required}
      className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition disabled:bg-gray-50"
    />
  </div>
)

function formatCuit(value: string) {
  const digits = value.replace(/\D/g, '').slice(0, 11)
  if (digits.length <= 2) return digits
  if (digits.length <= 10) return `${digits.slice(0, 2)}-${digits.slice(2)}`
  return `${digits.slice(0, 2)}-${digits.slice(2, 10)}-${digits.slice(10)}`
}

const CuitInput = ({ value, onChange }: { value: string; onChange: (val: string) => void }) => {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(formatCuit(e.target.value))
  }
  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">CUIT</label>
      <input
        type="text"
        inputMode="numeric"
        value={value}
        onChange={handleChange}
        placeholder="20-12345678-3"
        className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition"
      />
    </div>
  )
}

export default function PropietarioForm({ propietario, onGuardado, onCerrar }: Props) {
  const [form, setForm] = useState<PropietarioFormData>(propietarioFormVacio)
  const [activo, setActivo] = useState(true)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (propietario) {
      setForm({
        nombre: propietario.nombre,
        apellido: propietario.apellido,
        dni: propietario.dni ?? '',
        cuit: propietario.cuit ?? '',
        email: propietario.email ?? '',
        telefono: propietario.telefono ?? '',
        telefono2: propietario.telefono2 ?? '',
        direccion: propietario.direccion ?? '',
        banco: propietario.banco ?? '',
        cbu: propietario.cbu ?? '',
        notas: propietario.notas ?? '',
      })
      setActivo(propietario.activo)
    } else {
      setForm(propietarioFormVacio)
      setActivo(true)
    }
  }, [propietario])

  const set = (campo: keyof PropietarioFormData, valor: string) =>
    setForm(f => ({ ...f, [campo]: valor }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setGuardando(true)
    try {
      if (propietario) {
        await updatePropietario(propietario.id, form, activo)
      } else {
        await createPropietario(form)
      }
      onGuardado()
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string; title?: string } } }
      const msg = axErr.response?.data?.errors?.[0]
        ?? axErr.response?.data?.message
        ?? axErr.response?.data?.title
        ?? 'Error al guardar el propietario.'
      setError(msg)
    } finally {
      setGuardando(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl w-full max-w-lg max-h-[90vh] flex flex-col shadow-2xl">

        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-bold text-gray-800 text-lg">
            {propietario ? 'Editar propietario' : 'Nuevo propietario'}
          </h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto">
          <div className="px-6 py-5 space-y-4">

            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">{error}</div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <Input label="Apellido" required value={form.apellido} onChange={e => set('apellido', e.target.value)} placeholder="González" />
              <Input label="Nombre" required value={form.nombre} onChange={e => set('nombre', e.target.value)} placeholder="Juan" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <Input label="DNI" value={form.dni} onChange={e => set('dni', e.target.value)} placeholder="20123456" />
              <CuitInput value={form.cuit} onChange={val => set('cuit', val)} />
            </div>

            <Input label="Email" type="email" value={form.email} onChange={e => set('email', e.target.value)} placeholder="juan@ejemplo.com" />

            <div className="grid grid-cols-2 gap-3">
              <Input label="Teléfono" value={form.telefono} onChange={e => set('telefono', e.target.value)} placeholder="2664123456" />
              <Input label="Teléfono 2" value={form.telefono2} onChange={e => set('telefono2', e.target.value)} placeholder="2664654321" />
            </div>

            <Input label="Dirección" value={form.direccion} onChange={e => set('direccion', e.target.value)} placeholder="Av. San Martín 456" />
            <div className="grid grid-cols-2 gap-3">
              <Input label="Banco" value={form.banco} onChange={e => set('banco', e.target.value)} placeholder="Banco Nación" />
              <Input label="CBU" value={form.cbu} onChange={e => set('cbu', e.target.value)} placeholder="0000000000000000000000" />
            </div>

            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Notas</label>
              <textarea
                value={form.notas}
                onChange={e => set('notas', e.target.value)}
                rows={3}
                placeholder="Observaciones internas..."
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition resize-none"
              />
            </div>

            {propietario && (
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={activo}
                  onChange={e => setActivo(e.target.checked)}
                  className="w-4 h-4 rounded accent-blue-900"
                />
                <span className="text-sm text-gray-600">Activo</span>
              </label>
            )}

          </div>

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
                ? <><Loader2 className="w-4 h-4 animate-spin" /> Guardando...</>
                : propietario ? 'Guardar cambios' : 'Crear propietario'}
            </button>
          </div>
        </form>

      </div>
    </div>
  )
}
