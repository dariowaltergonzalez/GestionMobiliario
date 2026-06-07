import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export type NivelLog = 1 | 2 | 3

export const NIVELES_LOG: Record<NivelLog, string> = {
  1: 'Info', 2: 'Advertencia', 3: 'Error',
}

export const BADGE_NIVEL: Record<NivelLog, string> = {
  1: 'bg-blue-100 text-blue-700',
  2: 'bg-yellow-100 text-yellow-700',
  3: 'bg-red-100 text-red-700',
}

export interface AppLogDto {
  id: number
  timestamp: string
  nivel: NivelLog
  nivelNombre: string
  origen: string
  mensaje: string
  detalle: string | null
}

export const getLogs = async (pagina: number, tamano = 20) => {
  const res = await client.get<ApiResponse<PagedResult<AppLogDto>>>(
    `/api/logs?pagina=${pagina}&tamano=${tamano}`
  )
  return res.data
}
