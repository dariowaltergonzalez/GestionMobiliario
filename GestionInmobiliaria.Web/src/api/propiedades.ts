import client from './client'
import type { ApiResponse, PagedResult } from '../types/api'

export type TipoPropiedad = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8
export type TipoOperacion = 1 | 2 | 3
export type EstadoPropiedad = 1 | 2 | 3 | 4 | 5 | 6
export type EstadoConservacion = 1 | 2 | 3 | 4

export const TIPOS_PROPIEDAD: Record<TipoPropiedad, string> = {
  1: 'Departamento', 2: 'Casa', 3: 'Local',
  4: 'Oficina', 5: 'Terreno', 6: 'Galpón', 7: 'PH', 8: 'Otro',
}

export const TIPOS_OPERACION: Record<TipoOperacion, string> = {
  1: 'Alquiler', 2: 'Venta', 3: 'Alquiler o Venta',
}

export const ESTADOS_PROPIEDAD: Record<EstadoPropiedad, string> = {
  1: 'Disponible', 2: 'Alquilada', 3: 'En mantenimiento',
  4: 'No disponible', 5: 'Vendida', 6: 'Reservada',
}

export const ESTADOS_CONSERVACION: Record<EstadoConservacion, string> = {
  1: 'Excelente', 2: 'Bueno', 3: 'Regular', 4: 'A refaccionar',
}

export interface FotoPropiedadDto {
  id: number
  url: string
  nombreArchivo: string
  esPrincipal: boolean
  orden: number
}

export interface PropiedadDto {
  id: number
  tipo: TipoPropiedad
  tipoNombre: string
  operacion: TipoOperacion
  operacionNombre: string
  direccion: string
  barrio: string | null
  ciudad: string | null
  provincia: string | null
  direccionCompleta: string
  ambientes: number | null
  dormitorios: number | null
  banios: number | null
  superficieTotal: number | null
  superficieCubierta: number | null
  piso: string | null
  numeroDepartamento: string | null
  precioAlquiler: number | null
  precioVenta: number | null
  expensas: number | null
  estado: EstadoPropiedad
  estadoNombre: string
  estadoConservacion: EstadoConservacion | null
  estadoConservacionNombre: string | null
  cochera: boolean
  antiguedad: number | null
  tieneCalefaccion: boolean
  aceptaMascotas: boolean
  tienePiscina: boolean
  nroCatastro: string | null
  descripcion: string | null
  notas: string | null
  activo: boolean
  fechaCreacion: string
  propietarioId: number
  propietarioNombre: string
  fotos: FotoPropiedadDto[]
  fotoPrincipalUrl: string | null
}

export interface PropiedadFormData {
  tipo: TipoPropiedad
  operacion: TipoOperacion
  direccion: string
  barrio: string
  ciudad: string
  provincia: string
  piso: string
  numeroDepartamento: string
  ambientes: string
  dormitorios: string
  banios: string
  superficieTotal: string
  superficieCubierta: string
  antiguedad: string
  precioAlquiler: string
  precioVenta: string
  expensas: string
  estado: EstadoPropiedad
  estadoConservacion: EstadoConservacion | ''
  cochera: boolean
  tieneCalefaccion: boolean
  aceptaMascotas: boolean
  tienePiscina: boolean
  nroCatastro: string
  descripcion: string
  notas: string
  propietarioId: string
}

export const propiedadFormVacio = (): PropiedadFormData => ({
  tipo: 1,
  operacion: 1,
  direccion: '',
  barrio: '',
  ciudad: '',
  provincia: '',
  piso: '',
  numeroDepartamento: '',
  ambientes: '',
  dormitorios: '',
  banios: '',
  superficieTotal: '',
  superficieCubierta: '',
  antiguedad: '',
  precioAlquiler: '',
  precioVenta: '',
  expensas: '',
  estado: 1,
  estadoConservacion: '',
  cochera: false,
  tieneCalefaccion: false,
  aceptaMascotas: false,
  tienePiscina: false,
  nroCatastro: '',
  descripcion: '',
  notas: '',
  propietarioId: '',
})

export interface FiltrosPropiedades {
  buscar: string
  tipo: string
  estado: string
  operacion: string
  pagina: number
  tamano: number
}

const toNum = (v: string) => v !== '' ? Number(v) : null

const formToRequest = (f: PropiedadFormData) => ({
  tipo: f.tipo,
  operacion: f.operacion,
  direccion: f.direccion,
  barrio: f.barrio || null,
  ciudad: f.ciudad || null,
  provincia: f.provincia || null,
  piso: f.piso || null,
  numeroDepartamento: f.numeroDepartamento || null,
  ambientes: toNum(f.ambientes),
  dormitorios: toNum(f.dormitorios),
  banios: toNum(f.banios),
  superficieTotal: toNum(f.superficieTotal),
  superficieCubierta: toNum(f.superficieCubierta),
  antiguedad: toNum(f.antiguedad),
  precioAlquiler: toNum(f.precioAlquiler),
  precioVenta: toNum(f.precioVenta),
  expensas: toNum(f.expensas),
  estado: f.estado,
  estadoConservacion: f.estadoConservacion !== '' ? Number(f.estadoConservacion) : null,
  cochera: f.cochera,
  tieneCalefaccion: f.tieneCalefaccion,
  aceptaMascotas: f.aceptaMascotas,
  tienePiscina: f.tienePiscina,
  nroCatastro: f.nroCatastro || null,
  descripcion: f.descripcion || null,
  notas: f.notas || null,
  propietarioId: Number(f.propietarioId),
})

export const getPropiedades = async (filtros: FiltrosPropiedades) => {
  const params = new URLSearchParams({
    pagina: String(filtros.pagina),
    tamano: String(filtros.tamano),
  })
  if (filtros.buscar) params.append('buscar', filtros.buscar)
  if (filtros.tipo) params.append('tipo', filtros.tipo)
  if (filtros.estado) params.append('estado', filtros.estado)
  if (filtros.operacion) params.append('operacion', filtros.operacion)

  const res = await client.get<ApiResponse<PagedResult<PropiedadDto>>>(`/api/propiedades?${params}`)
  return res.data
}

export const createPropiedad = async (form: PropiedadFormData) => {
  const res = await client.post<ApiResponse<{ id: number }>>('/api/propiedades', formToRequest(form))
  return res.data
}

export const updatePropiedad = async (id: number, form: PropiedadFormData) => {
  const res = await client.put<ApiResponse<null>>(`/api/propiedades/${id}`, formToRequest(form))
  return res.data
}

export const deletePropiedad = async (id: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/propiedades/${id}`)
  return res.data
}

export interface PropiedadPublicaDto {
  id: number
  tipoNombre: string
  operacionNombre: string
  direccion: string
  barrio: string | null
  ciudad: string | null
  provincia: string | null
  ambientes: number | null
  dormitorios: number | null
  banios: number | null
  superficieTotal: number | null
  superficieCubierta: number | null
  piso: string | null
  numeroDepartamento: string | null
  precioAlquiler: number | null
  precioVenta: number | null
  expensas: number | null
  cochera: boolean
  tieneCalefaccion: boolean
  aceptaMascotas: boolean
  tienePiscina: boolean
  antiguedad: number | null
  estadoConservacionNombre: string | null
  descripcion: string | null
  videoUrl: string | null
  fotoPrincipalUrl: string | null
  fotosUrls: string[]
}

export const getPropiedad = async (id: number) => {
  const res = await client.get<ApiResponse<PropiedadDto>>(`/api/propiedades/${id}`)
  return res.data
}

export const subirFotosPropiedad = async (propiedadId: number, archivos: File[]) => {
  const form = new FormData()
  archivos.forEach(f => form.append('fotos', f))
  const res = await client.post<ApiResponse<FotoPropiedadDto[]>>(
    `/api/propiedades/${propiedadId}/fotos`, form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  )
  return res.data
}

export const setFotoPrincipal = async (propiedadId: number, fotoId: number) => {
  const res = await client.put<ApiResponse<null>>(`/api/propiedades/${propiedadId}/fotos/${fotoId}/principal`, {})
  return res.data
}

export const deleteFotoPropiedad = async (propiedadId: number, fotoId: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/propiedades/${propiedadId}/fotos/${fotoId}`)
  return res.data
}

export const getPropiedadesPublicas = async (tenantSlug?: string | null) => {
  const headers: Record<string, string> = {}
  if (tenantSlug) headers['X-Tenant'] = tenantSlug
  const res = await client.get<ApiResponse<PropiedadPublicaDto[]>>('/api/propiedades/publicas', { headers })
  return res.data
}

export const getPropiedadPublica = async (id: number, tenantSlug?: string | null) => {
  const headers: Record<string, string> = {}
  if (tenantSlug) headers['X-Tenant'] = tenantSlug
  const res = await client.get<ApiResponse<PropiedadPublicaDto>>(`/api/propiedades/publica/${id}`, { headers })
  return res.data
}

export const subirVideoPropiedad = async (propiedadId: number, archivo: File) => {
  const form = new FormData()
  form.append('video', archivo)
  const res = await client.post<ApiResponse<{ videoUrl: string }>>(
    `/api/propiedades/${propiedadId}/video`, form,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  )
  return res.data
}

export const deleteVideoPropiedad = async (propiedadId: number) => {
  const res = await client.delete<ApiResponse<null>>(`/api/propiedades/${propiedadId}/video`)
  return res.data
}
