import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, X, Save, Trash2, Pencil, CheckCircle2, AlertTriangle, ChevronLeft, ChevronRight } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getGastos, createGasto, updateGasto, deleteGasto, marcarGastoResuelto,
  gastoFormVacio, CATEGORIAS_GASTO, RESPONSABLES_GASTO,
  type GastoDto, type GastoFormData, type FiltrosGastos,
} from '../../../api/gastos'
import { MEDIOS_PAGO, REFERENCIA_PLACEHOLDER } from '../../../api/contratos'
import client from '../../../api/client'
import type { ApiResponse, PagedResult } from '../../../types/api'

interface PropiedadCombo { id: number; direccion: string }
interface ContratoCombo { id: number; codigo: string; locatarioNombre: string; locatarioApellido: string }

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatMoneda(v: number) {
  return `$ ${v.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

const ESTADO_BADGE: Record<string, string> = {
  Pendiente: 'bg-yellow-100 text-yellow-700',
  Resuelto: 'bg-green-100 text-green-700',
}

const RESPONSABLE_BADGE: Record<string, string> = {
  Propietario: 'bg-blue-50 text-blue-700',
  Inquilino: 'bg-purple-50 text-purple-700',
}

// ── Formulario ──────────────────────────────────────────────────────────────

interface GastoFormProps {
  gasto: GastoDto | null
  propiedades: PropiedadCombo[]
  onGuardado: () => void
  onCerrar: () => void
}

function GastoForm({ gasto, propiedades, onGuardado, onCerrar }: GastoFormProps) {
  const [form, setForm] = useState<GastoFormData>(gasto ? {
    propiedadId: gasto.propiedadId,
    contratoId: gasto.contratoId ?? '',
    categoria: Number(Object.entries(CATEGORIAS_GASTO).find(([, v]) => v === gasto.categoria)?.[0] ?? 1),
    descripcion: gasto.descripcion ?? '',
    monto: gasto.monto,
    fecha: gasto.fecha.slice(0, 10),
    responsable: Number(Object.entries(RESPONSABLES_GASTO).find(([, v]) => v === gasto.responsable)?.[0] ?? 1),
    visibleParaInquilino: gasto.visibleParaInquilino,
  } : gastoFormVacio())

  const [contratos, setContratos] = useState<ContratoCombo[]>([])
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const set = <K extends keyof GastoFormData>(campo: K, valor: GastoFormData[K]) =>
    setForm(f => ({ ...f, [campo]: valor }))

  useEffect(() => {
    if (!form.propiedadId) { setContratos([]); return }
    client.get<ApiResponse<PagedResult<ContratoCombo>>>('/api/contratos', {
      params: { propiedadId: form.propiedadId, estado: 2, pagina: 1, tamano: 50 },
    }).then(res => { if (res.data.success) setContratos(res.data.data.items) }).catch(() => {})
  }, [form.propiedadId])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.propiedadId) { setError('Seleccioná una propiedad.'); return }
    if (!form.monto || form.monto <= 0) { setError('El monto debe ser mayor a cero.'); return }

    setGuardando(true)
    setError('')
    try {
      const res = gasto ? await updateGasto(gasto.id, form) : await createGasto(form)
      if (res.success) {
        onGuardado()
      } else {
        setError(res.errors?.[0] ?? res.message ?? 'No se pudo guardar el gasto.')
      }
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string } } }
      setError(axErr.response?.data?.errors?.[0] ?? axErr.response?.data?.message ?? 'No se pudo guardar el gasto.')
    } finally {
      setGuardando(false)
    }
  }

  const inp = 'w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[92vh] flex flex-col">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 shrink-0">
          <h2 className="font-semibold text-gray-800">{gasto ? 'Editar gasto' : 'Nuevo gasto'}</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors cursor-pointer">
            <X className="w-4 h-4 text-gray-500" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Propiedad *</label>
            <select
              value={form.propiedadId}
              onChange={e => { set('propiedadId', e.target.value ? Number(e.target.value) : ''); set('contratoId', '') }}
              className={inp}
            >
              <option value="">Seleccionar propiedad...</option>
              {propiedades.map(p => <option key={p.id} value={p.id}>{p.direccion}</option>)}
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Contrato (opcional)</label>
            <select
              value={form.contratoId}
              onChange={e => set('contratoId', e.target.value ? Number(e.target.value) : '')}
              className={inp}
              disabled={!form.propiedadId}
            >
              <option value="">Sin asociar a un contrato específico</option>
              {contratos.map(c => (
                <option key={c.id} value={c.id}>{c.codigo} — {c.locatarioApellido}, {c.locatarioNombre}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Categoría</label>
              <select value={form.categoria} onChange={e => set('categoria', Number(e.target.value))} className={inp}>
                {Object.entries(CATEGORIAS_GASTO).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">A cargo de</label>
              <select value={form.responsable} onChange={e => set('responsable', Number(e.target.value))} className={inp}>
                {Object.entries(RESPONSABLES_GASTO).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Monto *</label>
              <input type="number" min={0} step={0.01} value={form.monto || ''} onChange={e => set('monto', Number(e.target.value))} className={inp} placeholder="0.00" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Fecha</label>
              <input type="date" value={form.fecha} onChange={e => set('fecha', e.target.value)} className={inp} />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Descripción</label>
            <textarea rows={2} value={form.descripcion} onChange={e => set('descripcion', e.target.value)}
              className={`${inp} resize-none`} placeholder="Detalle del gasto..." />
          </div>

          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input type="checkbox" checked={form.visibleParaInquilino} onChange={e => set('visibleParaInquilino', e.target.checked)} />
            Visible para el inquilino en el autoservicio (a futuro)
          </label>

          {error && <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>}

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

// ── Modal de resolver (registrar cómo se cobró) ──────────────────────────────

function ResolverGastoModal({ gasto, onResuelto, onCerrar }: {
  gasto: GastoDto
  onResuelto: () => void
  onCerrar: () => void
}) {
  const [fecha, setFecha] = useState(new Date().toISOString().slice(0, 10))
  const [medio, setMedio] = useState(1)
  const [referenciaCobro, setReferenciaCobro] = useState('')
  const [chequeBanco, setChequeBanco] = useState('')
  const [chequeNumero, setChequeNumero] = useState('')
  const [chequeFechaVencimiento, setChequeFechaVencimiento] = useState('')
  const [observaciones, setObservaciones] = useState('')
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const esCheque = medio === 4
  const refPlaceholder = !esCheque ? REFERENCIA_PLACEHOLDER[medio as 1 | 2 | 3 | 4] : ''

  const handleMedioChange = (v: number) => {
    setMedio(v)
    setReferenciaCobro('')
    setChequeBanco('')
    setChequeNumero('')
    setChequeFechaVencimiento('')
  }

  const handleConfirmar = async () => {
    setGuardando(true)
    setError('')
    try {
      const res = await marcarGastoResuelto(gasto.id, {
        medio,
        fecha: new Date(fecha).toISOString(),
        referenciaCobro: referenciaCobro || undefined,
        chequeBanco: chequeBanco || undefined,
        chequeNumero: chequeNumero || undefined,
        chequeFechaVencimiento: chequeFechaVencimiento ? new Date(chequeFechaVencimiento).toISOString() : undefined,
        observaciones: observaciones || undefined,
      })
      if (res.success) onResuelto()
      else setError(res.errors?.[0] ?? res.message ?? 'No se pudo marcar el gasto como resuelto.')
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string } } }
      setError(axErr.response?.data?.errors?.[0] ?? axErr.response?.data?.message ?? 'No se pudo marcar el gasto como resuelto.')
    } finally {
      setGuardando(false)
    }
  }

  const inp = 'w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl p-6 max-w-md w-full shadow-xl">
        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 bg-green-100 rounded-xl flex items-center justify-center">
            <CheckCircle2 className="w-5 h-5 text-green-600" />
          </div>
          <div>
            <h3 className="font-semibold text-gray-800">Marcar como resuelto</h3>
            <p className="text-xs text-gray-400">{formatMoneda(gasto.monto)} — {gasto.propiedadDireccion}</p>
          </div>
        </div>
        <p className="text-sm text-gray-600 mb-4">Registrá cómo te devolvió el inquilino esta plata.</p>

        <div className="space-y-3 mb-5">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Fecha de cobro</label>
              <input type="date" value={fecha} onChange={e => setFecha(e.target.value)} className={inp} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Medio de cobro</label>
              <select value={medio} onChange={e => handleMedioChange(Number(e.target.value))} className={`${inp} bg-white`}>
                {Object.entries(MEDIOS_PAGO).map(([valor, label]) => (
                  <option key={valor} value={valor}>{label}</option>
                ))}
              </select>
            </div>
          </div>

          {!esCheque && refPlaceholder && (
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Referencia</label>
              <input type="text" value={referenciaCobro} onChange={e => setReferenciaCobro(e.target.value)}
                placeholder={refPlaceholder} className={inp} />
            </div>
          )}

          {esCheque && (
            <div className="grid grid-cols-3 gap-2">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Banco</label>
                <input type="text" value={chequeBanco} onChange={e => setChequeBanco(e.target.value)}
                  placeholder="Ej: Nación" className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">N° cheque</label>
                <input type="text" value={chequeNumero} onChange={e => setChequeNumero(e.target.value)}
                  placeholder="12345678" className={inp} />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Fecha vto.</label>
                <input type="date" value={chequeFechaVencimiento} onChange={e => setChequeFechaVencimiento(e.target.value)} className={inp} />
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Observaciones</label>
            <textarea value={observaciones} onChange={e => setObservaciones(e.target.value)}
              rows={2} placeholder="Aclaraciones opcionales..." className={`${inp} resize-none`} />
          </div>
        </div>

        {error && <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl mb-4">{error}</p>}

        <div className="flex gap-3">
          <button onClick={onCerrar} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer">
            Cancelar
          </button>
          <button onClick={handleConfirmar} disabled={guardando}
            className="flex-1 bg-green-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-green-700 disabled:opacity-60 transition-colors cursor-pointer">
            {guardando ? 'Guardando...' : 'Confirmar'}
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Page ────────────────────────────────────────────────────────────────────

export default function GastosPage() {
  const [gastos, setGastos] = useState<GastoDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [propiedades, setPropiedades] = useState<PropiedadCombo[]>([])

  const [filtros, setFiltros] = useState<FiltrosGastos>({ pagina: 1, tamano: 10 })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [gastoEditar, setGastoEditar] = useState<GastoDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<GastoDto | null>(null)
  const [resolverGasto, setResolverGasto] = useState<GastoDto | null>(null)
  const [procesando, setProcesando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getGastos(filtros)
      if (res.success) {
        setGastos(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar los gastos.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  useEffect(() => {
    client.get<ApiResponse<PropiedadCombo[]>>('/api/propiedades/para-contrato')
      .then(res => { if (res.data.success) setPropiedades(res.data.data) })
      .catch(() => {})
  }, [])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput || undefined, pagina: 1 }))

  const handleGuardado = () => { setModalAbierto(false); setGastoEditar(null); cargar() }

  const handleResuelto = () => { setResolverGasto(null); cargar() }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setProcesando(true)
    try {
      const res = await deleteGasto(confirmDelete.id)
      if (res.success) { setConfirmDelete(null); cargar() }
      else setError(res.errors?.[0] ?? res.message ?? 'No se pudo eliminar el gasto.')
    } catch {
      setError('No se pudo eliminar el gasto.')
    } finally {
      setProcesando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Gastos">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Propiedad, descripción..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2.5 text-sm outline-none w-full text-gray-700 placeholder-gray-400"
            />
          </div>
          <button onClick={handleBuscar} className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors cursor-pointer">
            Buscar
          </button>
        </div>

        <div className="flex gap-2 flex-wrap">
          <select value={filtros.estado ?? ''} onChange={e => setFiltros(f => ({ ...f, pagina: 1, estado: e.target.value ? Number(e.target.value) : undefined }))} className={selectClass}>
            <option value="">Todos los estados</option>
            <option value="1">Pendiente</option>
            <option value="2">Resuelto</option>
          </select>
          <select value={filtros.responsable ?? ''} onChange={e => setFiltros(f => ({ ...f, pagina: 1, responsable: e.target.value ? Number(e.target.value) : undefined }))} className={selectClass}>
            <option value="">Propietario e Inquilino</option>
            {Object.entries(RESPONSABLES_GASTO).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
          <select value={filtros.categoria ?? ''} onChange={e => setFiltros(f => ({ ...f, pagina: 1, categoria: e.target.value ? Number(e.target.value) : undefined }))} className={selectClass}>
            <option value="">Todas las categorías</option>
            {Object.entries(CATEGORIAS_GASTO).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
          <button
            onClick={() => { setGastoEditar(null); setModalAbierto(true) }}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            Nuevo gasto
          </button>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      {/* TABLA */}
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Categoría</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Monto</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Fecha</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">A cargo de</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 4 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 7 }).map((_, j) => (
                      <td key={j} className="px-5 py-4"><div className="h-4 bg-gray-100 rounded animate-pulse" /></td>
                    ))}
                  </tr>
                ))
              ) : gastos.length === 0 ? (
                <tr><td colSpan={7} className="text-center py-16 text-gray-400">No se encontraron gastos</td></tr>
              ) : (
                gastos.map(g => (
                  <tr key={g.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="font-medium text-gray-800">{g.propiedadDireccion}</div>
                      {g.contratoCodigo && <div className="text-xs text-gray-400 font-mono">{g.contratoCodigo}</div>}
                    </td>
                    <td className="px-5 py-4 text-gray-600">
                      {g.categoria}
                      {g.descripcion && <div className="text-xs text-gray-400 truncate max-w-[220px]">{g.descripcion}</div>}
                    </td>
                    <td className="px-5 py-4 font-semibold text-gray-800">{formatMoneda(g.monto)}</td>
                    <td className="px-5 py-4 text-xs text-gray-500">{formatFecha(g.fecha)}</td>
                    <td className="px-5 py-4">
                      <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${RESPONSABLE_BADGE[g.responsable] ?? ''}`}>{g.responsable}</span>
                    </td>
                    <td className="px-5 py-4">
                      <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${ESTADO_BADGE[g.estado] ?? ''}`}>{g.estado}</span>
                      {g.estado === 'Resuelto' && g.medioCobro && (
                        <div className="text-xs text-gray-400 mt-0.5">{g.medioCobro}</div>
                      )}
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center justify-center gap-2">
                        {g.estado === 'Pendiente' && g.responsable === 'Inquilino' && (
                          <button onClick={() => setResolverGasto(g)} disabled={procesando}
                            className="p-1.5 text-green-600 hover:bg-green-50 rounded-lg transition-colors cursor-pointer disabled:opacity-50" title="Registrar cobro y marcar como resuelto">
                            <CheckCircle2 className="w-4 h-4" />
                          </button>
                        )}
                        {g.estado === 'Pendiente' && (
                          <>
                            <button onClick={() => { setGastoEditar(g); setModalAbierto(true) }} className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer" title="Editar">
                              <Pencil className="w-4 h-4" />
                            </button>
                            <button onClick={() => setConfirmDelete(g)} className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer" title="Eliminar">
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </>
                        )}
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
              {totalRegistros} gasto{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))} disabled={filtros.pagina <= 1} className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer">
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))} disabled={filtros.pagina >= totalPaginas} className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer">
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL FORM */}
      {modalAbierto && (
        <GastoForm
          gasto={gastoEditar}
          propiedades={propiedades}
          onGuardado={handleGuardado}
          onCerrar={() => { setModalAbierto(false); setGastoEditar(null) }}
        />
      )}

      {/* RESOLVER GASTO */}
      {resolverGasto && (
        <ResolverGastoModal
          gasto={resolverGasto}
          onResuelto={handleResuelto}
          onCerrar={() => setResolverGasto(null)}
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
              <h3 className="font-semibold text-gray-800">Eliminar gasto</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Eliminás el gasto de <strong>{formatMoneda(confirmDelete.monto)}</strong> en {confirmDelete.propiedadDireccion}?
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmDelete(null)} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer">
                Cancelar
              </button>
              <button onClick={handleConfirmarDelete} disabled={procesando} className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors cursor-pointer">
                {procesando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
