import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

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
}

export interface UpdateAgenteRequest {
  nombre: string
  apellido: string
  zona?: string
  telefonoInterno?: string
  comisionPorcentaje: number
  notas?: string
  activo: boolean
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
