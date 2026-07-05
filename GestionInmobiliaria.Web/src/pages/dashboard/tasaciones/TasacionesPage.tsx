import { useState, useEffect, useCallback } from 'react'
import { Search, X, Save, Trash2, AlertTriangle, ChevronLeft, ChevronRight, ImageOff, ZoomIn, CalendarPlus, FileDown } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getTasaciones, getTasacion, updateTasacion, deleteTasacion, deleteFoto,
  ESTADOS_TASACION, estadoNumero,
  type SolicitudTasacionDto, type FiltrosTasaciones,
} from '../../../api/tasaciones'
import { createEvento, TIPOS_EVENTO } from '../../../api/agenda'
import { exportarTasacionesPdf } from '../../../api/reportes'
import { useAuth } from '../../../context/AuthContext'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'

interface AgenteCombo { id: number; nombreCompleto: string }

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatMoneda(v: number | null) {
  if (v == null) return '—'
  return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(v)
}

function EstadoBadge({ estado }: { estado: string }) {
  const num = estadoNumero(estado) as keyof typeof ESTADOS_TASACION
  const info = ESTADOS_TASACION[num] ?? { label: estado, color: 'bg-gray-100 text-gray-500' }
  return (
    <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${info.color}`}>
      {info.label}
    </span>
  )
}

function mapTipoPropiedad(s: string) {
  const map: Record<string, string> = {
    Casa: 'Casa', Departamento: 'Depto.', Terreno: 'Terreno',
    Local: 'Local', Oficina: 'Oficina', Galpon: 'Galpón',
    PH: 'PH', Otro: 'Otro',
  }
  return map[s] ?? s
}

// ── Agendar Modal ───────────────────────────────────────────────────────────

interface AgendarModalProps {
  tasacion: SolicitudTasacionDto
  onCerrar: () => void
}

function AgendarModal({ tasacion, onCerrar }: AgendarModalProps) {
  const ahora = new Date()
  ahora.setMinutes(0, 0, 0)
  ahora.setHours(ahora.getHours() + 1)

  const toLocalInput = (d: Date) => {
    const pad = (n: number) => String(n).padStart(2, '0')
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
  }

  const [form, setForm] = useState({
    tipo: '1',
    fechaHora: toLocalInput(ahora),
    notas: `Tasación — ${tasacion.tipoPropiedad} en ${tasacion.direccion}${tasacion.barrio ? `, ${tasacion.barrio}` : ''}. Cliente: ${tasacion.nombre} ${tasacion.apellido} (${tasacion.telefono}).`,
  })
  const [agendando, setAgendando] = useState(false)
  const [error, setError] = useState('')
  const [exito, setExito] = useState(false)

  const set = (f: string, v: string) => setForm(p => ({ ...p, [f]: v }))

  const handleAgendar = async () => {
    if (!tasacion.agenteId) return
    setAgendando(true)
    setError('')
    try {
      await createEvento({
        tipo: Number(form.tipo),
        fechaHora: new Date(form.fechaHora).toISOString(),
        notas: form.notas.trim() || undefined,
        agenteId: tasacion.agenteId,
      })
      setExito(true)
    } catch {
      setError('No se pudo crear el evento.')
    } finally {
      setAgendando(false)
    }
  }

  const inputCls = 'w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h3 className="font-semibold text-gray-800">Agendar visita</h3>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors cursor-pointer">
            <X className="w-4 h-4 text-gray-500" />
          </button>
        </div>

        {exito ? (
          <div className="p-6 text-center space-y-3">
            <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto">
              <CalendarPlus className="w-6 h-6 text-green-600" />
            </div>
            <p className="font-semibold text-gray-800">Evento creado</p>
            <p className="text-sm text-gray-500">La visita fue agendada correctamente para {tasacion.nombreAgente}.</p>
            <button
              onClick={onCerrar}
              className="mt-2 bg-blue-900 text-white px-6 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 transition-colors cursor-pointer"
            >
              Cerrar
            </button>
          </div>
        ) : (
          <div className="p-6 space-y-4">
            <div className="bg-blue-50 rounded-xl px-4 py-3 text-sm">
              <span className="text-gray-500">Agente: </span>
              <span className="font-semibold text-gray-800">{tasacion.nombreAgente}</span>
            </div>

            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Tipo de evento</label>
              <select value={form.tipo} onChange={e => set('tipo', e.target.value)} className={inputCls}>
                {Object.entries(TIPOS_EVENTO).map(([k, v]) => (
                  <option key={k} value={k}>{v.icon} {v.label}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Fecha y hora</label>
              <input
                type="datetime-local"
                value={form.fechaHora}
                onChange={e => set('fechaHora', e.target.value)}
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
              />
            </div>

            {error && (
              <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>
            )}

            <div className="flex gap-3 pt-1">
              <button
                onClick={onCerrar}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                onClick={handleAgendar}
                disabled={agendando || !form.fechaHora}
                className="flex-1 flex items-center justify-center gap-2 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors cursor-pointer"
              >
                <CalendarPlus className="w-4 h-4" />
                {agendando ? 'Agendando...' : 'Agendar'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

// ── Detail Panel ────────────────────────────────────────────────────────────

interface DetailPanelProps {
  id: number
  agentes: AgenteCombo[]
  esAdmin: boolean
  onCerrar: () => void
  onActualizado: () => void
}

function DetailPanel({ id, agentes, esAdmin, onCerrar, onActualizado }: DetailPanelProps) {
  const [tasacion, setTasacion] = useState<SolicitudTasacionDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [deletando, setDeletando] = useState(false)
  const [fotoAmpliada, setFotoAmpliada] = useState<string | null>(null)
  const [agendarAbierto, setAgendarAbierto] = useState(false)

  const [form, setForm] = useState({
    estado: '1',
    agenteId: '',
    valorEstimado: '',
    notasInternas: '',
  })

  const cargarDetalle = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getTasacion(id)
      if (res.success) {
        const t = res.data
        setTasacion(t)
        setForm({
          estado: String(estadoNumero(t.estado)),
          agenteId: t.agenteId ? String(t.agenteId) : '',
          valorEstimado: t.valorEstimado != null ? String(t.valorEstimado) : '',
          notasInternas: t.notasInternas ?? '',
        })
      }
    } catch {
      setError('No se pudo cargar la solicitud.')
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => { cargarDetalle() }, [cargarDetalle])

  const set = (field: string, value: string) => setForm(f => ({ ...f, [field]: value }))

  const handleGuardar = async () => {
    setGuardando(true)
    setError('')
    try {
      await updateTasacion(id, {
        estado: Number(form.estado),
        agenteId: form.agenteId ? Number(form.agenteId) : null,
        valorEstimado: form.valorEstimado ? Number(form.valorEstimado) : null,
        notasInternas: form.notasInternas.trim() || null,
      })
      onActualizado()
      cargarDetalle()
    } catch {
      setError('No se pudo actualizar la solicitud.')
    } finally {
      setGuardando(false)
    }
  }

  const handleEliminarFoto = async (fotoId: number) => {
    try {
      await deleteFoto(id, fotoId)
      cargarDetalle()
    } catch {
      setError('No se pudo eliminar la foto.')
    }
  }

  const handleEliminar = async () => {
    setDeletando(true)
    try {
      await deleteTasacion(id)
      onActualizado()
      onCerrar()
    } catch {
      setError('No se pudo eliminar la solicitud.')
    } finally {
      setDeletando(false)
    }
  }

  const inputCls = 'w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-900/30'

  return (
    <>
      {/* Overlay */}
      <div className="fixed inset-0 bg-black/30 z-30" onClick={onCerrar} />

      {/* Panel */}
      <div className="fixed right-0 top-0 h-full w-full max-w-lg bg-white shadow-2xl z-40 flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100 shrink-0">
          <h2 className="font-semibold text-gray-800 text-base">Detalle de tasación</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors cursor-pointer">
            <X className="w-4 h-4 text-gray-500" />
          </button>
        </div>

        {loading ? (
          <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">Cargando...</div>
        ) : !tasacion ? (
          <div className="flex-1 flex items-center justify-center text-red-500 text-sm">Error al cargar</div>
        ) : (
          <div className="flex-1 overflow-y-auto">

            {/* Datos del solicitante */}
            <section className="px-5 py-4 border-b border-gray-100">
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">Solicitante</p>
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="font-semibold text-gray-800">{tasacion.apellido}, {tasacion.nombre}</p>
                  <p className="text-sm text-gray-500 mt-0.5">{tasacion.telefono}</p>
                  {tasacion.email && <p className="text-sm text-gray-500">{tasacion.email}</p>}
                </div>
                <div className="text-right shrink-0">
                  <EstadoBadge estado={tasacion.estado} />
                  <p className="text-xs text-gray-400 mt-1">{formatFecha(tasacion.fechaCreacion)}</p>
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-2">
                Preferencia contacto: <span className="font-medium text-gray-700">{tasacion.tipoContactoPreferido}</span>
              </p>
            </section>

            {/* Datos de la propiedad */}
            <section className="px-5 py-4 border-b border-gray-100">
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">Propiedad</p>
              <div className="grid grid-cols-2 gap-x-4 gap-y-1.5 text-sm">
                <Row label="Tipo" value={tasacion.tipoPropiedad} />
                <Row label="Dirección" value={tasacion.direccion} />
                {tasacion.barrio && <Row label="Barrio" value={tasacion.barrio} />}
                {tasacion.ciudad && <Row label="Ciudad" value={tasacion.ciudad} />}
                {tasacion.superficieTotal != null && <Row label="Sup. total" value={`${tasacion.superficieTotal} m²`} />}
                {tasacion.superficieCubierta != null && <Row label="Sup. cubierta" value={`${tasacion.superficieCubierta} m²`} />}
                {tasacion.ambientes != null && <Row label="Ambientes" value={String(tasacion.ambientes)} />}
                {tasacion.banios != null && <Row label="Baños" value={String(tasacion.banios)} />}
                {tasacion.antiguedad != null && <Row label="Antigüedad" value={`${tasacion.antiguedad} años`} />}
                <Row label="Estado conserv." value={tasacion.estadoConservacion} />
              </div>
              {tasacion.descripcion && (
                <p className="mt-3 text-xs text-gray-500 bg-gray-50 rounded-xl px-3 py-2 leading-relaxed">
                  {tasacion.descripcion}
                </p>
              )}
            </section>

            {/* Fotos */}
            {tasacion.fotos.length > 0 && (
              <section className="px-5 py-4 border-b border-gray-100">
                <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">
                  Fotos ({tasacion.fotos.length})
                </p>
                <div className="grid grid-cols-3 gap-2">
                  {tasacion.fotos.map(f => (
                    <div key={f.id} className="relative group rounded-xl overflow-hidden aspect-square bg-gray-100">
                      <img
                        src={f.url}
                        alt={f.nombreArchivo ?? 'foto'}
                        className="w-full h-full object-cover"
                        onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
                      />
                      <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-all flex items-center justify-center gap-1 opacity-0 group-hover:opacity-100">
                        <button
                          onClick={() => setFotoAmpliada(f.url)}
                          className="p-1.5 bg-white/90 rounded-lg cursor-pointer"
                          title="Ver"
                        >
                          <ZoomIn className="w-3.5 h-3.5 text-gray-700" />
                        </button>
                        {esAdmin && (
                          <button
                            onClick={() => handleEliminarFoto(f.id)}
                            className="p-1.5 bg-white/90 rounded-lg cursor-pointer"
                            title="Eliminar foto"
                          >
                            <Trash2 className="w-3.5 h-3.5 text-red-500" />
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            )}

            {tasacion.fotos.length === 0 && (
              <section className="px-5 py-3 border-b border-gray-100">
                <div className="flex items-center gap-2 text-gray-300 text-xs">
                  <ImageOff className="w-4 h-4" />
                  Sin fotos adjuntas
                </div>
              </section>
            )}

            {/* Gestión interna */}
            <section className="px-5 py-4 space-y-3">
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide">Gestión</p>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Estado</label>
                <select value={form.estado} onChange={e => set('estado', e.target.value)} className={inputCls}>
                  {Object.entries(ESTADOS_TASACION).map(([k, v]) => (
                    <option key={k} value={k}>{v.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Agente asignado</label>
                <select value={form.agenteId} onChange={e => set('agenteId', e.target.value)} className={inputCls}>
                  <option value="">Sin asignar</option>
                  {agentes.map(a => (
                    <option key={a.id} value={a.id}>{a.nombreCompleto}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Valor estimado ($)</label>
                <input
                  type="number"
                  min={0}
                  value={form.valorEstimado}
                  onChange={e => set('valorEstimado', e.target.value)}
                  className={inputCls}
                  placeholder="0"
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Notas internas</label>
                <textarea
                  rows={3}
                  value={form.notasInternas}
                  onChange={e => set('notasInternas', e.target.value)}
                  className={`${inputCls} resize-none`}
                  placeholder="Observaciones, seguimiento..."
                />
              </div>

              {error && (
                <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-xl">{error}</p>
              )}

              <button
                onClick={handleGuardar}
                disabled={guardando}
                className="w-full flex items-center justify-center gap-2 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-800 disabled:opacity-60 transition-colors cursor-pointer"
              >
                <Save className="w-4 h-4" />
                {guardando ? 'Guardando...' : 'Guardar cambios'}
              </button>

              <button
                onClick={() => setAgendarAbierto(true)}
                disabled={!tasacion.agenteId}
                title={!tasacion.agenteId ? 'Asigná un agente primero' : 'Crear evento en la agenda'}
                className="w-full flex items-center justify-center gap-2 border border-blue-200 text-blue-700 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <CalendarPlus className="w-4 h-4" />
                Agendar visita
              </button>

              {esAdmin && (
                <>
                  {!confirmDelete ? (
                    <button
                      onClick={() => setConfirmDelete(true)}
                      className="w-full border border-red-200 text-red-600 py-2.5 rounded-xl text-sm font-medium hover:bg-red-50 transition-colors cursor-pointer"
                    >
                      Eliminar solicitud
                    </button>
                  ) : (
                    <div className="bg-red-50 border border-red-200 rounded-xl p-4 space-y-3">
                      <div className="flex items-center gap-2">
                        <AlertTriangle className="w-4 h-4 text-red-500 shrink-0" />
                        <p className="text-sm text-red-700 font-medium">¿Confirmar eliminación?</p>
                      </div>
                      <div className="flex gap-2">
                        <button
                          onClick={() => setConfirmDelete(false)}
                          className="flex-1 border border-gray-200 text-gray-600 py-2 rounded-xl text-xs font-medium hover:bg-gray-50 transition-colors cursor-pointer"
                        >
                          Cancelar
                        </button>
                        <button
                          onClick={handleEliminar}
                          disabled={deletando}
                          className="flex-1 bg-red-600 text-white py-2 rounded-xl text-xs font-medium hover:bg-red-700 disabled:opacity-60 transition-colors cursor-pointer"
                        >
                          {deletando ? 'Eliminando...' : 'Eliminar'}
                        </button>
                      </div>
                    </div>
                  )}
                </>
              )}

              {/* Valor estimado actual */}
              {tasacion.valorEstimado != null && (
                <p className="text-xs text-gray-400 text-center">
                  Valor actual registrado: <span className="font-semibold text-gray-600">{formatMoneda(tasacion.valorEstimado)}</span>
                </p>
              )}
            </section>

          </div>
        )}
      </div>

      {/* Agendar modal */}
      {agendarAbierto && tasacion && (
        <AgendarModal
          tasacion={tasacion}
          onCerrar={() => setAgendarAbierto(false)}
        />
      )}

      {/* Foto ampliada */}
      {fotoAmpliada && (
        <div
          className="fixed inset-0 bg-black/80 z-50 flex items-center justify-center p-4 cursor-zoom-out"
          onClick={() => setFotoAmpliada(null)}
        >
          <img
            src={fotoAmpliada}
            alt="Foto ampliada"
            className="max-w-full max-h-full object-contain rounded-xl shadow-2xl"
          />
        </div>
      )}
    </>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-xs text-gray-400">{label}: </span>
      <span className="text-xs font-medium text-gray-700">{value}</span>
    </div>
  )
}

// ── Page ────────────────────────────────────────────────────────────────────

export default function TasacionesPage() {
  const { user } = useAuth()
  const esAdmin = user?.rol === 'Admin'

  const [tasaciones, setTasaciones] = useState<SolicitudTasacionDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [agentes, setAgentes] = useState<AgenteCombo[]>([])

  const [filtros, setFiltros] = useState<FiltrosTasaciones>({ buscar: '', estado: '', pagina: 1, tamano: 10 })
  const [buscarInput, setBuscarInput] = useState('')

  const [panelId, setPanelId] = useState<number | null>(null)
  const [exportando, setExportando] = useState(false)

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getTasaciones(filtros)
      if (res.success) {
        setTasaciones(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar las solicitudes.')
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

  const handleExportarPdf = async () => {
    setExportando(true)
    try {
      await exportarTasacionesPdf({ buscar: filtros.buscar, estado: filtros.estado })
    } catch {
      setError('No se pudo generar el PDF.')
    } finally {
      setExportando(false)
    }
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Tasaciones">

      {/* TOOLBAR */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="flex gap-2 flex-1">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white flex-1 max-w-xs">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Nombre, dirección, email..."
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

        <select
          value={filtros.estado}
          onChange={e => setFiltros(f => ({ ...f, estado: e.target.value, pagina: 1 }))}
          className={selectClass}
        >
          <option value="">Todos los estados</option>
          {Object.entries(ESTADOS_TASACION).map(([k, v]) => (
            <option key={k} value={k}>{v.label}</option>
          ))}
        </select>

        <button
          onClick={handleExportarPdf}
          disabled={exportando}
          className="flex items-center gap-2 border border-gray-200 text-gray-600 px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 disabled:opacity-60 transition-colors cursor-pointer"
        >
          <FileDown className="w-4 h-4" />
          {exportando ? 'Generando...' : 'Exportar PDF'}
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
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Solicitante</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Dirección</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Estado</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Agente</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Valor estimado</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Fecha solicitud</th>
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
              ) : tasaciones.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center py-16 text-gray-400">
                    No se encontraron solicitudes de tasación
                  </td>
                </tr>
              ) : (
                tasaciones.map(t => (
                  <tr
                    key={t.id}
                    onClick={() => setPanelId(t.id)}
                    className="border-b border-gray-50 hover:bg-blue-50/40 transition-colors cursor-pointer"
                  >
                    <td className="px-5 py-4">
                      <div className="font-medium text-gray-800">{t.apellido}, {t.nombre}</div>
                      <div className="text-xs text-gray-400 mt-0.5">{t.telefono}</div>
                    </td>
                    <td className="px-5 py-4 text-gray-600 text-xs font-medium">
                      {mapTipoPropiedad(t.tipoPropiedad)}
                    </td>
                    <td className="px-5 py-4 text-gray-500 text-xs max-w-[180px]">
                      <div className="truncate">{t.direccion}</div>
                      {t.barrio && <div className="text-gray-400">{t.barrio}</div>}
                    </td>
                    <td className="px-5 py-4">
                      <EstadoBadge estado={t.estado} />
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-500">
                      {t.nombreAgente ?? <span className="text-gray-300">Sin asignar</span>}
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-600 font-medium">
                      {formatMoneda(t.valorEstimado)}
                    </td>
                    <td className="px-5 py-4 text-xs text-gray-400">
                      {formatFecha(t.fechaCreacion)}
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
              {totalRegistros} solicitud{totalRegistros !== 1 ? 'es' : ''} · Página {filtros.pagina} de {totalPaginas}
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

      {/* DETAIL PANEL */}
      {panelId !== null && (
        <DetailPanel
          id={panelId}
          agentes={agentes}
          esAdmin={esAdmin}
          onCerrar={() => setPanelId(null)}
          onActualizado={cargar}
        />
      )}

    </DashboardLayout>
  )
}
