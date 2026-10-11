import { useEffect, useState } from 'react'
import { Users, Calendar, TrendingUp, ChevronRight, Building2, ClipboardList, Percent, Trophy, Wallet, PiggyBank, AlertTriangle } from 'lucide-react'
import { Link } from 'react-router-dom'
import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid } from 'recharts'
import { useAuth } from '../../context/AuthContext'
import DashboardLayout from '../../components/layout/DashboardLayout'
import client from '../../api/client'
import type { ApiResponse, PagedResult } from '../../types/api'
import type { EventoAgendaDto } from '../../api/agenda'
import { getComisionAgenteMetricas, getTopAgentesMes, type ComisionAgenteMetricasDto, type TopAgenteDto } from '../../api/comisionesAgentes'
import { getPropiedadesPorEstado, type PropiedadPorEstadoDto } from '../../api/propiedades'
import { getIngresosMensuales, getComisionEstimadaMes, getMorosidad, type IngresoMensualDto, type MorosidadDto } from '../../api/pagos'

const LABEL_ESTADO_PROPIEDAD: Record<string, string> = {
  Disponible: 'Disponible',
  Alquilada: 'Alquilada',
  EnMantenimiento: 'En mantenimiento',
  NoDisponible: 'No disponible',
  Vendida: 'Vendida',
  Reservada: 'Reservada',
  BoletoFirmado: 'Boleto firmado',
}

const COLOR_ESTADO_PROPIEDAD: Record<string, string> = {
  Disponible: '#16a34a',
  Alquilada: '#2563eb',
  EnMantenimiento: '#d97706',
  NoDisponible: '#6b7280',
  Vendida: '#9333ea',
  Reservada: '#eab308',
  BoletoFirmado: '#0d9488',
}

const NOMBRES_MES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic']

interface TasacionResumen {
  id: number
  nombre: string
  apellido: string
  tipoPropiedad: string
  barrio: string | null
  ciudad: string | null
  estado: string
}

interface Stats {
  propiedadesActivas: number
  leadsNuevos: number
  tasacionesPendientes: number
  eventosHoy: number
  proximoEvento: string | null
}

function estaHoy(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  const d = new Date(utc)
  const hoy = new Date()
  return d.getFullYear() === hoy.getFullYear()
    && d.getMonth() === hoy.getMonth()
    && d.getDate() === hoy.getDate()
}

function esFuturaOHoy(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc) >= new Date(new Date().setHours(0, 0, 0, 0))
}

function formatFechaHora(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  const d = new Date(utc)
  const hoy = new Date()
  const manana = new Date(hoy); manana.setDate(hoy.getDate() + 1)

  const mismoAnio = d.getFullYear() === hoy.getFullYear()
  const esHoy = mismoAnio && d.getMonth() === hoy.getMonth() && d.getDate() === hoy.getDate()
  const esManana = mismoAnio && d.getMonth() === manana.getMonth() && d.getDate() === manana.getDate()

  const hora = d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })
  if (esHoy) return `Hoy ${hora}`
  if (esManana) return `Mañana ${hora}`
  return d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' }) + ` ${hora}`
}

function estadoBadgeClass(estado: string) {
  switch (estado) {
    case 'Pendiente': return 'bg-red-50 text-red-600'
    case 'Asignada':  return 'bg-yellow-50 text-yellow-700'
    case 'EnProceso': return 'bg-blue-50 text-blue-700'
    default:          return 'bg-gray-50 text-gray-500'
  }
}

function estadoLabel(estado: string) {
  switch (estado) {
    case 'EnProceso': return 'En proceso'
    default: return estado
  }
}

function tipoBadge(tipo: string) {
  switch (tipo) {
    case 'Visita':  return '🏠'
    case 'Llamada': return '📞'
    case 'Reunion': return '🤝'
    default:        return '📌'
  }
}

function SkeletonLine({ w = 'w-full' }: { w?: string }) {
  return <div className={`h-4 ${w} bg-gray-100 rounded animate-pulse`} />
}

export default function DashboardPage() {
  const { user } = useAuth()

  const [stats, setStats] = useState<Stats | null>(null)
  const [tasaciones, setTasaciones] = useState<TasacionResumen[]>([])
  const [eventos, setEventos] = useState<EventoAgendaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [comisionesStats, setComisionesStats] = useState<ComisionAgenteMetricasDto | null>(null)
  const [topAgentes, setTopAgentes] = useState<TopAgenteDto[]>([])
  const [propiedadesPorEstado, setPropiedadesPorEstado] = useState<PropiedadPorEstadoDto[]>([])
  const [ingresosMensuales, setIngresosMensuales] = useState<IngresoMensualDto[]>([])
  const [comisionEstimadaMes, setComisionEstimadaMes] = useState<number | null>(null)
  const [gananciaProximoMes, setGananciaProximoMes] = useState<number | null>(null)
  const [morosidad, setMorosidad] = useState<MorosidadDto | null>(null)
  const puedeVerComisiones = user?.rol === 'Admin' || user?.rol === 'Operador'

  useEffect(() => {
    const agenteId = user?.agenteId ? String(user.agenteId) : ''

    Promise.all([
      client.get<ApiResponse<PagedResult<unknown>>>('/api/propiedades?pagina=1&tamano=1'),
      client.get<ApiResponse<PagedResult<unknown>>>('/api/leads?pagina=1&tamano=1&estado=1'),
      client.get<ApiResponse<PagedResult<TasacionResumen>>>('/api/solicitudes-tasacion?pagina=1&tamano=5&estado=1'),
      client.get<ApiResponse<PagedResult<EventoAgendaDto>>>(
        `/api/eventosagenda?pagina=1&tamano=100&estado=1${agenteId ? `&agenteId=${agenteId}` : ''}`
      ),
    ]).then(([propRes, leadsRes, tasRes, evRes]) => {
      const eventosData = evRes.data.success ? evRes.data.data.items : []
      const eventosHoy = eventosData.filter(e => estaHoy(e.fechaHora))
      const proximos = eventosData
        .filter(e => esFuturaOHoy(e.fechaHora))
        .sort((a, b) => new Date(a.fechaHora).getTime() - new Date(b.fechaHora).getTime())

      const proximoEvento = proximos.length > 0 ? `Próx: ${formatFechaHora(proximos[0].fechaHora)}` : null

      setStats({
        propiedadesActivas: propRes.data.success ? propRes.data.data.totalRegistros : 0,
        leadsNuevos: leadsRes.data.success ? leadsRes.data.data.totalRegistros : 0,
        tasacionesPendientes: tasRes.data.success ? tasRes.data.data.totalRegistros : 0,
        eventosHoy: eventosHoy.length,
        proximoEvento,
      })

      setTasaciones(tasRes.data.success ? tasRes.data.data.items : [])
      setEventos(proximos.slice(0, 5))
    }).catch(() => {
      setStats({ propiedadesActivas: 0, leadsNuevos: 0, tasacionesPendientes: 0, eventosHoy: 0, proximoEvento: null })
    }).finally(() => setLoading(false))
  }, [user?.agenteId])

  useEffect(() => {
    if (!puedeVerComisiones) return
    Promise.all([
      getComisionAgenteMetricas(), getTopAgentesMes(3), getComisionEstimadaMes(),
      getComisionEstimadaMes(1), getMorosidad(),
    ]).then(([metricasRes, topRes, estimadaRes, proximoMesRes, morosidadRes]) => {
      if (metricasRes.success) setComisionesStats(metricasRes.data)
      if (topRes.success) setTopAgentes(topRes.data)
      if (estimadaRes.success) setComisionEstimadaMes(estimadaRes.data.monto)
      if (proximoMesRes.success) setGananciaProximoMes(proximoMesRes.data.monto)
      if (morosidadRes.success) setMorosidad(morosidadRes.data)
    }).catch(() => {})
  }, [puedeVerComisiones])

  useEffect(() => {
    getPropiedadesPorEstado().then(res => { if (res.success) setPropiedadesPorEstado(res.data) }).catch(() => {})
  }, [])

  useEffect(() => {
    if (!puedeVerComisiones) return
    getIngresosMensuales(12).then(res => { if (res.success) setIngresosMensuales(res.data) }).catch(() => {})
  }, [puedeVerComisiones])

  const statsCards = [
    {
      label: 'Propiedades activas',
      valor: stats?.propiedadesActivas ?? '—',
      icono: Building2,
      color: 'bg-blue-50 text-blue-700',
      tendencia: 'en el sistema',
      href: '/dashboard/propiedades',
    },
    {
      label: 'Leads nuevos',
      valor: stats?.leadsNuevos ?? '—',
      icono: Users,
      color: 'bg-green-50 text-green-700',
      tendencia: 'sin contactar',
      href: '/dashboard/leads',
    },
    {
      label: 'Tasaciones pendientes',
      valor: stats?.tasacionesPendientes ?? '—',
      icono: ClipboardList,
      color: 'bg-yellow-50 text-yellow-700',
      tendencia: 'por gestionar',
      href: '/dashboard/tasaciones',
    },
    {
      label: 'Eventos hoy',
      valor: stats?.eventosHoy ?? '—',
      icono: Calendar,
      color: 'bg-purple-50 text-purple-700',
      tendencia: stats?.proximoEvento ?? 'sin eventos',
      href: '/dashboard/agenda',
    },
    ...(puedeVerComisiones ? [{
      label: 'Comisiones pendientes',
      valor: comisionesStats?.pendientesCount ?? '—',
      icono: Percent,
      color: 'bg-orange-50 text-orange-700',
      tendencia: comisionesStats ? `$ ${comisionesStats.montoTotalPendiente.toLocaleString('es-AR', { maximumFractionDigits: 0 })} por pagar` : 'por pagar',
      href: '/dashboard/comisiones-agentes',
    }] : []),
    ...(puedeVerComisiones ? [{
      label: 'Comisión estimada del mes',
      valor: comisionEstimadaMes !== null ? `$ ${comisionEstimadaMes.toLocaleString('es-AR', { maximumFractionDigits: 0 })}` : '—',
      icono: Wallet,
      color: 'bg-teal-50 text-teal-700',
      tendencia: 'administración, este mes',
      href: '/dashboard/pagos',
    }] : []),
    ...(puedeVerComisiones ? [{
      label: 'Ganancia estimada mes que viene',
      valor: gananciaProximoMes !== null ? `$ ${gananciaProximoMes.toLocaleString('es-AR', { maximumFractionDigits: 0 })}` : '—',
      icono: PiggyBank,
      color: 'bg-indigo-50 text-indigo-700',
      tendencia: 'tu % de administración',
      href: '/dashboard/pagos',
    }] : []),
    ...(puedeVerComisiones ? [{
      label: 'Morosidad acumulada',
      valor: morosidad?.cantidadCuotas ?? '—',
      icono: AlertTriangle,
      color: 'bg-red-50 text-red-700',
      tendencia: morosidad ? `$ ${morosidad.montoTotal.toLocaleString('es-AR', { maximumFractionDigits: 0 })} vencido sin cobrar` : 'vencido sin cobrar',
      href: '/dashboard/pagos',
    }] : []),
  ]

  return (
    <DashboardLayout titulo={`Buen día, ${user?.nombre} 👋`}>

      {/* STATS */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5 mb-8">
        {statsCards.map(({ label, valor, icono: Icon, color, tendencia, href }) => (
          <Link key={label} to={href} className="bg-white rounded-2xl p-5 shadow-sm border border-gray-100 hover:shadow-md transition-shadow">
            <div className="flex items-center justify-between mb-4">
              <span className="text-sm text-gray-500 font-medium">{label}</span>
              <div className={`w-9 h-9 rounded-xl flex items-center justify-center ${color}`}>
                <Icon className="w-4 h-4" />
              </div>
            </div>
            {loading ? (
              <div className="h-9 w-16 bg-gray-100 rounded animate-pulse mb-2" />
            ) : (
              <div className="text-3xl font-bold text-gray-800 mb-1">{valor}</div>
            )}
            <div className="flex items-center gap-1 text-xs text-gray-400">
              <TrendingUp className="w-3 h-3" />
              {tendencia}
            </div>
          </Link>
        ))}
      </div>

      {/* PANELES */}
      <div className="grid md:grid-cols-2 gap-6">

        {/* Tasaciones pendientes */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center justify-between mb-5">
            <h2 className="font-semibold text-gray-800">Tasaciones pendientes</h2>
            <Link to="/dashboard/tasaciones" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
              Ver todas <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
          <ul className="space-y-3">
            {loading ? (
              Array.from({ length: 3 }).map((_, i) => (
                <li key={i} className="flex items-center justify-between py-2 border-b border-gray-50">
                  <div className="space-y-2 flex-1">
                    <SkeletonLine w="w-36" />
                    <SkeletonLine w="w-24" />
                  </div>
                  <SkeletonLine w="w-16" />
                </li>
              ))
            ) : tasaciones.length === 0 ? (
              <li className="text-sm text-gray-400 text-center py-6">Sin tasaciones pendientes</li>
            ) : (
              tasaciones.map(t => (
                <li key={t.id} className="flex items-center justify-between py-2 border-b border-gray-50 last:border-0">
                  <div>
                    <div className="text-sm font-medium text-gray-700">{t.apellido}, {t.nombre}</div>
                    <div className="text-xs text-gray-400">
                      {t.tipoPropiedad}{t.barrio ? ` · ${t.barrio}` : t.ciudad ? ` · ${t.ciudad}` : ''}
                    </div>
                  </div>
                  <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${estadoBadgeClass(t.estado)}`}>
                    {estadoLabel(t.estado)}
                  </span>
                </li>
              ))
            )}
          </ul>
        </div>

        {/* Próximos eventos */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center justify-between mb-5">
            <h2 className="font-semibold text-gray-800">Próximos eventos</h2>
            <Link to="/dashboard/agenda" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
              Ver agenda <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
          <ul className="space-y-3">
            {loading ? (
              Array.from({ length: 3 }).map((_, i) => (
                <li key={i} className="flex items-start gap-3 py-2 border-b border-gray-50">
                  <div className="w-12 h-7 bg-gray-100 rounded-lg animate-pulse shrink-0" />
                  <div className="space-y-2 flex-1">
                    <SkeletonLine w="w-40" />
                    <SkeletonLine w="w-24" />
                  </div>
                </li>
              ))
            ) : eventos.length === 0 ? (
              <li className="text-sm text-gray-400 text-center py-6">Sin eventos próximos</li>
            ) : (
              eventos.map(ev => (
                <li key={ev.id} className="flex items-start gap-3 py-2 border-b border-gray-50 last:border-0">
                  <div className="text-xs font-bold text-blue-900 bg-blue-50 px-2 py-1 rounded-lg shrink-0 mt-0.5 whitespace-nowrap">
                    {formatFechaHora(ev.fechaHora)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium text-gray-700 truncate">
                      {tipoBadge(ev.tipo)} {ev.tipo} {ev.propiedadDireccion ? `· ${ev.propiedadDireccion}` : ev.leadNombre ? `· ${ev.leadNombre}` : ''}
                    </div>
                    <div className="text-xs text-gray-400">{ev.agenteNombre}</div>
                  </div>
                </li>
              ))
            )}
          </ul>
        </div>

        {/* Ingresos últimos 12 meses */}
        {puedeVerComisiones && (
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6 md:col-span-2">
            <div className="flex items-center justify-between mb-5">
              <h2 className="font-semibold text-gray-800">Ingresos últimos 12 meses</h2>
              <Link to="/dashboard/pagos" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
                Ver pagos <ChevronRight className="w-4 h-4" />
              </Link>
            </div>
            {ingresosMensuales.length === 0 ? (
              <div className="text-sm text-gray-400 text-center py-16">Sin cobros registrados todavía</div>
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <LineChart data={ingresosMensuales.map(i => ({ ...i, label: NOMBRES_MES[i.mes - 1] }))}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" />
                  <XAxis dataKey="label" tick={{ fontSize: 12, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                  <YAxis
                    tick={{ fontSize: 12, fill: '#9ca3af' }}
                    axisLine={false}
                    tickLine={false}
                    tickFormatter={(v: number) => v >= 1000 ? `${(v / 1000).toFixed(0)}k` : String(v)}
                  />
                  <Tooltip formatter={(v: number) => [`$ ${v.toLocaleString('es-AR', { maximumFractionDigits: 0 })}`, 'Cobrado']} />
                  <Line type="monotone" dataKey="monto" stroke="#2563eb" strokeWidth={2} dot={{ r: 3 }} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        )}

        {/* Estado de propiedades */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center justify-between mb-5">
            <h2 className="font-semibold text-gray-800">Estado de propiedades</h2>
            <Link to="/dashboard/propiedades" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
              Ver todas <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
          {propiedadesPorEstado.length === 0 ? (
            <div className="text-sm text-gray-400 text-center py-16">Sin propiedades cargadas</div>
          ) : (
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie
                  data={propiedadesPorEstado.map(p => ({ ...p }))}
                  dataKey="cantidad"
                  nameKey="estado"
                  innerRadius={55}
                  outerRadius={85}
                  paddingAngle={2}
                >
                  {propiedadesPorEstado.map(p => (
                    <Cell key={p.estado} fill={COLOR_ESTADO_PROPIEDAD[p.estado] ?? '#9ca3af'} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: number, _n: string, entry) => [v, LABEL_ESTADO_PROPIEDAD[String(entry.payload?.estado)] ?? entry.payload?.estado]} />
                <Legend
                  formatter={(value: string) => LABEL_ESTADO_PROPIEDAD[value] ?? value}
                  wrapperStyle={{ fontSize: 12 }}
                />
              </PieChart>
            </ResponsiveContainer>
          )}
        </div>

        {/* Top agentes del mes */}
        {puedeVerComisiones && (
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
            <div className="flex items-center justify-between mb-5">
              <h2 className="font-semibold text-gray-800">Top agentes del mes</h2>
              <Link to="/dashboard/comisiones-agentes" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
                Ver comisiones <ChevronRight className="w-4 h-4" />
              </Link>
            </div>
            <ul className="space-y-3">
              {topAgentes.length === 0 ? (
                <li className="text-sm text-gray-400 text-center py-6">Todavía no se pagó ninguna comisión este mes</li>
              ) : (
                topAgentes.map((a, i) => (
                  <li key={a.agenteId} className="flex items-center justify-between py-2 border-b border-gray-50 last:border-0">
                    <div className="flex items-center gap-3">
                      <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold shrink-0 ${
                        i === 0 ? 'bg-yellow-100 text-yellow-700' : i === 1 ? 'bg-gray-100 text-gray-600' : 'bg-orange-50 text-orange-600'
                      }`}>
                        {i === 0 ? <Trophy className="w-3.5 h-3.5" /> : i + 1}
                      </div>
                      <div>
                        <div className="text-sm font-medium text-gray-700">{a.agenteNombre}</div>
                        <div className="text-xs text-gray-400">{a.cantidadPagos} comisión(es) pagada(s)</div>
                      </div>
                    </div>
                    <span className="text-sm font-bold text-gray-800">
                      $ {a.montoPagadoMes.toLocaleString('es-AR', { maximumFractionDigits: 0 })}
                    </span>
                  </li>
                ))
              )}
            </ul>
          </div>
        )}

      </div>
    </DashboardLayout>
  )
}
