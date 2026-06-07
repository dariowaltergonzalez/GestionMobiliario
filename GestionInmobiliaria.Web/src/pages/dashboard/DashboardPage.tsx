import { Users, Calendar, TrendingUp, ChevronRight, Building2, ClipboardList } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import DashboardLayout from '../../components/layout/DashboardLayout'

const statsCards = [
  { label: 'Propiedades activas', valor: '48', icono: Building2, color: 'bg-blue-50 text-blue-700', tendencia: '+3 este mes' },
  { label: 'Leads nuevos', valor: '12', icono: Users, color: 'bg-green-50 text-green-700', tendencia: '+5 esta semana' },
  { label: 'Tasaciones pendientes', valor: '7', icono: ClipboardList, color: 'bg-yellow-50 text-yellow-700', tendencia: '2 sin asignar' },
  { label: 'Eventos hoy', valor: '4', icono: Calendar, color: 'bg-purple-50 text-purple-700', tendencia: 'Próx: 14:00 hs' },
]

export default function DashboardPage() {
  const { user } = useAuth()

  return (
    <DashboardLayout titulo={`Buen día, ${user?.nombre} 👋`}>

      {/* STATS */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5 mb-8">
        {statsCards.map(({ label, valor, icono: Icon, color, tendencia }) => (
          <div key={label} className="bg-white rounded-2xl p-5 shadow-sm border border-gray-100">
            <div className="flex items-center justify-between mb-4">
              <span className="text-sm text-gray-500 font-medium">{label}</span>
              <div className={`w-9 h-9 rounded-xl flex items-center justify-center ${color}`}>
                <Icon className="w-4 h-4" />
              </div>
            </div>
            <div className="text-3xl font-bold text-gray-800 mb-1">{valor}</div>
            <div className="flex items-center gap-1 text-xs text-green-600">
              <TrendingUp className="w-3 h-3" />
              {tendencia}
            </div>
          </div>
        ))}
      </div>

      {/* ACCESOS RÁPIDOS */}
      <div className="grid md:grid-cols-2 gap-6">

        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center justify-between mb-5">
            <h2 className="font-semibold text-gray-800">Tasaciones pendientes</h2>
            <Link to="/dashboard/tasaciones" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
              Ver todas <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
          <ul className="space-y-3">
            {[
              { nombre: 'Roberto Fernández', tipo: 'Departamento', zona: 'Palermo', estado: 'Sin asignar' },
              { nombre: 'Laura Gómez', tipo: 'Casa', zona: 'Belgrano', estado: 'Asignada' },
              { nombre: 'Marcelo Ríos', tipo: 'PH', zona: 'Villa Crespo', estado: 'En proceso' },
            ].map((t) => (
              <li key={t.nombre} className="flex items-center justify-between py-2 border-b border-gray-50 last:border-0">
                <div>
                  <div className="text-sm font-medium text-gray-700">{t.nombre}</div>
                  <div className="text-xs text-gray-400">{t.tipo} · {t.zona}</div>
                </div>
                <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${
                  t.estado === 'Sin asignar' ? 'bg-red-50 text-red-600' :
                  t.estado === 'Asignada' ? 'bg-yellow-50 text-yellow-700' :
                  'bg-blue-50 text-blue-700'
                }`}>
                  {t.estado}
                </span>
              </li>
            ))}
          </ul>
        </div>

        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center justify-between mb-5">
            <h2 className="font-semibold text-gray-800">Próximos eventos</h2>
            <Link to="/dashboard/agenda" className="text-blue-600 text-sm flex items-center gap-1 hover:gap-2 transition-all">
              Ver agenda <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
          <ul className="space-y-3">
            {[
              { hora: '10:00', titulo: 'Visita - Av. Corrientes 1234', tipo: 'Visita', agente: 'Carlos M.' },
              { hora: '12:30', titulo: 'Tasación online - Roberto F.', tipo: 'Online', agente: 'Ana G.' },
              { hora: '15:00', titulo: 'Llamada - Laura G.', tipo: 'Llamada', agente: 'Carlos M.' },
            ].map((ev) => (
              <li key={ev.titulo} className="flex items-start gap-3 py-2 border-b border-gray-50 last:border-0">
                <div className="text-xs font-bold text-blue-900 bg-blue-50 px-2 py-1 rounded-lg shrink-0 mt-0.5">{ev.hora}</div>
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium text-gray-700 truncate">{ev.titulo}</div>
                  <div className="text-xs text-gray-400">{ev.agente}</div>
                </div>
                <span className="text-xs text-gray-400 shrink-0">{ev.tipo}</span>
              </li>
            ))}
          </ul>
        </div>

      </div>
    </DashboardLayout>
  )
}
