import { useState, useEffect, useCallback } from 'react'
import { Plus, Search, Pencil, Trash2, ChevronLeft, ChevronRight, AlertTriangle, X, Save } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getLeads, createLead, updateLead, deleteLead,
  ESTADOS_LEAD, ORIGENES_LEAD,
  type LeadDto, type FiltrosLeads, type CreateLeadRequest, type UpdateLeadRequest,
} from '../../../api/leads'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'

interface AgenteCombo { id: number; nombreCompleto: string }

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function estadoNumero(estadoStr: string): number {
  const map: Record<string, number> = {
    Nuevo: 1, Contactado: 2, Interesado: 3, Convertido: 4, Descartado: 5,
  }
  return map[estadoStr] ?? 1
}

function origenNumero(origenStr: string): number {
  const map: Record<string, number> = {
    Web: 1, WhatsApp: 2, Referido: 3, RedesSociales: 4, Llamada: 5, Otro: 6,
  }
  return map[origenStr] ?? 1
}

function EstadoBadge({ estado }: { estado: string }) {
  const num = estadoNumero(estado) as keyof typeof ESTADOS_LEAD
  const info = ESTADOS_LEAD[num]
  return (
    <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${info.color}`}>
      {info.label}
    </span>
  )
}

function OrigenBadge({ origen }: { origen: string }) {
  const num = origenNumero(origen) as keyof typeof ORIGENES_LEAD
  const info = ORIGENES_LEAD[num]
  return (
    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${info.color}`}>
      {info.label}
    </span>
  )
}

// ── Modal form ──────────────────────────────────────────────────────────────

interface LeadFormProps {
  lead: LeadDto | null
  agentes: AgenteCombo[]
  onGuardado: () => void
  onCerrar: () => void
}

function LeadForm({ lead, agentes, onGuardado, onCerrar }: LeadFormProps) {
  const [form, setForm] = useState({
    nombre: lead?.nombre ?? '',
    apellido: lead?.apellido ?? '',
    email: lead?.email ?? '',
    telefono: lead?.telefono ?? '',
    origen: lead ? String(origenNumero(lead.origen)) : '1',
    estado: lead ? String(estadoNumero(lead.estado)) : '1',
    notas: lead?.notas ?? '',
    agenteId: lead?.agenteId ? String(lead.agenteId) : '',
  })
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const set = (field: string, value: string) => setForm(f => ({ ...f, [field]: value }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.nombre.trim() || !form.apellido.trim()) {
      setError('Nombre y apellido son requeridos.')
      return
    }
    setGuardando(true)
    setError('')
    try {
      const payload = {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        email: form.email.trim() || undefined,
        telefono: form.telefono.trim() || undefined,
        origen: Number(form.origen),
        notas: form.notas.trim() || undefined,
        agenteId: form.agenteId ? Number(form.agenteId) : null,
        propiedadId: lead?.propiedadId ?? null,
      }
      if (lead) {
        await updateLead(lead.id, { ...payload, estado: Number(form.estado) } as UpdateLeadRequest)
      } else {
        await createLead(payload as CreateLeadRequest)
      }
      onGuardado()
    } catch {
      setError('No se pudo guardar el lead.')
    } finally {
      setGuardando(false)
    }
  }

  const inputCls = 'w-full border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'
  const selectCls = inputCls

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">{lead ? 'Editar lead' : 'Nuevo lead'}</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
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

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Teléfono</label>
              <input type="tel" value={form.telefono} onChange={e => set('telefono', e.target.value)} className={inputCls} placeholder="+54 11 1234-5678" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Email</label>
              <input type="email" value={form.email} onChange={e => set('email', e.target.value)} className={inputCls} placeholder="juan@gmail.com" />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Origen</label>
              <select value={form.origen} onChange={e => set('origen', e.target.value)} className={selectCls}>
                {Object.entries(ORIGENES_LEAD).map(([k, v]) => (
                  <option key={k} value={k}>{v.label}</option>
                ))}
              </select>
            </div>
            {lead && (
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Estado</label>
                <select value={form.estado} onChange={e => set('estado', e.target.value)} className={selectCls}>
                  {Object.entries(ESTADOS_LEAD).map(([k, v]) => (
                    <option key={k} value={k}>{v.label}</option>
                  ))}
                </select>
              </div>
            )}
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Agente asignado</label>
            <select value={form.agenteId} onChange={e => set('agenteId', e.target.value)} className={selectCls}>
              <option value="">Sin asignar</option>
              {agentes.map(a => (
                <option key={a.id} value={a.id}>{a.nombreCompleto}</option>
              ))}
            </select>
          </div>

          {lead?.propiedadDireccion && (
            <div className="bg-gray-50 rounded-xl px-3 py-2 text-xs text-gray-500">
              Propiedad vinculada: <span className="font-medium text-gray-700">{lead.propiedadDireccion}</span>
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Notas</label>
            <textarea
              rows={3}
              value={form.notas}
              onChange={e => set('notas', e.target.value)}
              className={`${inputCls} resize-none`}
              placeholder="Observaciones, seguimiento, etc."
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

export default function LeadsPage() {
  const [leads, setLeads] = useState<LeadDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [agentes, setAgentes] = useState<AgenteCombo[]>([])

  const [filtros, setFiltros] = useState<FiltrosLeads>({ buscar: '', estado: '', pagina: 1, tamano: 10 })
  const [buscarInput, setBuscarInput] = useState('')

  const [modalAbierto, setModalAbierto] = useState(false)
  const [leadEditar, setLeadEditar] = useState<LeadDto | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<LeadDto | null>(null)
  const [deletando, setDeletando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getLeads(filtros)
      if (res.success) {
        setLeads(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar los leads.')
    } finally {
      setLoading(false)
    }
  }, [filtros])

  useEffect(() => { cargar() }, [cargar])

  useEffect(() => {
    client.get<ApiResponse<AgenteCombo[]>>('/api/agentes/activos')
      .then(res => { if (res.data.success) setAgentes(res.data.data) })
      .catch(() => {})
  }, [])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput, pagina: 1 }))
  const handleNuevo = () => { setLeadEditar(null); setModalAbierto(true) }
  const handleEditar = (l: LeadDto) => { setLeadEditar(l); setModalAbierto(true) }
  const handleGuardado = () => { setModalAbierto(false); cargar() }

  const handleConfirmarDelete = async () => {
    if (!confirmDelete) return
    setDeletando(true)
    try {
      await deleteLead(confirmDelete.id)
      setConfirmDelete(null)
      cargar()
    } catch {
      setError('No se pudo eliminar el lead.')
    } finally {
      setDeletando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Leads">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Nombre, email, teléfono..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2.5 text-sm outline-none w-full text-gray-700 placeholder-gray-400"
            />
          </div>
          <button
            onClick={handleBuscar}
            className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors"
          >
            Buscar
          </button>
        </div>

        <div className="flex gap-2">
          <select
            value={filtros.estado}
            onChange={e => setFiltros(f => ({ ...f, estado: e.target.value, pagina: 1 }))}
            className={selectClass}
          >
            <option value="">Todos los estados</option>
            {Object.entries(ESTADOS_LEAD).map(([k, v]) => (
              <option key={k} value={k}>{v.label}</option>
            ))}
          </select>
          <button
            onClick={handleNuevo}
            className="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-semibold px-4 py-2.5 rounded-xl text-sm transition-colors"
          >
            <Plus className="w-4 h-4" />
            Nuevo lead
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
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Lead</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Contacto</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Agente</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Fecha</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 7 }).map((_, j) => (
                      <td key={j} className="px-5 py-4">
                        <div className="h-4 bg-gray-100 rounded animate-pulse" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : leads.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center py-16 text-gray-400">
                    No se encontraron leads
                  </td>
                </tr>
              ) : (
                leads.map(l => (
                  <tr key={l.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="font-medium text-gray-800">{l.apellido}, {l.nombre}</div>
                      <div className="mt-1">
                        <OrigenBadge origen={l.origen} />
                      </div>
                    </td>
                    <td className="px-5 py-4 text-gray-500 text-xs">
                      {l.email && <div>{l.email}</div>}
                      {l.telefono && <div>{l.telefono}</div>}
                    </td>
                    <td className="px-5 py-4">
                      <EstadoBadge estado={l.estado} />
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {l.propiedadDireccion ?? <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {l.agenteNombre ?? <span className="text-gray-300">Sin asignar</span>}
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-400">
                      {formatFecha(l.fechaCreacion)}
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => handleEditar(l)}
                          className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                          title="Editar"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => setConfirmDelete(l)}
                          className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                          title="Eliminar"
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
              {totalRegistros} lead{totalRegistros !== 1 ? 's' : ''} · Página {filtros.pagina} de {totalPaginas}
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

      {/* MODAL FORM */}
      {modalAbierto && (
        <LeadForm
          lead={leadEditar}
          agentes={agentes}
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
              <h3 className="font-semibold text-gray-800">Eliminar lead</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Eliminás a <strong>{confirmDelete.apellido}, {confirmDelete.nombre}</strong>? Esta acción no se puede deshacer.
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setConfirmDelete(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirmarDelete}
                disabled={deletando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors"
              >
                {deletando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}

    </DashboardLayout>
  )
}
