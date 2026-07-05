import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export interface SolicitudTasacionDto {
  id: number
  nombre: string
  apellido: string
  email: string | null
  telefono: string
  tipoPropiedad: string
  direccion: string
  barrio: string | null
  ciudad: string | null
  superficieTotal: number | null
  superficieCubierta: number | null
  ambientes: number | null
  banios: number | null
  antiguedad: number | null
  estadoConservacion: string
  descripcion: string | null
  tipoContactoPreferido: string
  estado: string
  notasInternas: string | null
  valorEstimado: number | null
  agenteId: number | null
  nombreAgente: string | null
  fotos: FotoDto[]
  fechaCreacion: string
  fechaActualizacion: string
}

export interface FotoDto {
  id: number
  url: string
  nombreArchivo: string | null
  fechaSubida: string
}

export interface UpdateTasacionRequest {
  estado: number
  agenteId: number | null
  notasInternas: string | null
  valorEstimado: number | null
}

export interface FiltrosTasaciones {
  buscar?: string
  estado?: string
  pagina: number
  tamano: number
}

export const ESTADOS_TASACION: Record<number, { label: string; color: string }> = {
  1: { label: 'Pendiente',  color: 'bg-red-100 text-red-700' },
  2: { label: 'Asignada',   color: 'bg-yellow-100 text-yellow-700' },
  3: { label: 'En proceso', color: 'bg-blue-100 text-blue-700' },
  4: { label: 'Completada', color: 'bg-green-100 text-green-700' },
  5: { label: 'Cancelada',  color: 'bg-gray-100 text-gray-500' },
}

export function estadoNumero(s: string): number {
  return ({ Pendiente: 1, Asignada: 2, EnProceso: 3, Completada: 4, Cancelada: 5 } as Record<string, number>)[s] ?? 1
}

export const getTasaciones = async (filtros: FiltrosTasaciones) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.buscar) params.buscar = filtros.buscar
  if (filtros.estado) params.estado = filtros.estado
  const res = await client.get<ApiResponse<PagedResult<SolicitudTasacionDto>>>('/api/solicitudes-tasacion', { params })
  return res.data
}

export const getTasacion = async (id: number) => {
  const res = await client.get<ApiResponse<SolicitudTasacionDto>>(`/api/solicitudes-tasacion/${id}`)
  return res.data
}

export const updateTasacion = async (id: number, data: UpdateTasacionRequest) => {
  const res = await client.put<ApiResponse<SolicitudTasacionDto>>(`/api/solicitudes-tasacion/${id}`, data)
  return res.data
}

export const deleteTasacion = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/solicitudes-tasacion/${id}`)
  return res.data
}

export const deleteFoto = async (id: number, fotoId: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/solicitudes-tasacion/${id}/fotos/${fotoId}`)
  return res.data
}
