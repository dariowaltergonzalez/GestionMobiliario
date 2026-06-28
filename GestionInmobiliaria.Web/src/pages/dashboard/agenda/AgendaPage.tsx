import { useState, useEffect, useCallback } from 'react'
import { Plus, Pencil, Trash2, ChevronLeft, ChevronRight, AlertTriangle, X, Save, CheckCircle, FileDown } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getEventos, createEvento, updateEvento, deleteEvento,
  TIPOS_EVENTO, ESTADOS_EVENTO,
  type EventoAgendaDto, type FiltrosAgenda,
  type CreateEventoAgendaRequest, type UpdateEventoAgendaRequest,
} from '../../../api/agenda'
import { exportarAgendaPdf } from '../../../api/reportes'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'
import { useAuth } from '../../../context/AuthContext'

interface AgenteCombo { id: number; nombreCompleto: string }
interface LeadCombo { id: number; nombreCompleto: string }

function tipoNumero(s: string): number {
  return ({ Visita: 1, Llamada: 2, Reunion: 3, Otro: 4 } as Record<string, number>)[s] ?? 4
}
function estadoNumero(s: string): number {
  return ({ Pendiente: 1, Realizada: 2, Cancelada: 3 } as Record<string, number>)[s] ?? 1
}

function formatFechaHora(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleString('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function toLocalDatetimeInput(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  const d = new Date(utc)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function TipoBadge({ tipo }: { tipo: string }) {
  const num = tipoNumero(tipo) as keyof typeof TIPOS_EVENTO
  const info = TIPOS_EVENTO[num]
  return (
    <span className={`inline-flex items-center gap-1 text-xs font-semibold px-2.5 py-1 rounded-full ${info.color}`}>
      {info.icon} {info.label}
    </span>
  )
}

function EstadoBadge({ estado }: { estado: string }) {
  const num = estadoNumero(estado) as keyof typeof ESTADOS_EVENTO
  const info = ESTADOS_EVENTO[num]
  return (
    <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${info.color}`}>
      {info.label}
    </span>
  )
}

// ── Modal form ──────────────────────────────────────────────────────────────

interface EventoFormProps {
  evento: EventoAgendaDto | null
  agentes: AgenteCombo[]
  leads: LeadCombo[]
  esAdmin: boolean
  agenteIdPropio: number | null
  onGuardado: () => void
  onCerrar: () => void
}

function EventoForm({ evento, agentes, leads, esAdmin, agenteIdPropio, onGuardado, onCerrar }: EventoFormProps) {
  const ahora = toLocalDatetimeInput(new Date().toISOString())

  const agenteDefault = evento
    ? String(evento.agenteId)
    : agenteIdPropio ? String(agenteIdPropio) : ''

  const [form, setForm] = useState({
    tipo: evento ? String(tipoNumero(evento.tipo)) : '1',
    estado: evento ? String(estadoNumero(evento.estado)) : '1',
    fechaHora: evento ? toLocalDatetimeInput(evento.fechaHora) : ahora,
    agenteId: agenteDefault,
    leadId: evento?.leadId ? String(evento.leadId) : '',
    notas: evento?.notas ?? '',
  })
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const set = (field: string, value: string) => setForm(f => ({ ...f, [field]: value }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.agenteId) { setError('Seleccioná un agente.'); return }
    if (!form.fechaHora) { setError('La fecha y hora son requeridas.'); return }

    setGuardando(true)
    setError('')
    try {
      const payload: CreateEventoAgendaRequest = {
        tipo: Number(form.tipo),
        fechaHora: new Date(form.fechaHora).toISOString(),
        agenteId: Number(form.agenteId),
        leadId: form.leadId ? Number(form.leadId) : null,
        notas: form.notas.trim() || undefined,
      }
      if (evento) {
        await updateEvento(evento.id, { ...payload, estado: Number(form.estado) } as UpdateEventoAgendaRequest)
      } else {
        await createEvento(payload)
      }
      onGuardado()
    } catch {
      setError('No se pudo guardar el evento.')
    } finally {
      setGuardando(false)
    }
  }

  const inputCls = 'w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">{evento ? 'Editar evento' : 'Nuevo evento'}</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
            <X className="w-4 h-4 text-gray-500" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Tipo *</label>
              <select value={form.tipo} onChange={e => set('tipo', e.target.value)} className={inputCls}>
                {Object.entries(TIPOS_EVENTO).map(([k, v]) => (
                  <option key={k} value={k}>{v.icon} {v.label}</option>
                ))}
              </select>
            </div>
            {evento && (
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Estado</label>
                <select value={form.estado} onChange={e => set('estado', e.target.value)} className={inputCls}>
                  {Object.entries(ESTADOS_EVENTO).map(([k, v]) => (
                    <option key={k} value={k}>{v.label}</option>
                  ))}
                </select>
              </div>
            )}
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Fecha y hora *</label>
            <input
              type="datetime-local"
              value={form.fechaHora}
              onChange={e => set('fechaHora', e.target.value)}
              className={inputCls}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Agente *</label>
            {esAdmin ? (
              <select value={form.agenteId} onChange={e => set('agenteId', e.target.value)} className={inputCls} required>
                <option value="">Seleccioná un agente...</option>
                {agentes.map(a => (
                  <option key={a.id} value={a.id}>{a.nombreCompleto}</option>
                ))}
              </select>
            ) : (
              <div className={`${inputCls} bg-gray-50 text-gray-500 cursor-not-allowed`}>
                {agentes.find(a => a.id === agenteIdPropio)?.nombreCompleto ?? '—'}
              </div>
            )}
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Lead vinculado</label>
            <select value={form.leadId} onChange={e => set('leadId', e.target.value)} className={inputCls}>
              <option value="">Sin vincular</option>
              {leads.map(l => (
                <option key={l.id} value={l.id}>{l.nombreCompleto}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Notas</label>
            <textarea
              rows={3}
              value={form.notas}
              onChange={e => set('notas', e.target.value)}
              className={`${inputCls} resize-none`}
              placeholder="Detalles del evento, dirección, observaciones..."
            />
          </div>

          {error && (
            <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>
          )}

          <div className="flex gap-3 pt-1">
            <button type="button" onClick={onCerrar} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
              Cancelar
            </button>
            <button type="submit" disabled={guardando} className="flex-1 flex items-center justify-center gap-2 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors">
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

export default function AgendaPage() {
  const { user } = useAuth()
  const esAdmin = user?.rol === 'Admin'

  const [eventos, setEventos] = useState<EventoAgendaDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [agentes, setAgentes] = useState<AgenteCombo[]>([])
  const [leads, setLeads] = useState<LeadCombo[]>([])

  const [filtros, setFiltros] = useState<FiltrosAgenda>({
    tipo: '',
    estado: '1',
    pagina: 1,
    tamano: 15,
    agenteId: !esAdmin && user?.agenteId ? String(user.agenteId) : '',
  })

  const [exportando, setExportando] = useState(false)

  const handleExportarPdf = async () => {
    setExportando(true)
    try {
      await exportarAgendaPdf({
        estado: filtros.estado,
        tipo: filtros.tipo,
        agenteId: filtros.agenteId,
      })
    } catch {
      setError('No se pudo generar el PDF.')
    } finally {
      setExportando(false)
    }
  }

  const [modalAbierto, setModalAbierto] = useState(false)
  const [eventoEditar, setEventoEditar] = useState<EventoAgendaDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<EventoAgendaDto | null>(null)
  const [deletando, setDeletando] = useState(false)
  const [marcando, setMarcando] = useState<number | null>(null)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getEventos(filtros)
      if (res.success) {
        setEventos(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar los eventos.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  useEffect(() => {
    client.get<ApiResponse<AgenteCombo[]>>('/api/agentes/activos')
      .then(res => { if (res.data.success) setAgentes(res.data.data) })
      .catch(() => {})
    client.get<ApiResponse<LeadCombo[]>>('/api/leads/activos')
      .then(res => { if (res.data.success) setLeads(res.data.data) })
      .catch(() => {})
  }, [])

  const handleNuevo = () => { setEventoEditar(null); setModalAbierto(true) }
  const handleEditar = (e: EventoAgendaDto) => { setEventoEditar(e); setModalAbierto(true) }
  const handleGuardado = () => { setModalAbierto(false); cargar() }

  const handleMarcarRealizada = async (ev: EventoAgendaDto) => {
    setMarcando(ev.id)
    try {
      await updateEvento(ev.id, {
        tipo: tipoNumero(ev.tipo),
        estado: 2,
        fechaHora: ev.fechaHora,
        agenteId: ev.agenteId,
        leadId: ev.leadId,
        notas: ev.notas ?? undefined,
      })
      cargar()
    } catch {
      setError('No se pudo actualizar el evento.')
    } finally {
      setMarcando(null)
    }
  }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setDeletando(true)
    try {
      await deleteEvento(confirmDelete.id)
      setConfirmDelete(null)
      cargar()
    } catch {
      setError('No se pudo eliminar el evento.')
    } finally {
      setDeletando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Agenda">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1 flex-wrap">
          <select
            value={filtros.estado}
            onChange={e => setFiltros(f => ({ ...f, estado: e.target.value, pagina: 1 }))}
            className={selectClass}
          >
            <option value="">Todos los estados</option>
            {Object.entries(ESTADOS_EVENTO).map(([k, v]) => (
              <option key={k} value={k}>{v.label}</option>
            ))}
          </select>
          <select
            value={filtros.tipo}
            onChange={e => setFiltros(f => ({ ...f, tipo: e.target.value, pagina: 1 }))}
            className={selectClass}
          >
            <option value="">Todos los tipos</option>
            {Object.entries(TIPOS_EVENTO).map(([k, v]) => (
              <option key={k} value={k}>{v.icon} {v.label}</option>
            ))}
          </select>
          {esAdmin && (
            <select
              value={filtros.agenteId ?? ''}
              onChange={e => setFiltros(f => ({ ...f, agenteId: e.target.value, pagina: 1 }))}
              className={selectClass}
            >
              <option value="">Todos los agentes</option>
              {agentes.map(a => (
                <option key={a.id} value={a.id}>{a.nombreCompleto}</option>
              ))}
            </select>
          )}
        </div>
        <button
          onClick={handleExportarPdf}
          disabled={exportando}
          className="flex items-center gap-2 border border-gray-200 text-gray-600 bg-white hover:bg-gray-50 px-4 py-2.5 rounded-xl text-sm font-medium transition-colors whitespace-nowrap disabled:opacity-60 cursor-pointer"
        >
          <FileDown className="w-4 h-4" />
          {exportando ? 'Generando...' : 'Exportar PDF'}
        </button>
        <button
          onClick={handleNuevo}
          className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors whitespace-nowrap cursor-pointer"
        >
          <Plus className="w-4 h-4" />
          Nuevo evento
        </button>
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
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Tipo</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Fecha y hora</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Contacto</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Agente</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Notas</th>
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
              ) : eventos.length === 0 ? (
                <tr>
                  <td colSpan={8} className="text-center py-16 text-gray-400">
                    No hay eventos para mostrar
                  </td>
                </tr>
              ) : (
                eventos.map(ev => {
                  const esPendiente = estadoNumero(ev.estado) === 1
                  return (
                    <tr key={ev.id} className={`border-b border-gray-50 hover:bg-gray-50 transition-colors ${!esPendiente ? 'opacity-60' : ''}`}>
                      <td className="px-5 py-4">
                        <TipoBadge tipo={ev.tipo} />
                      </td>
                      <td className="px-5 py-4 text-sm font-medium text-gray-700 whitespace-nowrap">
                        {formatFechaHora(ev.fechaHora)}
                      </td>
                      <td className="px-5 py-4 text-xs text-gray-600">
                        {ev.leadNombre && <div className="font-medium">{ev.leadNombre}</div>}
                        {ev.inquilinoNombre && <div className="font-medium">{ev.inquilinoNombre}</div>}
                        {!ev.leadNombre && !ev.inquilinoNombre && <span className="text-gray-300">—</span>}
                      </td>
                      <td className="px-5 py-4 text-xs text-gray-500">
                        {ev.propiedadDireccion ?? <span className="text-gray-300">—</span>}
                      </td>
                      <td className="px-5 py-4 text-xs text-gray-500">
                        {ev.agenteNombre}
                      </td>
                      <td className="px-5 py-4">
                        <EstadoBadge estado={ev.estado} />
                      </td>
                      <td className="px-5 py-4 text-xs text-gray-400 max-w-[180px] truncate">
                        {ev.notas ?? '—'}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center justify-center gap-1">
                          {esPendiente && (
                            <button
                              onClick={() => handleMarcarRealizada(ev)}
                              disabled={marcando === ev.id}
                              className="p-1.5 text-green-600 hover:bg-green-50 rounded-lg transition-colors disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
                              title="Marcar como realizada"
                            >
                              <CheckCircle className="w-4 h-4" />
                            </button>
                          )}
                          <button
                            onClick={() => handleEditar(ev)}
                            className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                            title="Editar"
                          >
                            <Pencil className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => setConfirmDelete(ev)}
                            className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                            title="Eliminar"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>

        {!loading && totalRegistros > 0 && (
          <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
            <span className="text-sm text-gray-400">
              {totalRegistros} evento{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))}
                disabled={filtros.pagina <= 1}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))}
                disabled={filtros.pagina >= totalPaginas}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL */}
      {modalAbierto && (
        <EventoForm
          evento={eventoEditar}
          agentes={agentes}
          leads={leads}
          esAdmin={esAdmin}
          agenteIdPropio={user?.agenteId ?? null}
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
              <h3 className="font-semibold text-gray-800">Eliminar evento</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Eliminás el evento de <strong>{confirmDelete.tipo}</strong> del <strong>{formatFechaHora(confirmDelete.fechaHora)}</strong>?
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmDelete(null)} className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                Cancelar
              </button>
              <button onClick={handleConfirmarDelete} disabled={deletando} className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors">
                {deletando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
