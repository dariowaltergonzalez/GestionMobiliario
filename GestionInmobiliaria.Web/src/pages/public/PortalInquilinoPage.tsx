import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { Building2, AlertTriangle, CheckCircle2, Clock, Receipt } from 'lucide-react'
import { getPortalInquilino, type PortalInquilinoDto } from '../../api/portal'

function formatMoneda(monto: number, moneda: string) {
  const fmt = monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return moneda === 'USD' ? `U$S ${fmt}` : `$ ${fmt}`
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
  Pagado: 'bg-green-100 text-green-700',
  Atrasado: 'bg-red-100 text-red-700',
  Resuelto: 'bg-green-100 text-green-700',
}

export default function PortalInquilinoPage() {
  const { token } = useParams<{ token: string }>()
  const [data, setData] = useState<PortalInquilinoDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!token) return
    getPortalInquilino(token)
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

  // data.pagos viene ordenado del más reciente al más antiguo (para el histórico) — para "próxima
  // cuota" hay que buscar la Pendiente/Atrasada más CERCANA en el tiempo, no la primera de la lista.
  const proximaCuota = data.pagos
    .filter(p => p.estado === 'Pendiente' || p.estado === 'Atrasado')
    .sort((a, b) => new Date(a.periodo).getTime() - new Date(b.periodo).getTime())[0]
  const moneda = data.contrato?.moneda ?? 'ARS'

  return (
    <div className="min-h-screen bg-gray-200">
      <div className="bg-blue-900 text-white px-4 py-5">
        <div className="max-w-lg mx-auto flex items-center gap-3">
          {data.logoUrl
            ? <img src={data.logoUrl} alt="" className="w-10 h-10 rounded-lg object-cover bg-white" />
            : <Building2 className="w-8 h-8 opacity-80" />}
          <div>
            <p className="font-semibold">{data.nombreEmpresa || 'Portal del inquilino'}</p>
            <p className="text-xs text-blue-200">Hola, {data.inquilinoNombre} {data.inquilinoApellido}</p>
          </div>
        </div>
      </div>

      <div className="max-w-lg mx-auto px-4 py-5 space-y-5">
        {!data.contrato ? (
          <div className="bg-white rounded-2xl border border-gray-100 p-6 text-center text-gray-400">
            No tenés un contrato vigente en este momento.
          </div>
        ) : (
          <>
            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5">
              <p className="text-xs text-gray-400">{data.contrato.codigo}</p>
              <p className="font-semibold text-gray-800">{data.contrato.propiedadDireccion}</p>
              <p className="text-sm text-gray-500 mt-1">Alquiler vigente: {formatMoneda(data.contrato.montoActual, moneda)}</p>
            </div>

            {proximaCuota ? (
              <div className={`rounded-2xl p-5 border ${proximaCuota.montoPunitorio > 0 ? 'bg-red-50 border-red-200' : 'bg-blue-50 border-blue-100'}`}>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wide mb-1">Próxima cuota — {mesAnio(proximaCuota.periodo)}</p>
                <p className="text-2xl font-bold text-gray-800">{formatMoneda(proximaCuota.montoEsperado, moneda)}</p>
                {proximaCuota.montoPunitorio > 0 && (
                  <div className="flex items-center gap-1.5 mt-2 text-red-700 text-sm">
                    <Clock className="w-4 h-4" />
                    <span>+ {formatMoneda(proximaCuota.montoPunitorio, moneda)} de interés por {proximaCuota.diasAtraso} días de atraso</span>
                  </div>
                )}
              </div>
            ) : (
              <div className="bg-green-50 border border-green-200 rounded-2xl p-5 flex items-center gap-2 text-green-700">
                <CheckCircle2 className="w-5 h-5 shrink-0" />
                <span className="text-sm font-medium">Estás al día con tus pagos.</span>
              </div>
            )}

            {data.gastos.length > 0 && (
              <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
                <p className="px-5 pt-4 pb-2 text-xs font-semibold text-gray-400 uppercase tracking-wide">Gastos a tu cargo</p>
                <div className="divide-y divide-gray-50">
                  {data.gastos.map((g, i) => (
                    <div key={i} className="px-5 py-3 flex items-center justify-between gap-3">
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-gray-700">{g.categoria}</p>
                        {g.descripcion && <p className="text-xs text-gray-400 truncate">{g.descripcion}</p>}
                        <p className="text-xs text-gray-400">{formatFecha(g.fecha)}</p>
                      </div>
                      <div className="text-right shrink-0">
                        <p className="text-sm font-semibold text-gray-800">{formatMoneda(g.monto, moneda)}</p>
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ESTADO_BADGE[g.estado] ?? ''}`}>{g.estado}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
              <p className="px-5 pt-4 pb-2 text-xs font-semibold text-gray-400 uppercase tracking-wide flex items-center gap-1.5">
                <Receipt className="w-3.5 h-3.5" /> Histórico de pagos
              </p>
              <div className="divide-y divide-gray-50">
                {data.pagos.map(p => (
                  <div key={p.numeroCuota} className="px-5 py-3 flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm text-gray-700 capitalize">{mesAnio(p.periodo)}</p>
                      <p className="text-xs text-gray-400">Cuota #{p.numeroCuota}{p.fechaPago && ` · pagado ${formatFecha(p.fechaPago)}`}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-medium text-gray-800">{formatMoneda(p.montoPagado ?? p.montoEsperado, moneda)}</p>
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${ESTADO_BADGE[p.estado] ?? ''}`}>{p.estado}</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </>
        )}

        <p className="text-center text-xs text-gray-300 pt-2">{data.nombreEmpresa}</p>
      </div>
    </div>
  )
}
