import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, Pencil, Trash2, ChevronLeft, ChevronRight, AlertTriangle, X, Save, Eye, EyeOff } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getAgentes, createAgente, updateAgente, deleteAgente,
  type AgenteDto, type FiltrosAgentes,
  type CreateAgenteRequest, type UpdateAgenteRequest,
} from '../../../api/agentes'

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

// ── Modal form ──────────────────────────────────────────────────────────────

interface AgenteFormProps {
  agente: AgenteDto | null
  onGuardado: () => void
  onCerrar: () => void
}

function AgenteForm({ agente, onGuardado, onCerrar }: AgenteFormProps) {
  const esNuevo = agente === null
  const [form, setForm] = useState({
    nombre: agente?.nombre ?? '',
    apellido: agente?.apellido ?? '',
    email: agente?.email ?? '',
    password: '',
    zona: agente?.zona ?? '',
    telefonoInterno: agente?.telefonoInterno ?? '',
    comisionPorcentaje: agente ? String(agente.comisionPorcentaje) : '0',
    notas: agente?.notas ?? '',
    activo: agente?.activo ?? true,
  })
  const [mostrarPassword, setMostrarPassword] = useState(false)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const set = (field: string, value: string | boolean) => setForm(f => ({ ...f, [field]: value }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.nombre.trim() || !form.apellido.trim()) {
      setError('Nombre y apellido son requeridos.')
      return
    }
    if (esNuevo && !form.password) {
      setError('La contraseña es requerida.')
      return
    }
    setGuardando(true)
    setError('')
    try {
      if (esNuevo) {
        const payload: CreateAgenteRequest = {
          nombre: form.nombre.trim(),
          apellido: form.apellido.trim(),
          email: form.email.trim(),
          password: form.password,
          zona: form.zona.trim() || undefined,
          telefonoInterno: form.telefonoInterno.trim() || undefined,
          comisionPorcentaje: Number(form.comisionPorcentaje),
          notas: form.notas.trim() || undefined,
        }
        await createAgente(payload)
      } else {
        const payload: UpdateAgenteRequest = {
          nombre: form.nombre.trim(),
          apellido: form.apellido.trim(),
          zona: form.zona.trim() || undefined,
          telefonoInterno: form.telefonoInterno.trim() || undefined,
          comisionPorcentaje: Number(form.comisionPorcentaje),
          notas: form.notas.trim() || undefined,
          activo: form.activo as boolean,
        }
        await updateAgente(agente!.id, payload)
      }
      onGuardado()
    } catch {
      setError('No se pudo guardar el agente.')
    } finally {
      setGuardando(false)
    }
  }

  const inputCls = 'w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">{esNuevo ? 'Nuevo agente' : 'Editar agente'}</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors cursor-pointer">
            <X className="w-4 h-4 text-gray-500" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Nombre *</label>
              <input required type="text" value={form.nombre} onChange={e => set('nombre', e.target.value)} className={inputCls} placeholder="Juan" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Apellido *</label>
              <input required type="text" value={form.apellido} onChange={e => set('apellido', e.target.value)} className={inputCls} placeholder="García" />
            </div>
          </div>

          {esNuevo && (
            <>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Email *</label>
                <input required type="email" value={form.email} onChange={e => set('email', e.target.value)} className={inputCls} placeholder="juan@inmobiliaria.com" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Contraseña *</label>
                <div className="relative">
                  <input
                    required
                    type={mostrarPassword ? 'text' : 'password'}
                    value={form.password}
                    onChange={e => set('password', e.target.value)}
                    className={`${inputCls} pr-10`}
                    placeholder="Mínimo 8 caracteres"
                  />
                  <button
                    type="button"
                    onClick={() => setMostrarPassword(v => !v)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 cursor-pointer"
                  >
                    {mostrarPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>
            </>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Zona</label>
              <input type="text" value={form.zona} onChange={e => set('zona', e.target.value)} className={inputCls} placeholder="Palermo, Belgrano..." />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Tel. interno</label>
              <input type="text" value={form.telefonoInterno} onChange={e => set('telefonoInterno', e.target.value)} className={inputCls} placeholder="Int. 205" />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Comisión (%)</label>
            <input
              type="number"
              min="0" max="100" step="0.5"
              value={form.comisionPorcentaje}
              onChange={e => set('comisionPorcentaje', e.target.value)}
              className={inputCls}
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Notas</label>
            <textarea
              rows={3}
              value={form.notas}
              onChange={e => set('notas', e.target.value)}
              className={`${inputCls} resize-none`}
              placeholder="Observaciones internas..."
            />
          </div>

          {!esNuevo && (
            <label className="flex items-center gap-3 cursor-pointer">
              <div
                onClick={() => set('activo', !form.activo)}
                className={`relative w-10 h-5 rounded-full transition-colors ${form.activo ? 'bg-green-500' : 'bg-gray-300'}`}
              >
                <div className={`absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform ${form.activo ? 'translate-x-5' : 'translate-x-0.5'}`} />
              </div>
              <span className="text-sm text-gray-600">{form.activo ? 'Activo' : 'Inactivo'}</span>
            </label>
          )}

          {error && (
            <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>
          )}

          <div className="flex gap-3 pt-1">
            <button type="button" onClick={onCerrar} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer">
              Cancelar
            </button>
            <button type="submit" disabled={guardando} className="flex-1 flex items-center justify-center gap-2 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors cursor-pointer">
              <Save className="w-4 h-4" />
              {guardando ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Page ────────────────────────────────────────────────────────────────────

export default function AgentesPage() {
  const [agentes, setAgentes] = useState<AgenteDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [filtros, setFiltros] = useState<FiltrosAgentes>({ buscar: '', activo: 'true', pagina: 1, tamano: 10 })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [agenteEditar, setAgenteEditar] = useState<AgenteDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<AgenteDto | null>(null)
  const [deletando, setDeletando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getAgentes(filtros)
      if (res.success) {
        setAgentes(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar los agentes.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput, pagina: 1 }))
  const handleNuevo = () => { setAgenteEditar(null); setModalAbierto(true) }
  const handleEditar = (a: AgenteDto) => { setAgenteEditar(a); setModalAbierto(true) }
  const handleGuardado = () => { setModalAbierto(false); cargar() }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setDeletando(true)
    try {
      await deleteAgente(confirmDelete.id)
      setConfirmDelete(null)
      cargar()
    } catch {
      setError('No se pudo dar de baja al agente.')
    } finally {
      setDeletando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Agentes">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Nombre, apellido, email..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2.5 text-sm outline-none w-full text-gray-700 placeholder-gray-400"
            />
          </div>
          <button
            onClick={handleBuscar}
            className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors cursor-pointer"
          >
            Buscar
          </button>
        </div>

        <div className="flex gap-2">
          <select
            value={filtros.activo}
            onChange={e => setFiltros(f => ({ ...f, activo: e.target.value, pagina: 1 }))}
            className={selectClass}
          >
            <option value="true">Activos</option>
            <option value="false">Inactivos</option>
            <option value="">Todos</option>
          </select>
          <button
            onClick={handleNuevo}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            Nuevo agente
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>
      )}

      {/* TABLA */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Agente</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Contacto</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Zona</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Comisión</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedades</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Alta</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 8 }).map((_, j) => (
                      <td key={j} className="px-5 py-4">
                        <div className="h-4 bg-gray-100 rounded animate-pulse" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : agentes.length === 0 ? (
                <tr>
                  <td colSpan={8} className="text-center py-16 text-gray-400">
                    No se encontraron agentes
                  </td>
                </tr>
              ) : (
                agentes.map(a => (
                  <tr key={a.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="font-medium text-gray-800">{a.apellido}, {a.nombre}</div>
                      <div className="text-xs text-gray-400 mt-0.5">{a.email}</div>
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {a.telefonoInterno ?? <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {a.zona ?? <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-5 py-4 text-sm text-gray-700 font-medium">
                      {a.comisionPorcentaje}%
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {a.cantidadPropiedades} prop. · {a.cantidadInquilinos} inq.
                    </td>
                    <td className="px-5 py-4">
                      <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${a.activo ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                        {a.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-400">
                      {formatFecha(a.fechaCreacion)}
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => handleEditar(a)}
                          className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                          title="Editar"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => setConfirmDelete(a)}
                          className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                          title="Dar de baja"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {!loading && totalRegistros > 0 && (
          <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
            <span className="text-sm text-gray-400">
              {totalRegistros} agente{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))}
                disabled={filtros.pagina <= 1}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))}
                disabled={filtros.pagina >= totalPaginas}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL FORM */}
      {modalAbierto && (
        <AgenteForm
          agente={agenteEditar}
          onGuardado={handleGuardado}
          onCerrar={() => setModalAbierto(false)}
        />
      )}

      {/* CONFIRM DELETE */}
      {confirmDelete && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Dar de baja agente</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Dás de baja a <strong>{confirmDelete.apellido}, {confirmDelete.nombre}</strong>? El usuario perderá acceso al sistema.
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setConfirmDelete(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirmarDelete}
                disabled={deletando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors cursor-pointer"
              >
                {deletando ? 'Procesando...' : 'Dar de baja'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
