import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import LandingPage from './pages/public/LandingPage'
import PropiedadDetallePage from './pages/public/PropiedadDetallePage'
import LoginPage from './pages/auth/LoginPage'
import DashboardPage from './pages/dashboard/DashboardPage'
import PropiedadesPage from './pages/dashboard/propiedades/PropiedadesPage'
import LogsPage from './pages/dashboard/logs/LogsPage'
import AuditoriaPage from './pages/dashboard/auditoria/AuditoriaPage'
import PropietariosPage from './pages/dashboard/propietarios/PropietariosPage'
import ConfiguracionPage from './pages/dashboard/configuracion/ConfiguracionPage'
import TasacionPage from './pages/public/TasacionPage'
import LeadsPage from './pages/dashboard/leads/LeadsPage'
import AgendaPage from './pages/dashboard/agenda/AgendaPage'
import AgentesPage from './pages/dashboard/agentes/AgentesPage'
import TasacionesPage from './pages/dashboard/tasaciones/TasacionesPage'

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
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dashboard" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
      <Route path="/dashboard/propiedades" element={<ProtectedRoute><PropiedadesPage /></ProtectedRoute>} />
      <Route path="/dashboard/leads" element={<ProtectedRoute><LeadsPage /></ProtectedRoute>} />
      <Route path="/dashboard/agenda" element={<ProtectedRoute><AgendaPage /></ProtectedRoute>} />
      <Route path="/dashboard/logs" element={<ProtectedRoute><LogsPage /></ProtectedRoute>} />
      <Route path="/dashboard/auditoria" element={<ProtectedRoute><AuditoriaPage /></ProtectedRoute>} />
      <Route path="/dashboard/agentes" element={<ProtectedRoute><AgentesPage /></ProtectedRoute>} />
      <Route path="/dashboard/tasaciones" element={<ProtectedRoute><TasacionesPage /></ProtectedRoute>} />
      <Route path="/dashboard/propietarios" element={<ProtectedRoute><PropietariosPage /></ProtectedRoute>} />
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
