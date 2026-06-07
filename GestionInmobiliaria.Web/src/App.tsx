import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import LandingPage from './pages/public/LandingPage'
import LoginPage from './pages/auth/LoginPage'
import DashboardPage from './pages/dashboard/DashboardPage'
import PropiedadesPage from './pages/dashboard/propiedades/PropiedadesPage'
import LogsPage from './pages/dashboard/logs/LogsPage'
import AuditoriaPage from './pages/dashboard/auditoria/AuditoriaPage'
import PropietariosPage from './pages/dashboard/propietarios/PropietariosPage'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dashboard" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
      <Route path="/dashboard/propiedades" element={<ProtectedRoute><PropiedadesPage /></ProtectedRoute>} />
      <Route path="/dashboard/logs" element={<ProtectedRoute><LogsPage /></ProtectedRoute>} />
      <Route path="/dashboard/auditoria" element={<ProtectedRoute><AuditoriaPage /></ProtectedRoute>} />
      <Route path="/dashboard/propietarios" element={<ProtectedRoute><PropietariosPage /></ProtectedRoute>} />
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
