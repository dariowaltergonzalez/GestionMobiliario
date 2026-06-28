import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export type EstadoLead = 1 | 2 | 3 | 4 | 5
export type OrigenLead = 1 | 2 | 3 | 4 | 5 | 6

export const ESTADOS_LEAD: Record<EstadoLead, { label: string; color: string }> = {
  1: { label: 'Nuevo',      color: 'bg-blue-100 text-blue-700' },
  2: { label: 'Contactado', color: 'bg-yellow-100 text-yellow-700' },
  3: { label: 'Interesado', color: 'bg-green-100 text-green-700' },
  4: { label: 'Convertido', color: 'bg-emerald-100 text-emerald-700' },
  5: { label: 'Descartado', color: 'bg-gray-100 text-gray-500' },
}

export const ORIGENES_LEAD: Record<OrigenLead, { label: string; color: string }> = {
  1: { label: 'Web',          color: 'bg-indigo-100 text-indigo-700' },
  2: { label: 'WhatsApp',     color: 'bg-green-100 text-green-700' },
  3: { label: 'Referido',     color: 'bg-purple-100 text-purple-700' },
  4: { label: 'Redes Soc.',   color: 'bg-pink-100 text-pink-700' },
  5: { label: 'Llamada',      color: 'bg-orange-100 text-orange-700' },
  6: { label: 'Otro',         color: 'bg-gray-100 text-gray-600' },
}

export interface LeadDto {
  id: number
  nombre: string
  apellido: string
  email: string | null
  telefono: string | null
  origen: string
  estado: string
  notas: string | null
  activo: boolean
  fechaCreacion: string
  agenteId: number | null
  agenteNombre: string | null
  propiedadId: number | null
  propiedadDireccion: string | null
}

export interface CreateLeadRequest {
  nombre: string
  apellido: string
  email?: string
  telefono?: string
  origen: number
  notas?: string
  agenteId?: number | null
  propiedadId?: number | null
}

export interface UpdateLeadRequest extends CreateLeadRequest {
  estado: number
}

export interface FiltrosLeads {
  buscar?: string
  estado?: string
  pagina: number
  tamano: number
}

export const getLeads = async (filtros: FiltrosLeads) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.buscar) params.buscar = filtros.buscar
  if (filtros.estado) params.estado = filtros.estado
  const res = await client.get<ApiResponse<PagedResult<LeadDto>>>('/api/leads', { params })
  return res.data
}

export const createLead = async (data: CreateLeadRequest) => {
  const res = await client.post<ApiResponse<LeadDto>>('/api/leads', data)
  return res.data
}

export const updateLead = async (id: number, data: UpdateLeadRequest) => {
  const res = await client.put<ApiResponse<LeadDto>>(`/api/leads/${id}`, data)
  return res.data
}

export const deleteLead = async (id: number) => {
  const res = await client.delete<ApiResponse<boolean>>(`/api/leads/${id}`)
  return res.data
}
