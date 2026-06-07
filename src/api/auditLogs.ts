import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export interface AuditLogDto {
  id: number
  timestamp: string
  entityName: string
  action: string
  entityId: string
  userName: string | null
  oldValues: string | null
  newValues: string | null
  changedProperties: string | null
}

export const ACCIONES = ['INSERT', 'UPDATE', 'DELETE'] as const

export const BADGE_ACCION: Record<string, string> = {
  INSERT: 'bg-green-100 text-green-700',
  UPDATE: 'bg-yellow-100 text-yellow-700',
  DELETE: 'bg-red-100 text-red-700',
}

export const LABEL_ACCION: Record<string, string> = {
  INSERT: 'Creación',
  UPDATE: 'Modificación',
  DELETE: 'Eliminación',
}

export const getAuditLogs = async (pagina: number, accion?: string, entidad?: string, tamano = 20) => {
  const params = new URLSearchParams({ pagina: String(pagina), tamano: String(tamano) })
  if (accion) params.append('accion', accion)
  if (entidad) params.append('entidad', entidad)
  const res = await client.get<ApiResponse<PagedResult<AuditLogDto>>>(`/api/audit-logs?${params}`)
  return res.data
}

export const getEntidades = async () => {
  const res = await client.get<ApiResponse<string[]>>('/api/audit-logs/entidades')
  return res.data
}
