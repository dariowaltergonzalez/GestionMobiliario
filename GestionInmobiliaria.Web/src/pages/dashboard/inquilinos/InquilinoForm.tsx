import { useState, useEffect } from 'react'
import { X, Loader2, Plus, Trash2 } from 'lucide-react'
import {
  createInquilino, updateInquilino, getTemasNotificacionInquilino,
  inquilinoFormVacio,
  type InquilinoDto, type InquilinoFormData, type TemaNotificacionDto,
} from '../../../api/inquilinos'

interface Props {
  inquilino: InquilinoDto | null
  /** Solo se usa cuando inquilino es null: precarga el alta (ej: al convertir un Lead). */
  datosIniciales?: Partial<InquilinoFormData>
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

export default function InquilinoForm({ inquilino, datosIniciales, onGuardado, onCerrar }: Props) {
  const [form, setForm] = useState<InquilinoFormData>(inquilinoFormVacio)
  const [activo, setActivo] = useState(true)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')
  const [temas, setTemas] = useState<TemaNotificacionDto[]>([])
  const [temaAAgregar, setTemaAAgregar] = useState('')
  const [temaWhatsAppAAgregar, setTemaWhatsAppAAgregar] = useState('')

  useEffect(() => {
    getTemasNotificacionInquilino().then(res => { if (res.success) setTemas(res.data) }).catch(() => {})
  }, [])

  useEffect(() => {
    if (inquilino) {
      setForm({
        nombre: inquilino.nombre,
        apellido: inquilino.apellido,
        dni: inquilino.dni ?? '',
        cuit: inquilino.cuit ?? '',
        email: inquilino.email ?? '',
        telefono: inquilino.telefono ?? '',
        telefono2: inquilino.telefono2 ?? '',
        direccion: inquilino.direccion ?? '',
        ocupacion: inquilino.ocupacion ?? '',
        nombreGarante: inquilino.nombreGarante ?? '',
        telefonoGarante: inquilino.telefonoGarante ?? '',
        dniGarante: inquilino.dniGarante ?? '',
        notas: inquilino.notas ?? '',
        notificaciones: { ...inquilino.notificaciones },
        notificacionesWhatsApp: { ...inquilino.notificacionesWhatsApp },
      })
      setActivo(inquilino.activo)
    } else {
      setForm({ ...inquilinoFormVacio, ...datosIniciales })
      setActivo(true)
    }
  }, [inquilino])

  const set = (campo: keyof InquilinoFormData, valor: string) =>
    setForm(f => ({ ...f, [campo]: valor }))

  type CampoNotificaciones = 'notificaciones' | 'notificacionesWhatsApp'

  const temasDisponibles = temas.filter(t => !(t.codigo in form.notificaciones))
  const temasWhatsAppDisponibles = temas.filter(t => !(t.codigo in form.notificacionesWhatsApp))

  const agregarTema = (campo: CampoNotificaciones, codigo: string, limpiar: () => void) => {
    if (!codigo) return
    setForm(f => ({ ...f, [campo]: { ...f[campo], [codigo]: true } }))
    limpiar()
  }

  const toggleTema = (campo: CampoNotificaciones, codigo: string) =>
    setForm(f => ({ ...f, [campo]: { ...f[campo], [codigo]: !f[campo][codigo] } }))

  const quitarTema = (campo: CampoNotificaciones, codigo: string) =>
    setForm(f => {
      const resto = { ...f[campo] }
      delete resto[codigo]
      return { ...f, [campo]: resto }
    })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setGuardando(true)
    try {
      if (inquilino) {
        await updateInquilino(inquilino.id, form, activo)
      } else {
        await createInquilino(form)
      }
      onGuardado()
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string; title?: string } } }
      const msg = axErr.response?.data?.errors?.[0]
        ?? axErr.response?.data?.message
        ?? axErr.response?.data?.title
        ?? 'Error al guardar el inquilino.'
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
            {inquilino ? 'Editar inquilino' : 'Nuevo inquilino'}
          </h2>
          <div className="flex items-center gap-2">
            {inquilino && (
              <button
                type="button"
                onClick={() => setActivo(a => !a)}
                title="Estado del inquilino en el sistema"
                className={`text-xs px-3 py-1.5 rounded-full font-semibold transition-colors ${
                  activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'
                }`}
              >
                Inquilino {activo ? 'activo' : 'inactivo'}
              </button>
            )}
            <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
              <X className="w-5 h-5 text-gray-500" />
            </button>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto">
          <div className="px-6 py-5 space-y-4">

            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl text-sm">{error}</div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <Input label="Apellido" required value={form.apellido} onChange={e => set('apellido', e.target.value)} placeholder="Domínguez" />
              <Input label="Nombre" required value={form.nombre} onChange={e => set('nombre', e.target.value)} placeholder="Gustavo" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <Input label="DNI" value={form.dni} onChange={e => set('dni', e.target.value)} placeholder="20123456" />
              <CuitInput value={form.cuit} onChange={val => set('cuit', val)} />
            </div>

            <Input label="Email" type="email" value={form.email} onChange={e => set('email', e.target.value)} placeholder="gustavo@ejemplo.com" />

            <div className="grid grid-cols-2 gap-3">
              <Input label="Teléfono" value={form.telefono} onChange={e => set('telefono', e.target.value)} placeholder="2664123456" />
              <Input label="Teléfono 2" value={form.telefono2} onChange={e => set('telefono2', e.target.value)} placeholder="2664654321" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <Input label="Dirección" value={form.direccion} onChange={e => set('direccion', e.target.value)} placeholder="Av. San Martín 456" />
              <Input label="Ocupación" value={form.ocupacion} onChange={e => set('ocupacion', e.target.value)} placeholder="Empleado, comerciante..." />
            </div>

            <div>
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-2">Garante (opcional)</p>
              <div className="grid grid-cols-2 gap-3">
                <Input label="Nombre" value={form.nombreGarante} onChange={e => set('nombreGarante', e.target.value)} />
                <Input label="Teléfono" value={form.telefonoGarante} onChange={e => set('telefonoGarante', e.target.value)} />
                <Input label="DNI" value={form.dniGarante} onChange={e => set('dniGarante', e.target.value)} />
              </div>
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

            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Notificaciones automáticas por email</label>
              <p className="text-xs text-gray-400 mb-2">
                Solo se envía lo que se agregue acá. Si no hay nada configurado, este inquilino no recibe ningún email automático.
              </p>

              {Object.keys(form.notificaciones).length > 0 && (
                <div className="border border-gray-200 rounded-lg divide-y divide-gray-100 mb-2">
                  {Object.entries(form.notificaciones).map(([codigo, habilitado]) => (
                    <div key={codigo} className="flex items-center justify-between gap-2 px-3 py-2">
                      <span className="text-sm text-gray-700">
                        {temas.find(t => t.codigo === codigo)?.label ?? codigo}
                      </span>
                      <div className="flex items-center gap-1 shrink-0">
                        <button
                          type="button"
                          onClick={() => toggleTema('notificaciones', codigo)}
                          className={`text-xs px-2.5 py-1 rounded-full font-medium transition-colors ${
                            habilitado ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                          }`}
                        >
                          {habilitado ? 'Sí' : 'No'}
                        </button>
                        <button type="button" onClick={() => quitarTema('notificaciones', codigo)}
                          className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {temasDisponibles.length > 0 && (
                <div className="flex gap-2">
                  <select
                    value={temaAAgregar}
                    onChange={e => setTemaAAgregar(e.target.value)}
                    className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition bg-white"
                  >
                    <option value="">Seleccionar tema...</option>
                    {temasDisponibles.map(t => (
                      <option key={t.codigo} value={t.codigo}>{t.label}</option>
                    ))}
                  </select>
                  <button
                    type="button"
                    onClick={() => agregarTema('notificaciones', temaAAgregar, () => setTemaAAgregar(''))}
                    disabled={!temaAAgregar}
                    className="flex items-center gap-1 px-3 py-2 rounded-lg text-sm font-medium bg-gray-100 text-gray-700 hover:bg-gray-200 disabled:opacity-50 transition-colors"
                  >
                    <Plus className="w-4 h-4" /> Agregar
                  </button>
                </div>
              )}
            </div>

            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Notificaciones automáticas por WhatsApp</label>
              <p className="text-xs text-gray-400 mb-2">
                Requiere que el inquilino tenga un teléfono cargado arriba. Solo se envía lo que se agregue acá.
              </p>

              {Object.keys(form.notificacionesWhatsApp).length > 0 && (
                <div className="border border-gray-200 rounded-lg divide-y divide-gray-100 mb-2">
                  {Object.entries(form.notificacionesWhatsApp).map(([codigo, habilitado]) => (
                    <div key={codigo} className="flex items-center justify-between gap-2 px-3 py-2">
                      <span className="text-sm text-gray-700">
                        {temas.find(t => t.codigo === codigo)?.label ?? codigo}
                      </span>
                      <div className="flex items-center gap-1 shrink-0">
                        <button
                          type="button"
                          onClick={() => toggleTema('notificacionesWhatsApp', codigo)}
                          className={`text-xs px-2.5 py-1 rounded-full font-medium transition-colors ${
                            habilitado ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                          }`}
                        >
                          {habilitado ? 'Sí' : 'No'}
                        </button>
                        <button type="button" onClick={() => quitarTema('notificacionesWhatsApp', codigo)}
                          className="p-1 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {temasWhatsAppDisponibles.length > 0 && (
                <div className="flex gap-2">
                  <select
                    value={temaWhatsAppAAgregar}
                    onChange={e => setTemaWhatsAppAAgregar(e.target.value)}
                    className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition bg-white"
                  >
                    <option value="">Seleccionar tema...</option>
                    {temasWhatsAppDisponibles.map(t => (
                      <option key={t.codigo} value={t.codigo}>{t.label}</option>
                    ))}
                  </select>
                  <button
                    type="button"
                    onClick={() => agregarTema('notificacionesWhatsApp', temaWhatsAppAAgregar, () => setTemaWhatsAppAAgregar(''))}
                    disabled={!temaWhatsAppAAgregar}
                    className="flex items-center gap-1 px-3 py-2 rounded-lg text-sm font-medium bg-gray-100 text-gray-700 hover:bg-gray-200 disabled:opacity-50 transition-colors"
                  >
                    <Plus className="w-4 h-4" /> Agregar
                  </button>
                </div>
              )}
            </div>

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
                : inquilino ? 'Guardar cambios' : 'Crear inquilino'}
            </button>
          </div>
        </form>

      </div>
    </div>
  )
}
