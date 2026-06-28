import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export type TipoEvento = 1 | 2 | 3 | 4
export type EstadoEvento = 1 | 2 | 3

export const TIPOS_EVENTO: Record<TipoEvento, { label: string; color: string; icon: string }> = {
  1: { label: 'Visita',   color: 'bg-blue-100 text-blue-700',   icon: '🏠' },
  2: { label: 'Llamada',  color: 'bg-green-100 text-green-700', icon: '📞' },
  3: { label: 'Reunión',  color: 'bg-purple-100 text-purple-700', icon: '🤝' },
  4: { label: 'Otro',     color: 'bg-gray-100 text-gray-600',   icon: '📌' },
}

export const ESTADOS_EVENTO: Record<EstadoEvento, { label: string; color: string }> = {
  1: { label: 'Pendiente',  color: 'bg-yellow-100 text-yellow-700' },
  2: { label: 'Realizada',  color: 'bg-green-100 text-green-700' },
  3: { label: 'Cancelada',  color: 'bg-gray-100 text-gray-500' },
}

export interface EventoAgendaDto {
  id: number
  tipo: string
  estado: string
  fechaHora: string
  notas: string | null
  agenteId: number
  agenteNombre: string
  propiedadId: number | null
  propiedadDireccion: string | null
  leadId: number | null
  leadNombre: string | null
  inquilinoId: number | null
  inquilinoNombre: string | null
}

export interface CreateEventoAgendaRequest {
  tipo: number
  fechaHora: string
  notas?: string
  agenteId: number
  propiedadId?: number | null
  leadId?: number | null
  inquilinoId?: number | null
}

export interface UpdateEventoAgendaRequest extends CreateEventoAgendaRequest {
  estado: number
}

export interface FiltrosAgenda {
  tipo?: string
  estado?: string
  agenteId?: string
  pagina: number
  tamano: number
}

export const getEventos = async (filtros: FiltrosAgenda) => {
  const params: Record<string, string> = {
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  }
  if (filtros.tipo) params.tipo = filtros.tipo
  if (filtros.estado) params.estado = filtros.estado
  if (filtros.agenteId) params.agenteId = filtros.agenteId
  const res = await client.get<ApiResponse<PagedResult<EventoAgendaDto>>>('/api/eventosagenda', { params })
  return res.data
}

export const createEvento = async (data: CreateEventoAgendaRequest) => {
  const res = await client.post<ApiResponse<EventoAgendaDto>>('/api/eventosagenda', data)
  return res.data
}

export const updateEvento = async (id: number, data: UpdateEventoAgendaRequest) => {
  const res = await client.put<ApiResponse<EventoAgendaDto>>(`/api/eventosagenda/${id}`, data)
  return res.data
}

export const deleteEvento = async (id: number) => {
  const res = await client.delete<ApiResponse<boolean>>(`/api/eventosagenda/${id}`)
  return res.data
}
