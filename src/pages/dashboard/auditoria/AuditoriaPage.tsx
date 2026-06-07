import { useState, useEffect, useCallback } from 'react'
import { ChevronLeft, ChevronRight, X } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getAuditLogs, getEntidades,
  ACCIONES, BADGE_ACCION, LABEL_ACCION,
  type AuditLogDto,
} from '../../../api/auditLogs'

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleString('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function formatJson(json: string | null) {
  if (!json) return null
  try { return JSON.stringify(JSON.parse(json), null, 2) }
  catch { return json }
}

function DetalleModal({ log, onCerrar }: { log: AuditLogDto; onCerrar: () => void }) {
  const oldVals = formatJson(log.oldValues)
  const newVals = formatJson(log.newValues)
  const changed = log.changedProperties
    ? log.changedProperties.replace(/[\[\]"]/g, '').split(',').map(s => s.trim()).filter(Boolean)
    : []

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl w-full max-w-2xl max-h-[85vh] flex flex-col shadow-2xl">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="font-bold text-gray-800">Detalle del cambio</h2>
          <button onClick={onCerrar} className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors">
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        <div className="overflow-y-auto flex-1 px-6 py-5 space-y-5">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Entidad</span>
              <p className="font-medium text-gray-800 mt-0.5">{log.entityName} <span className="text-gray-400">(ID: {log.entityId})</span></p>
            </div>
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Acción</span>
              <p className="mt-0.5">
                <span className={`inline-block px-2.5 py-1 rounded-full text-xs font-medium ${BADGE_ACCION[log.action] ?? 'bg-gray-100 text-gray-600'}`}>
                  {LABEL_ACCION[log.action] ?? log.action}
                </span>
              </p>
            </div>
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Usuario</span>
              <p className="font-medium text-gray-800 mt-0.5">{log.userName ?? '—'}</p>
            </div>
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Fecha</span>
              <p className="font-medium text-gray-800 mt-0.5">{formatFecha(log.timestamp)}</p>
            </div>
          </div>

          {changed.length > 0 && (
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Propiedades modificadas</span>
              <div className="flex flex-wrap gap-1.5 mt-2">
                {changed.map(p => (
                  <span key={p} className="text-xs bg-yellow-50 text-yellow-700 border border-yellow-200 px-2 py-0.5 rounded-lg">{p}</span>
                ))}
              </div>
            </div>
          )}

          {oldVals && (
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Valores anteriores</span>
              <pre className="mt-2 text-xs text-gray-600 bg-red-50 border border-red-100 rounded-xl p-4 overflow-x-auto font-mono whitespace-pre-wrap">{oldVals}</pre>
            </div>
          )}

          {newVals && (
            <div>
              <span className="text-xs text-gray-400 uppercase tracking-wide">Valores nuevos</span>
              <pre className="mt-2 text-xs text-gray-600 bg-green-50 border border-green-100 rounded-xl p-4 overflow-x-auto font-mono whitespace-pre-wrap">{newVals}</pre>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

export default function AuditoriaPage() {
  const [logs, setLogs] = useState<AuditLogDto[]>([])
  const [entidades, setEntidades] = useState<string[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [pagina, setPagina] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [filtroAccion, setFiltroAccion] = useState('')
  const [filtroEntidad, setFiltroEntidad] = useState('')

  const [detalle, setDetalle] = useState<AuditLogDto | null>(null)

  useEffect(() => {
    getEntidades().then(res => { if (res.success) setEntidades(res.data) }).catch(() => {})
  }, [])

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getAuditLogs(pagina, filtroAccion || undefined, filtroEntidad || undefined)
      if (res.success) {
        setLogs(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudo cargar el registro de auditoría.')
    } finally {
      setLoading(false)
    }
  }, [pagina, filtroAccion, filtroEntidad])

  useEffect(() => { cargar() }, [cargar])

  const handleFiltro = () => { setPagina(1); cargar() }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Registro de Auditoría">

      {/* FILTROS */}
      <div className="flex flex-wrap gap-3 mb-6">
        <select value={filtroAccion} onChange={e => setFiltroAccion(e.target.value)} className={selectClass}>
          <option value="">Todas las acciones</option>
          {ACCIONES.map(a => <option key={a} value={a}>{LABEL_ACCION[a]}</option>)}
        </select>
        <select value={filtroEntidad} onChange={e => setFiltroEntidad(e.target.value)} className={selectClass}>
          <option value="">Todas las entidades</option>
          {entidades.map(e => <option key={e} value={e}>{e}</option>)}
        </select>
        <button
          onClick={handleFiltro}
          className="bg-blue-900 text-white px-4 py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors"
        >
          Aplicar filtros
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>
      )}

      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Fecha</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Usuario</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Acción</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Entidad</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">ID</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Propiedades modificadas</th>
                <th className="text-center px-5 py-3.5 font-semibold text-gray-600">Detalle</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 7 }).map((_, j) => (
                      <td key={j} className="px-5 py-4"><div className="h-4 bg-gray-100 rounded animate-pulse" /></td>
                    ))}
                  </tr>
                ))
              ) : logs.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center py-16 text-gray-400">No hay registros de auditoría</td>
                </tr>
              ) : (
                logs.map(log => {
                  const changed = log.changedProperties
                    ? log.changedProperties.replace(/[\[\]"]/g, '').split(',').map(s => s.trim()).filter(Boolean)
                    : []
                  return (
                    <tr key={log.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                      <td className="px-5 py-3.5 text-xs text-gray-500 whitespace-nowrap font-mono">{formatFecha(log.timestamp)}</td>
                      <td className="px-5 py-3.5 text-gray-600 text-xs">{log.userName ?? '—'}</td>
                      <td className="px-5 py-3.5">
                        <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${BADGE_ACCION[log.action] ?? 'bg-gray-100 text-gray-600'}`}>
                          {LABEL_ACCION[log.action] ?? log.action}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-gray-700 font-medium">{log.entityName}</td>
                      <td className="px-5 py-3.5 text-gray-400 text-xs font-mono">{log.entityId}</td>
                      <td className="px-5 py-3.5 text-xs text-gray-500 max-w-[220px] truncate">
                        {changed.length > 0 ? changed.join(', ') : <span className="text-gray-300">—</span>}
                      </td>
                      <td className="px-5 py-3.5 text-center">
                        <button
                          onClick={() => setDetalle(log)}
                          className="text-blue-600 hover:text-blue-800 text-xs font-medium hover:underline"
                        >
                          Ver
                        </button>
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
              {totalRegistros} registro{totalRegistros !== 1 ? 's' : ''} · Página {pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button onClick={() => setPagina(p => p - 1)} disabled={pagina <= 1}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button onClick={() => setPagina(p => p + 1)} disabled={pagina >= totalPaginas}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {detalle && <DetalleModal log={detalle} onCerrar={() => setDetalle(null)} />}

    </DashboardLayout>
  )
}
