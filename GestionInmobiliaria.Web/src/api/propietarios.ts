import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export interface PropietarioDto {
  id: number
  nombre: string
  apellido: string
  dni: string | null
  cuit: string | null
  email: string | null
  telefono: string | null
  telefono2: string | null
  direccion: string | null
  banco: string | null
  cbu: string | null
  notas: string | null
  notificaciones: Record<string, boolean>
  activo: boolean
  fechaCreacion: string
  cantidadPropiedades: number
}

export interface PropietarioComboDto {
  id: number
  nombreCompleto: string
}

export interface TemaNotificacionDto {
  codigo: string
  label: string
}

export interface PropietarioFormData {
  nombre: string
  apellido: string
  dni: string
  cuit: string
  email: string
  telefono: string
  telefono2: string
  direccion: string
  banco: string
  cbu: string
  notas: string
  notificaciones: Record<string, boolean>
}

export const propietarioFormVacio: PropietarioFormData = {
  nombre: '', apellido: '', dni: '', cuit: '', email: '',
  telefono: '', telefono2: '', direccion: '', banco: '', cbu: '', notas: '',
  notificaciones: {},
}

export const getTemasNotificacionPropietario = async () => {
  const res = await client.get<ApiResponse<TemaNotificacionDto[]>>('/api/propietarios/temas-notificacion')
  return res.data
}

export interface FiltrosPropietarios {
  buscar: string
  activo: string
  pagina: number
  tamano: number
}

export const getPropietarios = async (filtros: FiltrosPropietarios) => {
  const params = new URLSearchParams({
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  })
  if (filtros.buscar) params.append('buscar', filtros.buscar)
  if (filtros.activo !== '') params.append('activo', filtros.activo)
  const res = await client.get<ApiResponse<PagedResult<PropietarioDto>>>(`/api/propietarios?${params}`)
  return res.data
}

export const getPropietariosActivos = async () => {
  const res = await client.get<ApiResponse<PropietarioComboDto[]>>('/api/propietarios/activos')
  return res.data
}

const toRequest = (f: PropietarioFormData, activo?: boolean) => ({
  nombre: f.nombre,
  apellido: f.apellido,
  dni: f.dni || null,
  cuit: f.cuit || null,
  email: f.email || null,
  telefono: f.telefono || null,
  telefono2: f.telefono2 || null,
  direccion: f.direccion || null,
  banco: f.banco || null,
  cbu: f.cbu || null,
  notas: f.notas || null,
  notificaciones: f.notificaciones,
  ...(activo !== undefined ? { activo } : {}),
})

export const createPropietario = async (form: PropietarioFormData) => {
  const res = await client.post<ApiResponse<{ id: number }>>('/api/propietarios', toRequest(form))
  return res.data
}

export const updatePropietario = async (id: number, form: PropietarioFormData, activo: boolean) => {
  const res = await client.put<ApiResponse<null>>(`/api/propietarios/${id}`, toRequest(form, activo))
  return res.data
}

export const deletePropietario = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/propietarios/${id}`)
  return res.data
}

export const generarTokenPortalPropietario = async (id: number) => {
  const res = await client.post<ApiResponse<{ token: string }>>(`/api/propietarios/${id}/token-portal`)
  return res.data
}
