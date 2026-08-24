import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { Building2, AlertTriangle, Wallet, ChevronDown, ChevronUp, FileImage } from 'lucide-react'
import { getPortalPropietario, type PortalPropietarioDto } from '../../api/portal'

const API_URL = 'http://localhost:5005'

function formatMoneda(monto: number) {
  return `$ ${monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function formatFecha(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function mesAnio(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleDateString('es-AR', { month: 'long', year: 'numeric' })
}

const ESTADO_BADGE: Record<string, string> = {
  Pendiente: 'bg-yellow-100 text-yellow-700',
  Parcial: 'bg-orange-100 text-orange-700',
  Liquidado: 'bg-green-100 text-green-700',
}

export default function PortalPropietarioPage() {
  const { token } = useParams<{ token: string }>()
  const [data, setData] = useState<PortalPropietarioDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [expandidas, setExpandidas] = useState<Set<number>>(new Set())

  const toggleExpandida = (i: number) =>
    setExpandidas(prev => {
      const next = new Set(prev)
      if (next.has(i)) next.delete(i); else next.add(i)
      return next
    })

  useEffect(() => {
    if (!token) return
    getPortalPropietario(token)
      .then(res => { if (res.success) setData(res.data); else setError(res.message ?? 'Link inválido.') })
      .catch(() => setError('Link inválido o vencido.'))
      .finally(() => setLoading(false))
  }, [token])

  if (loading) {
    return <div className="min-h-screen flex items-center justify-center text-gray-400">Cargando...</div>
  }

  if (error || !data) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <div className="text-center max-w-sm">
          <AlertTriangle className="w-10 h-10 text-red-400 mx-auto mb-3" />
          <p className="text-gray-600">{error || 'No se pudo cargar la información.'}</p>
          <p className="text-sm text-gray-400 mt-2">Pedile a tu inmobiliaria que te comparta el link de nuevo.</p>
        </div>
      </div>
    )
  }

  const pendientes = data.liquidaciones.filter(l => l.estado !== 'Liquidado')
  const totalPendiente = pendientes.reduce((sum, l) => sum + (l.montoALiquidar - l.montoAbonado), 0)

  return (
    <div className="min-h-screen bg-gray-200">
      <div className="bg-blue-900 text-white px-4 py-5">
        <div className="max-w-lg mx-auto flex items-center gap-3">
          {data.logoUrl
            ? <img src={data.logoUrl} alt="" className="w-10 h-10 rounded-lg object-cover bg-white" />
            : <Building2 className="w-8 h-8 opacity-80" />}
          <div>
            <p className="font-semibold">{data.nombreEmpresa || 'Portal del propietario'}</p>
            <p className="text-xs text-blue-200">Hola, {data.propietarioNombre} {data.propietarioApellido}</p>
          </div>
        </div>
      </div>

      <div className="max-w-lg mx-auto px-4 py-5 space-y-5">
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5">
          <p className="text-xs text-gray-400 uppercase tracking-wide">Total pendiente de cobrar</p>
          <p className="text-2xl font-bold text-gray-800 mt-1">{formatMoneda(totalPendiente)}</p>
          <p className="text-xs text-gray-400 mt-1">{pendientes.length} liquidación(es) sin cobrar del todo</p>
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
          <p className="px-5 pt-4 pb-2 text-xs font-semibold text-gray-400 uppercase tracking-wide flex items-center gap-1.5">
            <Wallet className="w-3.5 h-3.5" /> Liquidaciones
          </p>
          {data.liquidaciones.length === 0 ? (
            <p className="px-5 pb-4 text-sm text-gray-400">Todavía no hay liquidaciones registradas.</p>
          ) : (
            <div className="divide-y divide-gray-50">
              {data.liquidaciones.map((l, i) => {
                const hayDetalle = l.abonos.length > 0 || l.gastos.length > 0
                const abierta = expandidas.has(i)
                return (
                  <div key={i} className="px-5 py-3">
                    <button
                      onClick={() => hayDetalle && toggleExpandida(i)}
                      className={`w-full flex items-center justify-between gap-3 text-left ${hayDetalle ? 'cursor-pointer' : ''}`}
                    >
                      <div className="min-w-0 flex items-center gap-1.5">
                        {hayDetalle && (abierta
                          ? <ChevronUp className="w-3.5 h-3.5 text-gray-300 shrink-0" />
                          : <ChevronDown className="w-3.5 h-3.5 text-gray-300 shrink-0" />)}
                        <div className="min-w-0">
                          <p className="text-sm font-medium text-gray-700 truncate">{l.propiedadDireccion}</p>
                          <p className="text-xs text-gray-400">{l.contratoCodigo} · {mesAnio(l.periodo)}</p>
                        </div>
                      </div>
                      <div className="text-right shrink-0">
                        <p className="text-sm font-semibold text-gray-800">{formatMoneda(l.montoALiquidar)}</p>
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ESTADO_BADGE[l.estado] ?? ''}`}>
                          {l.estado === 'Liquidado' && l.fechaLiquidacion ? `Liquidado ${formatFecha(l.fechaLiquidacion)}` : l.estado}
                        </span>
                      </div>
                    </button>
                    {l.montoAbonado > 0 && l.estado !== 'Liquidado' && (
                      <p className="text-xs text-gray-400 mt-1">Cobrado hasta ahora: {formatMoneda(l.montoAbonado)}</p>
                    )}
                    {l.montoGastos > 0 && (
                      <p className="text-xs text-gray-400 mt-1">Incluye {formatMoneda(l.montoGastos)} de gastos descontados</p>
                    )}

                    {abierta && (
                      <div className="mt-3 bg-slate-800 rounded-xl p-3 space-y-3">
                        {l.abonos.length > 0 && (
                          <div>
                            <p className="text-xs font-bold text-white uppercase tracking-wide mb-1.5">Transferencias recibidas</p>
                            <div className="space-y-2">
                              {l.abonos.map((a, j) => (
                                <div key={j} className="flex items-center justify-between gap-2 text-xs">
                                  <div className="min-w-0">
                                    <p className="text-white font-bold">{formatMoneda(a.monto)} <span className="text-slate-300 font-normal">· {formatFecha(a.fecha)} · {a.medio}</span></p>
                                    <p className="text-slate-300 truncate">
                                      {a.entidadDestino && a.entidadDestino}
                                      {a.numeroOperacion && ` · Op. ${a.numeroOperacion}`}
                                    </p>
                                  </div>
                                  {a.comprobanteUrl && (
                                    <a href={`${API_URL}${a.comprobanteUrl}`} target="_blank" rel="noreferrer"
                                      className="shrink-0 flex items-center gap-1 text-yellow-300 hover:text-yellow-200 font-bold">
                                      <FileImage className="w-3.5 h-3.5" /> Comprobante
                                    </a>
                                  )}
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                        {l.gastos.length > 0 && (
                          <div>
                            <p className="text-xs font-bold text-white uppercase tracking-wide mb-1.5">Gastos descontados</p>
                            <div className="space-y-1">
                              {l.gastos.map((g, j) => (
                                <div key={j} className="flex items-center justify-between gap-2 text-xs">
                                  <p className="text-slate-200 truncate">{g.categoria}{g.descripcion && ` · ${g.descripcion}`}</p>
                                  <p className="text-white font-bold shrink-0">{formatMoneda(g.monto)}</p>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          )}
        </div>

        <p className="text-center text-xs text-gray-300 pt-2">{data.nombreEmpresa}</p>
      </div>
    </div>
  )
}
