import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import LandingPage from './pages/public/LandingPage'
import PropiedadDetallePage from './pages/public/PropiedadDetallePage'
import PortalInquilinoPage from './pages/public/PortalInquilinoPage'
import PortalPropietarioPage from './pages/public/PortalPropietarioPage'
import LoginPage from './pages/auth/LoginPage'
import DashboardPage from './pages/dashboard/DashboardPage'
import PropiedadesPage from './pages/dashboard/propiedades/PropiedadesPage'
import LogsPage from './pages/dashboard/logs/LogsPage'
import AuditoriaPage from './pages/dashboard/auditoria/AuditoriaPage'
import PropietariosPage from './pages/dashboard/propietarios/PropietariosPage'
import InquilinosPage from './pages/dashboard/inquilinos/InquilinosPage'
import ConfiguracionPage from './pages/dashboard/configuracion/ConfiguracionPage'
import TasacionPage from './pages/public/TasacionPage'
import LeadsPage from './pages/dashboard/leads/LeadsPage'
import AgendaPage from './pages/dashboard/agenda/AgendaPage'
import AgentesPage from './pages/dashboard/agentes/AgentesPage'
import TasacionesPage from './pages/dashboard/tasaciones/TasacionesPage'
import ReservasPage from './pages/dashboard/reservas/ReservasPage'
import ContratosPage from './pages/dashboard/contratos/ContratosPage'
import ClausulasContratoPage from './pages/dashboard/clausulas/ClausulasContratoPage'
import PagosPage from './pages/dashboard/pagos/PagosPage'
import LiquidacionesPage from './pages/dashboard/liquidaciones/LiquidacionesPage'
import GastosPage from './pages/dashboard/gastos/GastosPage'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/propiedades/:id" element={<PropiedadDetallePage />} />
      <Route path="/tasacion" element={<TasacionPage />} />
      <Route path="/portal/inquilino/:token" element={<PortalInquilinoPage />} />
      <Route path="/portal/propietario/:token" element={<PortalPropietarioPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dashboard" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
      <Route path="/dashboard/propiedades" element={<ProtectedRoute><PropiedadesPage /></ProtectedRoute>} />
      <Route path="/dashboard/leads" element={<ProtectedRoute><LeadsPage /></ProtectedRoute>} />
      <Route path="/dashboard/agenda" element={<ProtectedRoute><AgendaPage /></ProtectedRoute>} />
      <Route path="/dashboard/logs" element={<ProtectedRoute><LogsPage /></ProtectedRoute>} />
      <Route path="/dashboard/auditoria" element={<ProtectedRoute><AuditoriaPage /></ProtectedRoute>} />
      <Route path="/dashboard/agentes" element={<ProtectedRoute><AgentesPage /></ProtectedRoute>} />
      <Route path="/dashboard/tasaciones" element={<ProtectedRoute><TasacionesPage /></ProtectedRoute>} />
      <Route path="/dashboard/reservas" element={<ProtectedRoute><ReservasPage /></ProtectedRoute>} />
      <Route path="/dashboard/contratos" element={<ProtectedRoute><ContratosPage /></ProtectedRoute>} />
      <Route path="/dashboard/clausulas-contrato" element={<ProtectedRoute><ClausulasContratoPage /></ProtectedRoute>} />
      <Route path="/dashboard/pagos" element={<ProtectedRoute><PagosPage /></ProtectedRoute>} />
      <Route path="/dashboard/liquidaciones" element={<ProtectedRoute><LiquidacionesPage /></ProtectedRoute>} />
      <Route path="/dashboard/gastos" element={<ProtectedRoute><GastosPage /></ProtectedRoute>} />
      <Route path="/dashboard/propietarios" element={<ProtectedRoute><PropietariosPage /></ProtectedRoute>} />
      <Route path="/dashboard/inquilinos" element={<ProtectedRoute><InquilinosPage /></ProtectedRoute>} />
      <Route path="/dashboard/configuracion" element={<ProtectedRoute><ConfiguracionPage /></ProtectedRoute>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  )
}
