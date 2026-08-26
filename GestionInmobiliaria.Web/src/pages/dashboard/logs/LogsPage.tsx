import { useState, useEffect, useCallback } from 'react'
import { ChevronLeft, ChevronRight, ChevronDown, ChevronUp, AlertCircle, Info, AlertTriangle } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import { getLogs, BADGE_NIVEL, type AppLogDto, type NivelLog } from '../../../api/logs'

const NIVEL_ICON: Record<NivelLog, React.ReactNode> = {
  1: <Info className="w-3.5 h-3.5" />,
  2: <AlertTriangle className="w-3.5 h-3.5" />,
  3: <AlertCircle className="w-3.5 h-3.5" />,
}

function formatFecha(iso: string) {
  const d = new Date(iso.endsWith('Z') ? iso : iso + 'Z')
  return d.toLocaleString('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function LogRow({ log }: { log: AppLogDto }) {
  const [expandido, setExpandido] = useState(false)
  const tieneDetalle = !!log.detalle

  return (
    <>
      <tr
        className={`border-b border-gray-50 transition-colors ${tieneDetalle ? 'cursor-pointer hover:bg-gray-50' : ''}`}
        onClick={() => tieneDetalle && setExpandido(e => !e)}
      >
        <td className="px-5 py-3.5 text-xs text-gray-500 whitespace-nowrap font-mono">
          {formatFecha(log.timestamp)}
        </td>
        <td className="px-5 py-3.5">
          <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium ${BADGE_NIVEL[log.nivel] ?? 'bg-gray-100 text-gray-600'}`}>
            {NIVEL_ICON[log.nivel]}
            {log.nivelNombre}
          </span>
        </td>
        <td className="px-5 py-3.5 text-xs text-gray-500 font-mono max-w-[180px] truncate">
          {log.origen}
        </td>
        <td className="px-5 py-3.5 text-sm text-gray-700">
          {log.mensaje}
        </td>
        <td className="px-5 py-3.5 text-center">
          {tieneDetalle && (
            <span className="text-gray-400">
              {expandido ? <ChevronUp className="w-4 h-4 inline" /> : <ChevronDown className="w-4 h-4 inline" />}
            </span>
          )}
        </td>
      </tr>

      {expandido && tieneDetalle && (
        <tr className="bg-gray-50 border-b border-gray-100">
          <td colSpan={5} className="px-5 py-4">
            <pre className="text-xs text-gray-600 whitespace-pre-wrap break-all font-mono bg-white border border-gray-200 rounded-xl p-4 max-h-64 overflow-y-auto">
              {log.detalle}
            </pre>
          </td>
        </tr>
      )}
    </>
  )
}

export default function LogsPage() {
  const [logs, setLogs] = useState<AppLogDto[]>([])
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [pagina, setPagina] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const cargar = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const res = await getLogs(pagina)
      if (res.success) {
        setLogs(res.data.items)
        setTotalRegistros(res.data.totalRegistros)
        setTotalPaginas(res.data.totalPaginas)
      }
    } catch {
      setError('No se pudieron cargar los logs.')
    } finally {
      setLoading(false)
    }
  }, [pagina])

  useEffect(() => { cargar() }, [cargar])

  return (
    <DashboardLayout titulo="Visor de Logs">

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>
      )}

      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600 whitespace-nowrap">Timestamp</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Nivel</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Origen</th>
                <th className="text-left px-5 py-3.5 font-semibold text-gray-600">Mensaje</th>
                <th className="w-10" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 4 }).map((_, j) => (
                      <td key={j} className="px-5 py-4">
                        <div className="h-4 bg-gray-100 rounded animate-pulse" />
                      </td>
                    ))}
                    <td />
                  </tr>
                ))
              ) : logs.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-16 text-gray-400">
                    No hay registros de log todavía
                  </td>
                </tr>
              ) : (
                logs.map(log => <LogRow key={log.id} log={log} />)
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

    </DashboardLayout>
  )
}
