import { type ReactNode, useState, useEffect } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { Home, Users, UserCog, FileText, Calendar, LogOut, Building2, ClipboardList, Bell, ScrollText, ShieldCheck, Settings, Handshake, BookOpen, FilePen, Banknote, User, Wallet, Receipt, ChevronLeft, ChevronRight } from 'lucide-react'
import { useAuth } from '../../context/AuthContext'
import { getConfiguracionPublica } from '../../api/configuracion'

const navItems = [
  { icono: Home,         label: 'Dashboard',    href: '/dashboard',                roles: ['Admin', 'Agente'] },
  { icono: Building2,    label: 'Propiedades',  href: '/dashboard/propiedades',    roles: ['Admin', 'Agente'] },
  { icono: Users,        label: 'Leads',        href: '/dashboard/leads',          roles: ['Admin', 'Agente'] },
  { icono: ClipboardList,label: 'Tasaciones',   href: '/dashboard/tasaciones',     roles: ['Admin', 'Agente'] },
  { icono: Handshake,    label: 'Reservas',     href: '/dashboard/reservas',       roles: ['Admin', 'Agente'] },
  { icono: BookOpen,     label: 'Contratos',    href: '/dashboard/contratos',      roles: ['Admin', 'Agente'] },
  { icono: Banknote,     label: 'Pagos',        href: '/dashboard/pagos',          roles: ['Admin', 'Operador'] },
  { icono: Wallet,       label: 'Liquidaciones',href: '/dashboard/liquidaciones',  roles: ['Admin', 'Operador'] },
  { icono: Receipt,      label: 'Gastos',       href: '/dashboard/gastos',         roles: ['Admin', 'Operador'] },
  { icono: FilePen,      label: 'Plantilla Contrato', href: '/dashboard/clausulas-contrato', roles: ['Admin'] },
  { icono: Calendar,     label: 'Agenda',       href: '/dashboard/agenda',         roles: ['Admin', 'Agente'] },
  { icono: UserCog,      label: 'Agentes',      href: '/dashboard/agentes',        roles: ['Admin'] },
  { icono: FileText,     label: 'Propietarios', href: '/dashboard/propietarios',   roles: ['Admin'] },
  { icono: User,         label: 'Inquilinos',   href: '/dashboard/inquilinos',     roles: ['Admin'] },
  { icono: ScrollText,   label: 'Logs',         href: '/dashboard/logs',           roles: ['Admin'] },
  { icono: ShieldCheck,  label: 'Auditoría',    href: '/dashboard/auditoria',      roles: ['Admin'] },
  { icono: Settings,     label: 'Configuración',href: '/dashboard/configuracion',  roles: ['Admin'] },
]

interface Props {
  children: ReactNode
  titulo: string
}

export default function DashboardLayout({ children, titulo }: Props) {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [nombreEmpresa, setNombreEmpresa] = useState('')
  const [colapsado, setColapsado] = useState(() => localStorage.getItem('sidebarColapsado') === 'true')

  useEffect(() => {
    localStorage.setItem('sidebarColapsado', String(colapsado))
  }, [colapsado])

  useEffect(() => {
    getConfiguracionPublica()
      .then(res => { if (res.success && res.data) setNombreEmpresa(res.data.nombreComercial) })
      .catch(() => {})
  }, [])

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  const isActive = (href: string) =>
    href === '/dashboard'
      ? location.pathname === '/dashboard'
      : location.pathname.startsWith(href)

  return (
    <div className="min-h-screen bg-gray-50 flex">

      {/* SIDEBAR */}
      <aside className={`${colapsado ? 'w-20' : 'w-64'} bg-blue-950 flex flex-col shrink-0 fixed h-full z-10 transition-all duration-200`}>
        <div className={`relative border-b border-blue-900 ${colapsado ? 'p-4' : 'p-6'}`}>
          <Link to="/" className="flex items-center gap-2 overflow-hidden">
            <div className="w-8 h-8 bg-yellow-400 rounded-lg flex items-center justify-center shrink-0">
              <Home className="w-4 h-4 text-blue-900" />
            </div>
            {!colapsado && (
              <div className="min-w-0">
                <div className="text-white font-bold text-sm truncate">{nombreEmpresa}</div>
                <div className="text-blue-400 text-xs">Panel de gestión</div>
              </div>
            )}
          </Link>

          <button
            onClick={() => setColapsado(v => !v)}
            title={colapsado ? 'Expandir menú' : 'Colapsar menú'}
            className="absolute top-1/2 -right-3 -translate-y-1/2 w-6 h-6 bg-blue-800 hover:bg-blue-700 text-white rounded-full flex items-center justify-center shadow-md transition-colors"
          >
            {colapsado ? <ChevronRight className="w-3.5 h-3.5" /> : <ChevronLeft className="w-3.5 h-3.5" />}
          </button>
        </div>

        <nav className="flex-1 py-6 px-3 overflow-y-auto overflow-x-hidden">
          {!colapsado && (
            <div className="text-blue-500 text-xs font-semibold uppercase tracking-wider px-3 mb-3">Módulos</div>
          )}
          <ul className="space-y-1">
            {navItems.filter(item => item.roles.includes(user?.rol ?? '')).map(({ icono: Icon, label, href }) => (
              <li key={href}>
                <Link
                  to={href}
                  title={colapsado ? label : undefined}
                  className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors ${
                    colapsado ? 'justify-center' : ''
                  } ${
                    isActive(href)
                      ? 'bg-blue-800 text-white'
                      : 'text-blue-300 hover:bg-blue-900 hover:text-white'
                  }`}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  {!colapsado && label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>

        <div className="p-4 border-t border-blue-900">
          <div className={`flex items-center gap-3 mb-3 ${colapsado ? 'justify-center' : ''}`}>
            <div className="w-8 h-8 bg-blue-800 rounded-full flex items-center justify-center text-white text-xs font-bold shrink-0">
              {user?.nombre?.[0]}{user?.apellido?.[0]}
            </div>
            {!colapsado && (
              <div className="min-w-0">
                <div className="text-white text-sm font-medium truncate">{user?.nombre} {user?.apellido}</div>
                <div className="text-blue-400 text-xs">{user?.rol}</div>
              </div>
            )}
          </div>
          <button
            onClick={handleLogout}
            title={colapsado ? 'Cerrar sesión' : undefined}
            className={`w-full flex items-center gap-2 text-blue-400 hover:text-white text-sm px-2 py-1.5 rounded-lg hover:bg-blue-900 transition-colors ${
              colapsado ? 'justify-center' : ''
            }`}
          >
            <LogOut className="w-4 h-4" />
            {!colapsado && 'Cerrar sesión'}
          </button>
        </div>
      </aside>

      {/* CONTENIDO */}
      <div className={`flex-1 flex flex-col transition-all duration-200 ${colapsado ? 'ml-20' : 'ml-64'}`}>
        <header className="bg-white border-b border-gray-100 px-8 py-4 flex items-center justify-between sticky top-0 z-10">
          <h1 className="text-xl font-bold text-gray-800">{titulo}</h1>
          <button className="relative p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-xl transition-colors">
            <Bell className="w-5 h-5" />
            <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-red-500 rounded-full" />
          </button>
        </header>

        <main className="flex-1 p-8">
          {children}
        </main>
      </div>
    </div>
  )
}
