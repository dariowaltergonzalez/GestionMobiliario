import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'
import type { TemaNotificacionDto } from './propietarios'

export type { TemaNotificacionDto }

export interface AgenteDto {
  id: number
  userId: string
  nombre: string
  apellido: string
  nombreCompleto: string
  email: string
  zona: string | null
  telefonoInterno: string | null
  comisionPorcentaje: number
  notas: string | null
  telefonoWhatsApp: string | null
  notificaciones: Record<string, boolean>
  notificacionesWhatsApp: Record<string, boolean>
  activo: boolean
  fechaCreacion: string
  cantidadPropiedades: number
  cantidadInquilinos: number
}

export interface CreateAgenteRequest {
  nombre: string
  apellido: string
  email: string
  password: string
  zona?: string
  telefonoInterno?: string
  comisionPorcentaje: number
  notas?: string
  telefonoWhatsApp?: string
  notificaciones?: Record<string, boolean>
  notificacionesWhatsApp?: Record<string, boolean>
}

export interface UpdateAgenteRequest {
  nombre: string
  apellido: string
  zona?: string
  telefonoInterno?: string
  comisionPorcentaje: number
  notas?: string
  telefonoWhatsApp?: string
  notificaciones?: Record<string, boolean>
  notificacionesWhatsApp?: Record<string, boolean>
  activo: boolean
}

export const getTemasNotificacionAgente = async () => {
  const res = await client.get<ApiResponse<TemaNotificacionDto[]>>('/api/agentes/temas-notificacion')
  return res.data
}

export interface FiltrosAgentes {
  buscar?: string
  activo?: string
  pagina: number
  tamano: number
}

export const getAgentes = async (filtros: FiltrosAgentes) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.buscar) params.buscar = filtros.buscar
  if (filtros.activo !== undefined && filtros.activo !== '') params.activo = filtros.activo
  const res = await client.get<ApiResponse<PagedResult<AgenteDto>>>('/api/agentes', { params })
  return res.data
}

export const createAgente = async (data: CreateAgenteRequest) => {
  const res = await client.post<ApiResponse<{ id: number }>>('/api/agentes', data)
  return res.data
}

export const updateAgente = async (id: number, data: UpdateAgenteRequest) => {
  const res = await client.put<ApiResponse<null>>(`/api/agentes/${id}`, data)
  return res.data
}

export const deleteAgente = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/agentes/${id}`)
  return res.data
}
