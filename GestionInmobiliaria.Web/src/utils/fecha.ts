/**
 * Fecha en formato YYYY-MM-DD según la hora local del navegador.
 * No usar toISOString(): trabaja en UTC y, después de las 21 hs en Argentina, devuelve el día siguiente.
 */
export function fechaLocal(fecha: Date = new Date()): string {
  const y = fecha.getFullYear()
  const m = String(fecha.getMonth() + 1).padStart(2, '0')
  const d = String(fecha.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}
