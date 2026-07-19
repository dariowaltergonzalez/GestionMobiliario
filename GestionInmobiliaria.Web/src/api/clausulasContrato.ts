import client from './client'

export interface ClausulaContratoDto {
  id: number
  orden: number
  numero: string
  titulo: string
  texto: string
  activo: boolean
}

export interface CreateClausulaContratoRequest {
  numero: string
  titulo: string
  texto: string
}

export interface UpdateClausulaContratoRequest {
  numero: string
  titulo: string
  texto: string
  activo: boolean
}

export interface PlaceholderCampo {
  clave: string
  descripcion: string
}

export interface PlaceholderGrupo {
  entidad: string
  etiqueta: string
  campos: PlaceholderCampo[]
}

export const getClausulas = async (): Promise<ClausulaContratoDto[]> => {
  const res = await client.get('/api/clausulas-contrato')
  return res.data.data
}

export const getPlaceholders = async (): Promise<PlaceholderGrupo[]> => {
  const res = await client.get('/api/clausulas-contrato/placeholders')
  return res.data.data
}

export const createClausula = async (req: CreateClausulaContratoRequest): Promise<ClausulaContratoDto> => {
  const res = await client.post('/api/clausulas-contrato', req)
  return res.data.data
}

export const updateClausula = async (id: number, req: UpdateClausulaContratoRequest): Promise<ClausulaContratoDto> => {
  const res = await client.put(`/api/clausulas-contrato/${id}`, req)
  return res.data.data
}

export const deleteClausula = async (id: number): Promise<void> => {
  await client.delete(`/api/clausulas-contrato/${id}`)
}

export const moverClausula = async (id: number, subir: boolean): Promise<void> => {
  await client.put(`/api/clausulas-contrato/${id}/mover?subir=${subir}`)
}

export const inicializarClausulas = async (): Promise<ClausulaContratoDto[]> => {
  const res = await client.post('/api/clausulas-contrato/inicializar')
  return res.data.data
}
