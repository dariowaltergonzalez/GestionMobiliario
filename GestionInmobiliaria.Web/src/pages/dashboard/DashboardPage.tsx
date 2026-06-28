import { useEffect, useState } from 'react'
import { Users, Calendar, TrendingUp, ChevronRight, Building2, ClipboardList } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import DashboardLayout from '../../components/layout/DashboardLayout'
import client from '../../api/client'
import type { ApiResponse, PagedResult } from '../../types/api'
import type { EventoAgendaDto } from '../../api/agenda'

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

function formatHora(iso: string) {
  const utc = iso.endsWith('Z') ? iso : iso + 'Z'
  return new Date(utc).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })
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

      </div>
    </DashboardLayout>
  )
}
