import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'
import type { TemaNotificacionDto } from './propietarios'

export type { TemaNotificacionDto }

export interface InquilinoDto {
  id: number
  nombre: string
  apellido: string
  dni: string | null
  cuit: string | null
  email: string | null
  telefono: string | null
  telefono2: string | null
  direccion: string | null
  ocupacion: string | null
  nombreGarante: string | null
  telefonoGarante: string | null
  dniGarante: string | null
  notas: string | null
  notificaciones: Record<string, boolean>
  activo: boolean
  fechaCreacion: string
}

export interface InquilinoComboDto {
  id: number
  nombreCompleto: string
}

export interface InquilinoFormData {
  nombre: string
  apellido: string
  dni: string
  cuit: string
  email: string
  telefono: string
  telefono2: string
  direccion: string
  ocupacion: string
  nombreGarante: string
  telefonoGarante: string
  dniGarante: string
  notas: string
  notificaciones: Record<string, boolean>
}

export const inquilinoFormVacio: InquilinoFormData = {
  nombre: '', apellido: '', dni: '', cuit: '', email: '',
  telefono: '', telefono2: '', direccion: '', ocupacion: '',
  nombreGarante: '', telefonoGarante: '', dniGarante: '', notas: '',
  notificaciones: {},
}

export interface FiltrosInquilinos {
  buscar: string
  activo: string
  pagina: number
  tamano: number
}

export const getInquilinos = async (filtros: FiltrosInquilinos) => {
  const params = new URLSearchParams({
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  })
  if (filtros.buscar) params.append('buscar', filtros.buscar)
  if (filtros.activo !== '') params.append('activo', filtros.activo)
  const res = await client.get<ApiResponse<PagedResult<InquilinoDto>>>(`/api/inquilinos?${params}`)
  return res.data
}

export const getInquilinosActivos = async () => {
  const res = await client.get<ApiResponse<InquilinoComboDto[]>>('/api/inquilinos/activos')
  return res.data
}

export const getTemasNotificacionInquilino = async () => {
  const res = await client.get<ApiResponse<TemaNotificacionDto[]>>('/api/inquilinos/temas-notificacion')
  return res.data
}

const toRequest = (f: InquilinoFormData, activo?: boolean) => ({
  nombre: f.nombre,
  apellido: f.apellido,
  dni: f.dni || null,
  cuit: f.cuit || null,
  email: f.email || null,
  telefono: f.telefono || null,
  telefono2: f.telefono2 || null,
  direccion: f.direccion || null,
  ocupacion: f.ocupacion || null,
  nombreGarante: f.nombreGarante || null,
  telefonoGarante: f.telefonoGarante || null,
  dniGarante: f.dniGarante || null,
  notas: f.notas || null,
  notificaciones: f.notificaciones,
  ...(activo !== undefined ? { activo } : {}),
})

export const createInquilino = async (form: InquilinoFormData) => {
  const res = await client.post<ApiResponse<{ id: number }>>('/api/inquilinos', toRequest(form))
  return res.data
}

export const updateInquilino = async (id: number, form: InquilinoFormData, activo: boolean) => {
  const res = await client.put<ApiResponse<null>>(`/api/inquilinos/${id}`, toRequest(form, activo))
  return res.data
}

export const deleteInquilino = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/inquilinos/${id}`)
  return res.data
}

export const generarTokenPortalInquilino = async (id: number) => {
  const res = await client.post<ApiResponse<{ token: string }>>(`/api/inquilinos/${id}/token-portal`)
  return res.data
}
