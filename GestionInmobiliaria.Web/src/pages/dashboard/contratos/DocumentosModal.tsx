import { useState, useEffect, useRef } from 'react'
import { Upload, Download, Trash2, FileText, AlertTriangle, X } from 'lucide-react'
import client from '../../../api/client'
import type { ApiResponse } from '../../../types/api'
import type { ContratoDto } from '../../../api/contratos'

interface DocumentoDto {
  id: number
  contratoId: number
  nombreOriginal: string
  tipoMime: string
  tamanoBytes: number
  descripcion: string | null
  fechaCreacion: string
}

function formatTamano(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function iconoMime(mime: string) {
  if (mime.startsWith('image/')) return '🖼️'
  if (mime === 'application/pdf') return '📄'
  if (mime.includes('word') || mime.includes('document')) return '📝'
  if (mime.includes('sheet') || mime.includes('excel')) return '📊'
  if (mime.includes('zip') || mime.includes('compressed')) return '📦'
  return '📎'
}

export default function DocumentosModal({ contrato, onCerrar }: {
  contrato: ContratoDto
  onCerrar: () => void
}) {
  const [documentos, setDocumentos] = useState<DocumentoDto[]>([])
  const [cargando, setCargando] = useState(true)
  const [subiendo, setSubiendo] = useState(false)
  const [descripcion, setDescripcion] = useState('')
  const [error, setError] = useState('')
  const [confirmDelete, setConfirmDelete] = useState<DocumentoDto | null>(null)
  const [eliminando, setEliminando] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const cargar = async () => {
    setCargando(true)
    try {
      const res = await client.get<ApiResponse<DocumentoDto[]>>(`/api/contratos/${contrato.id}/documentos`)
      if (res.data.success) setDocumentos(res.data.data ?? [])
    } catch { setError('No se pudieron cargar los documentos.') }
    finally { setCargando(false) }
  }

  useEffect(() => { cargar() }, [contrato.id])

  const handleSubir = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const archivo = e.target.files?.[0]
    if (!archivo) return
    if (archivo.size > 20 * 1024 * 1024) {
      setError('El archivo supera el tamaño máximo de 20 MB.')
      return
    }
    setSubiendo(true); setError('')
    try {
      const form = new FormData()
      form.append('archivo', archivo)
      if (descripcion.trim()) form.append('descripcion', descripcion.trim())
      await client.post(`/api/contratos/${contrato.id}/documentos`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      setDescripcion('')
      if (inputRef.current) inputRef.current.value = ''
      await cargar()
    } catch { setError('No se pudo subir el archivo.') }
    finally { setSubiendo(false) }
  }

  const handleDescargar = async (doc: DocumentoDto) => {
    try {
      const res = await client.get(`/api/contratos/${contrato.id}/documentos/${doc.id}`, {
        responseType: 'blob',
      })
      const url = URL.createObjectURL(res.data)
      const a = document.createElement('a')
      a.href = url
      a.download = doc.nombreOriginal
      a.click()
      URL.revokeObjectURL(url)
    } catch { setError('No se pudo descargar el archivo.') }
  }

  const handleEliminar = async () => {
    if (!confirmDelete) return
    setEliminando(true)
    try {
      await client.delete(`/api/contratos/${contrato.id}/documentos/${confirmDelete.id}`)
      setConfirmDelete(null)
      await cargar()
    } catch { setError('No se pudo eliminar el documento.') }
    finally { setEliminando(false) }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col">

        {/* Header */}
        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div>
            <h2 className="font-semibold text-gray-800">
              Documentos — <span className="font-mono text-blue-600">{contrato.codigo}</span>
            </h2>
            <p className="text-xs text-gray-400 mt-0.5">{contrato.propiedadDireccion}</p>
          </div>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600 text-xl leading-none"><X className="w-5 h-5" /></button>
        </div>

        {/* Upload */}
        <div className="px-6 py-4 border-b border-gray-100 space-y-2">
          <input
            type="text"
            placeholder="Descripción opcional (adenda, contrato, nota...)"
            value={descripcion}
            onChange={e => setDescripcion(e.target.value)}
            className="border border-gray-200 rounded-xl px-3 py-2 text-sm w-full outline-none focus:border-blue-400"
          />
          <div className="flex gap-2">
            <input ref={inputRef} type="file" className="hidden" onChange={handleSubir} />
            <button
              onClick={() => inputRef.current?.click()}
              disabled={subiendo}
              className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-xl text-sm font-medium hover:bg-blue-700 disabled:opacity-60 transition-colors w-full justify-center"
            >
              <Upload className="w-4 h-4" />
              {subiendo ? 'Subiendo...' : 'Seleccionar y subir archivo'}
            </button>
          </div>
          <p className="text-xs text-gray-400">Máximo 20 MB · cualquier tipo de archivo</p>
          {error && <p className="text-xs text-red-600">{error}</p>}
        </div>

        {/* Lista */}
        <div className="overflow-y-auto flex-1 px-6 py-4 space-y-2">
          {cargando ? (
            <p className="text-center text-gray-400 py-8 text-sm">Cargando...</p>
          ) : documentos.length === 0 ? (
            <div className="text-center py-10">
              <FileText className="w-10 h-10 text-gray-200 mx-auto mb-2" />
              <p className="text-sm text-gray-400">No hay documentos adjuntos.</p>
            </div>
          ) : (
            documentos.map(doc => (
              <div key={doc.id} className="flex items-center gap-3 border border-gray-100 rounded-xl px-4 py-3">
                <span className="text-2xl leading-none">{iconoMime(doc.tipoMime)}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-800 truncate">{doc.nombreOriginal}</p>
                  {doc.descripcion && <p className="text-xs text-gray-500 truncate">{doc.descripcion}</p>}
                  <p className="text-xs text-gray-400">{formatTamano(doc.tamanoBytes)} · {new Date(doc.fechaCreacion).toLocaleDateString('es-AR')}</p>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  <button onClick={() => handleDescargar(doc)} title="Descargar"
                    className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors">
                    <Download className="w-4 h-4" />
                  </button>
                  <button onClick={() => setConfirmDelete(doc)} title="Eliminar"
                    className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors">
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Confirm delete */}
      {confirmDelete && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-60 p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center shrink-0">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Eliminar documento</h3>
            </div>
            <p className="text-sm text-gray-600 mb-5">
              ¿Eliminás <strong>{confirmDelete.nombreOriginal}</strong>? Esta acción no se puede deshacer.
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmDelete(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50">
                Cancelar
              </button>
              <button onClick={handleEliminar} disabled={eliminando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60">
                {eliminando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
