export interface ApiResponse<T> {
  success: boolean
  data: T
  message: string | null
  errors: string[]
}

export interface PagedResult<T> {
  items: T[]
  pagina: number
  tamano: number
  totalRegistros: number
  totalPaginas: number
  tienePaginaAnterior: boolean
  tienePaginaSiguiente: boolean
}
