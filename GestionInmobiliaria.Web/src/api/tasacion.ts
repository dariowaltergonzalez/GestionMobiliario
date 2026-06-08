import client from './client'
import type { ApiResponse } from '../types/api'

export interface CreateSolicitudTasacionRequest {
  nombre: string
  apellido: string
  email?: string
  telefono: string
  tipoPropiedad: number
  direccion: string
  barrio?: string
  ciudad?: string
  superficieTotal?: number
  superficieCubierta?: number
  ambientes?: number
  banios?: number
  antiguedad?: number
  estadoConservacion: number
  descripcion?: string
  tipoContactoPreferido: number
}

export const solicitarTasacion = async (
  data: CreateSolicitudTasacionRequest,
  tenantSlug?: string | null
) => {
  const headers: Record<string, string> = {}
  if (tenantSlug) headers['X-Tenant'] = tenantSlug
  const res = await client.post<ApiResponse<{ id: number }>>(
    '/api/solicitudes-tasacion/solicitar',
    data,
    { headers }
  )
  return res.data
}
