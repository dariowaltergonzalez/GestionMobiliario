import client from './client'

function descargarPdf(blob: Blob, nombre: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = nombre
  a.click()
  URL.revokeObjectURL(url)
}

export const exportarPropietariosPdf = async (params: { buscar?: string; activo?: string }) => {
  const query = new URLSearchParams()
  if (params.buscar) query.append('buscar', params.buscar)
  if (params.activo !== undefined && params.activo !== '') query.append('activo', params.activo)

  const res = await client.get(`/api/reportes/propietarios?${query}`, { responseType: 'blob' })
  descargarPdf(res.data, `Propietarios_${new Date().toISOString().slice(0, 10)}.pdf`)
}

export const exportarPropiedadesPdf = async (params: { buscar?: string; tipo?: string; estado?: string; operacion?: string }) => {
  const query = new URLSearchParams()
  if (params.buscar) query.append('buscar', params.buscar)
  if (params.tipo) query.append('tipo', params.tipo)
  if (params.estado) query.append('estado', params.estado)
  if (params.operacion) query.append('operacion', params.operacion)

  const res = await client.get(`/api/reportes/propiedades?${query}`, { responseType: 'blob' })
  descargarPdf(res.data, `Propiedades_${new Date().toISOString().slice(0, 10)}.pdf`)
}

export const exportarAgendaPdf = async (params: { estado?: string; tipo?: string; agenteId?: string }) => {
  const query = new URLSearchParams()
  if (params.estado) query.append('estado', params.estado)
  if (params.tipo) query.append('tipo', params.tipo)
  if (params.agenteId) query.append('agenteId', params.agenteId)

  const res = await client.get(`/api/reportes/agenda?${query}`, { responseType: 'blob' })
  descargarPdf(res.data, `Agenda_${new Date().toISOString().slice(0, 10)}.pdf`)
}

export const exportarReservasPdf = async (params: { buscar?: string; estado?: string }) => {
  const query = new URLSearchParams()
  if (params.buscar) query.append('buscar', params.buscar)
  if (params.estado) query.append('estado', params.estado)

  const res = await client.get(`/api/reportes/reservas?${query}`, { responseType: 'blob' })
  descargarPdf(res.data, `Reservas_${new Date().toISOString().slice(0, 10)}.pdf`)
}

export const exportarTasacionesPdf = async (params: { buscar?: string; estado?: string }) => {
  const query = new URLSearchParams()
  if (params.buscar) query.append('buscar', params.buscar)
  if (params.estado) query.append('estado', params.estado)

  const res = await client.get(`/api/reportes/tasaciones?${query}`, { responseType: 'blob' })
  descargarPdf(res.data, `Tasaciones_${new Date().toISOString().slice(0, 10)}.pdf`)
}

export const exportarContratoPdf = async (id: number, codigo: string) => {
  const res = await client.get(`/api/reportes/contratos/${id}`, { responseType: 'blob' })
  descargarPdf(res.data, `Contrato_${codigo}.pdf`)
}
