import { useState, useEffect, useRef } from 'react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getClausulas, getPlaceholders, createClausula, updateClausula, deleteClausula, moverClausula,
  inicializarClausulas, type ClausulaContratoDto, type PlaceholderGrupo
} from '../../../api/clausulasContrato'
import { Pencil, Trash2, ChevronUp, ChevronDown, Plus, Eye, EyeOff, RefreshCw, Braces } from 'lucide-react'

interface FormState {
  numero: string
  titulo: string
  texto: string
  activo: boolean
}

const FORM_INICIAL: FormState = { numero: '', titulo: '', texto: '', activo: true }

export default function ClausulasContratoPage() {
  const [clausulas, setClausulas] = useState<ClausulaContratoDto[]>([])
  const [grupos, setGrupos] = useState<PlaceholderGrupo[]>([])
  const [loading, setLoading] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [form, setForm] = useState<FormState>(FORM_INICIAL)
  const [saving, setSaving] = useState(false)
  const [inicializando, setInicializando] = useState(false)
  const [error, setError] = useState('')

  // Variable picker state
  const [pickerOpen, setPickerOpen] = useState(false)
  const [grupoActivo, setGrupoActivo] = useState<string>('')
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const cursorPosRef = useRef<number>(0)

  const cargar = async () => {
    try {
      setLoading(true)
      const [lista, ph] = await Promise.all([getClausulas(), getPlaceholders()])
      setClausulas(lista)
      setGrupos(ph)
      if (ph.length > 0) setGrupoActivo(ph[0].entidad)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { cargar() }, [])

  const abrirCrear = () => {
    setEditandoId(null)
    setForm(FORM_INICIAL)
    setError('')
    setPickerOpen(false)
    setModalOpen(true)
  }

  const abrirEditar = (c: ClausulaContratoDto) => {
    setEditandoId(c.id)
    setForm({ numero: c.numero, titulo: c.titulo, texto: c.texto, activo: c.activo })
    setError('')
    setPickerOpen(false)
    setModalOpen(true)
  }

  const guardar = async () => {
    if (!form.numero.trim() || !form.titulo.trim() || !form.texto.trim()) {
      setError('Número, título y texto son obligatorios.')
      return
    }
    setSaving(true)
    setError('')
    try {
      if (editandoId) {
        await updateClausula(editandoId, form)
      } else {
        await createClausula({ numero: form.numero, titulo: form.titulo, texto: form.texto })
      }
      setModalOpen(false)
      await cargar()
    } catch {
      setError('Error al guardar la cláusula.')
    } finally {
      setSaving(false)
    }
  }

  const toggleActivo = async (c: ClausulaContratoDto) => {
    await updateClausula(c.id, { numero: c.numero, titulo: c.titulo, texto: c.texto, activo: !c.activo })
    await cargar()
  }

  const eliminar = async (id: number) => {
    if (!confirm('¿Eliminar esta cláusula?')) return
    await deleteClausula(id)
    await cargar()
  }

  const mover = async (id: number, subir: boolean) => {
    await moverClausula(id, subir)
    await cargar()
  }

  const inicializar = async () => {
    setInicializando(true)
    try {
      const lista = await inicializarClausulas()
      setClausulas(lista)
    } finally {
      setInicializando(false)
    }
  }

  // Guardar posición del cursor antes de abrir el picker
  const onTextareaBlur = () => {
    if (textareaRef.current) {
      cursorPosRef.current = textareaRef.current.selectionStart
    }
  }

  const insertarVariable = (clave: string) => {
    const ta = textareaRef.current
    if (!ta) return

    const pos = cursorPosRef.current
    const texto = form.texto
    const nuevoTexto = texto.slice(0, pos) + clave + texto.slice(pos)
    const nuevaPos = pos + clave.length

    setForm(f => ({ ...f, texto: nuevoTexto }))

    // Restaurar foco y cursor después del render
    requestAnimationFrame(() => {
      ta.focus()
      ta.setSelectionRange(nuevaPos, nuevaPos)
      cursorPosRef.current = nuevaPos
    })
  }

  const grupoSeleccionado = grupos.find(g => g.entidad === grupoActivo)

  return (
    <DashboardLayout titulo="Plantilla de Contrato">
      <div className="space-y-6">

        {/* Header */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-gray-500">
            Administrá las cláusulas del contrato de locación. Los cambios se aplican al generar el PDF.
          </p>
          <button
            onClick={abrirCrear}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-xl text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            <Plus className="w-4 h-4" />
            Nueva cláusula
          </button>
        </div>

        {/* Tabla */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
          {loading ? (
            <div className="p-12 text-center text-gray-400 text-sm">Cargando cláusulas...</div>
          ) : clausulas.length === 0 ? (
            <div className="p-12 text-center space-y-4">
              <p className="text-gray-400 text-sm">No hay cláusulas configuradas.</p>
              <button
                onClick={inicializar}
                disabled={inicializando}
                className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-xl text-sm font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                <RefreshCw className={`w-4 h-4 ${inicializando ? 'animate-spin' : ''}`} />
                {inicializando ? 'Cargando...' : 'Cargar cláusulas predeterminadas (Ley 27551)'}
              </button>
            </div>
          ) : (
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50">
                  <th className="text-left text-xs font-semibold text-gray-500 uppercase tracking-wide px-4 py-3 w-8">#</th>
                  <th className="text-left text-xs font-semibold text-gray-500 uppercase tracking-wide px-4 py-3 w-36">Número</th>
                  <th className="text-left text-xs font-semibold text-gray-500 uppercase tracking-wide px-4 py-3">Título</th>
                  <th className="text-left text-xs font-semibold text-gray-500 uppercase tracking-wide px-4 py-3 w-20">Estado</th>
                  <th className="text-right text-xs font-semibold text-gray-500 uppercase tracking-wide px-4 py-3 w-36">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {clausulas.map((c, i) => (
                  <tr key={c.id} className={`hover:bg-gray-50 transition-colors ${!c.activo ? 'opacity-50' : ''}`}>
                    <td className="px-4 py-3 text-sm text-gray-400">{c.orden}</td>
                    <td className="px-4 py-3 text-sm font-medium text-gray-700">{c.numero}</td>
                    <td className="px-4 py-3">
                      <div className="text-sm font-medium text-gray-800">{c.titulo}</div>
                      <div className="text-xs text-gray-400 mt-0.5 line-clamp-1">{c.texto.replace(/\{[^}]+\}/g, '…')}</div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                        c.activo ? 'bg-green-50 text-green-700' : 'bg-gray-100 text-gray-500'
                      }`}>
                        {c.activo ? 'Activa' : 'Inactiva'}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => mover(c.id, true)}
                          disabled={i === 0}
                          className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-30 cursor-pointer"
                          title="Subir"
                        >
                          <ChevronUp className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => mover(c.id, false)}
                          disabled={i === clausulas.length - 1}
                          className="p-1.5 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-30 cursor-pointer"
                          title="Bajar"
                        >
                          <ChevronDown className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => toggleActivo(c)}
                          className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                          title={c.activo ? 'Desactivar' : 'Activar'}
                        >
                          {c.activo ? <Eye className="w-4 h-4" /> : <EyeOff className="w-4 h-4" />}
                        </button>
                        <button
                          onClick={() => abrirEditar(c)}
                          className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors cursor-pointer"
                          title="Editar"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => eliminar(c.id)}
                          className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                          title="Eliminar"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <h2 className="text-lg font-bold text-gray-800">
                {editandoId ? 'Editar cláusula' : 'Nueva cláusula'}
              </h2>
              <button onClick={() => setModalOpen(false)} className="text-gray-400 hover:text-gray-600 text-2xl leading-none cursor-pointer">×</button>
            </div>

            <div className="overflow-y-auto flex-1 p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Número *</label>
                  <input
                    value={form.numero}
                    onChange={e => setForm(f => ({ ...f, numero: e.target.value }))}
                    placeholder="ej: VIGÉSIMA SEXTA"
                    className="w-full border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-gray-600 mb-1">Título *</label>
                  <input
                    value={form.titulo}
                    onChange={e => setForm(f => ({ ...f, titulo: e.target.value }))}
                    placeholder="ej: GARANTÍA ADICIONAL"
                    className="w-full border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              {/* Texto con botón para abrir picker */}
              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className="block text-xs font-semibold text-gray-600">Texto *</label>
                  <button
                    type="button"
                    onClick={() => setPickerOpen(p => !p)}
                    className={`flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-lg border transition-colors ${
                      pickerOpen
                        ? 'bg-blue-600 text-white border-blue-600'
                        : 'text-blue-600 border-blue-200 hover:bg-blue-50'
                    }`}
                  >
                    <Braces className="w-3.5 h-3.5" />
                    {pickerOpen ? 'Cerrar variables' : 'Insertar variable'}
                  </button>
                </div>

                <textarea
                  ref={textareaRef}
                  value={form.texto}
                  onChange={e => setForm(f => ({ ...f, texto: e.target.value }))}
                  onBlur={onTextareaBlur}
                  onClick={() => { if (textareaRef.current) cursorPosRef.current = textareaRef.current.selectionStart }}
                  onKeyUp={() => { if (textareaRef.current) cursorPosRef.current = textareaRef.current.selectionStart }}
                  rows={pickerOpen ? 5 : 8}
                  placeholder="Texto de la cláusula. Hacé clic en 'Insertar variable' para agregar datos del contrato."
                  className="w-full border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-y font-mono"
                />

                {/* Selector de variables */}
                {pickerOpen && grupos.length > 0 && (
                  <div className="mt-2 border border-blue-100 rounded-xl overflow-hidden bg-blue-50">
                    {/* Tabs de entidad */}
                    <div className="flex overflow-x-auto border-b border-blue-100 bg-white">
                      {grupos.map(g => (
                        <button
                          key={g.entidad}
                          type="button"
                          onClick={() => setGrupoActivo(g.entidad)}
                          className={`shrink-0 px-3 py-2 text-xs font-medium transition-colors border-b-2 ${
                            grupoActivo === g.entidad
                              ? 'border-blue-600 text-blue-700 bg-blue-50'
                              : 'border-transparent text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                          }`}
                        >
                          {g.etiqueta}
                        </button>
                      ))}
                    </div>

                    {/* Campos del grupo seleccionado */}
                    {grupoSeleccionado && (
                      <div className="p-2 grid grid-cols-1 gap-1 max-h-40 overflow-y-auto">
                        {grupoSeleccionado.campos.map(campo => (
                          <button
                            key={campo.clave}
                            type="button"
                            onClick={() => insertarVariable(campo.clave)}
                            className="flex items-center gap-2 w-full text-left px-2 py-1.5 rounded-lg hover:bg-white hover:shadow-sm transition-all group"
                          >
                            <code className="font-mono text-blue-700 bg-white border border-blue-100 px-1.5 py-0.5 rounded text-xs shrink-0 group-hover:border-blue-300 transition-colors">
                              {campo.clave}
                            </code>
                            <span className="text-xs text-gray-500">{campo.descripcion}</span>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>

              {editandoId && (
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="activo"
                    checked={form.activo}
                    onChange={e => setForm(f => ({ ...f, activo: e.target.checked }))}
                    className="w-4 h-4 rounded accent-blue-600"
                  />
                  <label htmlFor="activo" className="text-sm text-gray-700">Cláusula activa (incluida en el PDF)</label>
                </div>
              )}

              {error && <p className="text-sm text-red-600">{error}</p>}
            </div>

            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-100">
              <button
                onClick={() => setModalOpen(false)}
                className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 border border-gray-200 rounded-xl hover:bg-gray-50 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={guardar}
                disabled={saving}
                className="px-5 py-2 text-sm font-medium bg-blue-600 text-white rounded-xl hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {saving ? 'Guardando...' : editandoId ? 'Guardar cambios' : 'Crear cláusula'}
              </button>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  )
}
