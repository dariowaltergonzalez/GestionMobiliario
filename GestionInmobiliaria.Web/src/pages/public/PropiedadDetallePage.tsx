import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import {
  MapPin, Home, BedDouble, Bath, Maximize2, Car, Flame, PawPrint,
  ChevronLeft, ChevronRight, ArrowLeft, Phone, MessageCircle, Clock, Shield
} from 'lucide-react'
import { getPropiedadPublica, getPropiedadesPublicas, type PropiedadPublicaDto } from '../../api/propiedades'
import { getConfiguracionPublica, type ConfiguracionPublicaDto } from '../../api/configuracion'

const API_URL = 'http://localhost:5005'

function formatPrecio(p: PropiedadPublicaDto): { lineas: string[] } {
  const lineas: string[] = []
  if (p.precioAlquiler) lineas.push(`$${p.precioAlquiler.toLocaleString('es-AR')} / mes`)
  if (p.precioVenta) lineas.push(`USD ${p.precioVenta.toLocaleString('es-AR')}`)
  return { lineas: lineas.length ? lineas : ['Consultar precio'] }
}

function Galeria({ fotos, direccion }: { fotos: string[]; direccion: string }) {
  const [idx, setIdx] = useState(0)
  const [lightbox, setLightbox] = useState(false)

  if (fotos.length === 0) {
    return (
      <div className="w-full h-80 bg-gray-100 rounded-2xl flex items-center justify-center">
        <Home className="w-16 h-16 text-gray-300" />
      </div>
    )
  }

  const prev = () => setIdx(i => (i - 1 + fotos.length) % fotos.length)
  const next = () => setIdx(i => (i + 1) % fotos.length)

  return (
    <>
      {/* Foto principal */}
      <div className="relative rounded-2xl overflow-hidden bg-gray-100 aspect-[16/9] cursor-zoom-in" onClick={() => setLightbox(true)}>
        <img
          src={`${API_URL}${fotos[idx]}`}
          alt={`${direccion} - foto ${idx + 1}`}
          className="w-full h-full object-cover"
        />
        {fotos.length > 1 && (
          <>
            <button onClick={e => { e.stopPropagation(); prev() }}
              className="absolute left-3 top-1/2 -translate-y-1/2 bg-black/50 hover:bg-black/70 text-white p-2 rounded-full transition-colors">
              <ChevronLeft className="w-5 h-5" />
            </button>
            <button onClick={e => { e.stopPropagation(); next() }}
              className="absolute right-3 top-1/2 -translate-y-1/2 bg-black/50 hover:bg-black/70 text-white p-2 rounded-full transition-colors">
              <ChevronRight className="w-5 h-5" />
            </button>
            <span className="absolute bottom-3 right-3 bg-black/60 text-white text-xs px-2.5 py-1 rounded-full">
              {idx + 1} / {fotos.length}
            </span>
          </>
        )}
      </div>

      {/* Miniaturas */}
      {fotos.length > 1 && (
        <div className="flex gap-2 mt-3 overflow-x-auto pb-1">
          {fotos.map((url, i) => (
            <button key={i} onClick={() => setIdx(i)}
              className={`shrink-0 w-20 h-16 rounded-lg overflow-hidden border-2 transition-all ${i === idx ? 'border-blue-900 scale-105' : 'border-transparent opacity-60 hover:opacity-100'}`}>
              <img src={`${API_URL}${url}`} alt="" className="w-full h-full object-cover" />
            </button>
          ))}
        </div>
      )}

      {/* Lightbox */}
      {lightbox && (
        <div className="fixed inset-0 bg-black/90 z-50 flex items-center justify-center p-4" onClick={() => setLightbox(false)}>
          <button className="absolute top-4 right-4 text-white/70 hover:text-white p-2">
            <ChevronRight className="w-6 h-6 rotate-45" />
          </button>
          <button onClick={e => { e.stopPropagation(); prev() }}
            className="absolute left-4 top-1/2 -translate-y-1/2 bg-white/10 hover:bg-white/20 text-white p-3 rounded-full">
            <ChevronLeft className="w-6 h-6" />
          </button>
          <img
            src={`${API_URL}${fotos[idx]}`}
            alt=""
            className="max-w-full max-h-full object-contain rounded-xl"
            onClick={e => e.stopPropagation()}
          />
          <button onClick={e => { e.stopPropagation(); next() }}
            className="absolute right-4 top-1/2 -translate-y-1/2 bg-white/10 hover:bg-white/20 text-white p-3 rounded-full">
            <ChevronRight className="w-6 h-6" />
          </button>
          <span className="absolute bottom-6 left-1/2 -translate-x-1/2 text-white/60 text-sm">
            {idx + 1} / {fotos.length} · Hacé clic afuera para cerrar
          </span>
        </div>
      )}
    </>
  )
}

function VideoPlayer({ url }: { url: string }) {
  return (
    <div className="mt-8">
      <h2 className="text-lg font-bold text-blue-900 mb-3">Video de la propiedad</h2>
      <div className="rounded-2xl overflow-hidden bg-black aspect-[16/9]">
        <video
          src={`${API_URL}${url}`}
          controls
          className="w-full h-full"
          preload="metadata"
        >
          Tu navegador no soporta la reproducción de video.
        </video>
      </div>
    </div>
  )
}

function TagBadge({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col items-center bg-gray-50 rounded-xl p-3 text-center">
      <span className="text-xs text-gray-400 mb-1">{label}</span>
      <span className="font-semibold text-gray-800 text-sm">{value}</span>
    </div>
  )
}

export default function PropiedadDetallePage() {
  const { id } = useParams<{ id: string }>()
  const [propiedad, setPropiedad] = useState<PropiedadPublicaDto | null>(null)
  const [config, setConfig] = useState<ConfiguracionPublicaDto | null>(null)
  const [relacionadas, setRelacionadas] = useState<PropiedadPublicaDto[]>([])
  const [loading, setLoading] = useState(true)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    window.scrollTo(0, 0)
    setLoading(true)
    setNotFound(false)

    getConfiguracionPublica()
      .then(async res => {
        const cfg = res.success && res.data ? res.data : null
        setConfig(cfg)
        const slug = cfg?.tenantSlug ?? null

        const [detRes, listRes] = await Promise.all([
          getPropiedadPublica(Number(id), slug),
          getPropiedadesPublicas(slug),
        ])

        if (!detRes.success || !detRes.data) { setNotFound(true); return }
        setPropiedad(detRes.data)
        setRelacionadas(
          (listRes.data ?? [])
            .filter(p => p.id !== Number(id))
            .slice(0, 3)
        )
      })
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [id])

  if (loading) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <div className="w-8 h-8 border-4 border-blue-900 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (notFound || !propiedad) {
    return (
      <div className="min-h-screen bg-white flex flex-col items-center justify-center gap-4">
        <Home className="w-16 h-16 text-gray-300" />
        <h1 className="text-2xl font-bold text-gray-700">Propiedad no encontrada</h1>
        <Link to="/" className="text-blue-900 hover:underline flex items-center gap-1">
          <ArrowLeft className="w-4 h-4" /> Volver al inicio
        </Link>
      </div>
    )
  }

  const { lineas: precios } = formatPrecio(propiedad)
  const nombre = config?.nombreComercial || 'Inmobiliaria'
  const whatsapp = config?.whatsApp
    ? `https://wa.me/${config.whatsApp.replace(/\D/g, '')}?text=${encodeURIComponent(`Hola, me interesa la propiedad en ${propiedad.direccion}. ¿Podría darme más información?`)}`
    : '#'
  const telefono = config?.telefono || ''

  const esVenta = propiedad.operacionNombre === 'Venta' || propiedad.operacionNombre === 'AlquilerOVenta'

  return (
    <div className="min-h-screen bg-white font-sans">

      {/* NAVBAR */}
      <nav className="sticky top-0 z-40 bg-white/95 backdrop-blur border-b border-gray-100 shadow-sm">
        <div className="max-w-6xl mx-auto px-4 h-14 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-2 text-blue-900 hover:text-blue-700 transition-colors">
            <ArrowLeft className="w-4 h-4" />
            <span className="text-sm font-medium">{nombre}</span>
          </Link>
          <div className="flex items-center gap-2">
            <span className={`px-3 py-1 rounded-full text-xs font-bold ${esVenta ? 'bg-blue-900 text-white' : 'bg-yellow-400 text-blue-900'}`}>
              {propiedad.operacionNombre === 'AlquilerOVenta' ? 'Alq. / Venta' : propiedad.operacionNombre}
            </span>
            <span className="px-3 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
              {propiedad.tipoNombre}
            </span>
          </div>
        </div>
      </nav>

      <div className="max-w-6xl mx-auto px-4 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">

          {/* COLUMNA PRINCIPAL */}
          <div className="lg:col-span-2">

            {/* Galería */}
            <Galeria fotos={propiedad.fotosUrls} direccion={propiedad.direccion} />

            {/* Título y ubicación */}
            <div className="mt-6">
              <h1 className="text-2xl font-bold text-gray-800 mb-1">
                {propiedad.tipoNombre} en {propiedad.direccion}
                {propiedad.piso && `, piso ${propiedad.piso}`}
                {propiedad.numeroDepartamento && ` dpto. ${propiedad.numeroDepartamento}`}
              </h1>
              {(propiedad.barrio || propiedad.ciudad) && (
                <div className="flex items-center gap-1.5 text-gray-500 text-sm mt-1">
                  <MapPin className="w-4 h-4 text-gray-400" />
                  <span>{[propiedad.barrio, propiedad.ciudad, propiedad.provincia].filter(Boolean).join(', ')}</span>
                </div>
              )}
            </div>

            {/* Características en grid */}
            <div className="grid grid-cols-3 sm:grid-cols-4 gap-3 mt-6">
              {propiedad.ambientes && <TagBadge label="Ambientes" value={<span className="flex items-center gap-1"><Home className="w-3.5 h-3.5 text-blue-900" />{propiedad.ambientes}</span>} />}
              {propiedad.dormitorios && <TagBadge label="Dormitorios" value={<span className="flex items-center gap-1"><BedDouble className="w-3.5 h-3.5 text-blue-900" />{propiedad.dormitorios}</span>} />}
              {propiedad.banios && <TagBadge label="Baños" value={<span className="flex items-center gap-1"><Bath className="w-3.5 h-3.5 text-blue-900" />{propiedad.banios}</span>} />}
              {propiedad.superficieTotal && <TagBadge label="Sup. total" value={`${propiedad.superficieTotal} m²`} />}
              {propiedad.superficieCubierta && <TagBadge label="Sup. cubierta" value={`${propiedad.superficieCubierta} m²`} />}
              {propiedad.antiguedad != null && <TagBadge label="Antigüedad" value={propiedad.antiguedad === 0 ? 'A estrenar' : `${propiedad.antiguedad} años`} />}
              {propiedad.estadoConservacionNombre && <TagBadge label="Conservación" value={propiedad.estadoConservacionNombre} />}
            </div>

            {/* Extras */}
            {(propiedad.cochera || propiedad.tieneCalefaccion || propiedad.aceptaMascotas) && (
              <div className="flex flex-wrap gap-2 mt-4">
                {propiedad.cochera && (
                  <span className="flex items-center gap-1.5 bg-blue-50 text-blue-800 text-xs font-medium px-3 py-1.5 rounded-full">
                    <Car className="w-3.5 h-3.5" /> Cochera
                  </span>
                )}
                {propiedad.tieneCalefaccion && (
                  <span className="flex items-center gap-1.5 bg-orange-50 text-orange-700 text-xs font-medium px-3 py-1.5 rounded-full">
                    <Flame className="w-3.5 h-3.5" /> Calefacción
                  </span>
                )}
                {propiedad.aceptaMascotas && (
                  <span className="flex items-center gap-1.5 bg-green-50 text-green-700 text-xs font-medium px-3 py-1.5 rounded-full">
                    <PawPrint className="w-3.5 h-3.5" /> Acepta mascotas
                  </span>
                )}
              </div>
            )}

            {/* Descripción */}
            {propiedad.descripcion && (
              <div className="mt-8">
                <h2 className="text-lg font-bold text-blue-900 mb-3">Descripción</h2>
                <p className="text-gray-600 leading-relaxed whitespace-pre-line">{propiedad.descripcion}</p>
              </div>
            )}

            {/* Video */}
            {propiedad.videoUrl && <VideoPlayer url={propiedad.videoUrl} />}

          </div>

          {/* COLUMNA LATERAL */}
          <div className="lg:col-span-1">
            <div className="sticky top-20 space-y-4">

              {/* Precio */}
              <div className="bg-blue-900 rounded-2xl p-6 text-white">
                <p className="text-blue-300 text-xs uppercase tracking-wider mb-2">Precio</p>
                {precios.map((p, i) => (
                  <p key={i} className="text-2xl font-bold leading-tight">{p}</p>
                ))}
                {propiedad.expensas && propiedad.expensas > 0 && (
                  <p className="text-blue-300 text-sm mt-2">
                    + Expensas ${propiedad.expensas.toLocaleString('es-AR')}
                  </p>
                )}
              </div>

              {/* Contacto */}
              <div className="bg-white border border-gray-200 rounded-2xl p-5 space-y-3">
                <h3 className="font-semibold text-gray-800 text-sm">¿Te interesa esta propiedad?</h3>

                <a
                  href={whatsapp}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 w-full bg-green-500 hover:bg-green-600 text-white font-semibold py-3 rounded-xl transition-colors text-sm"
                >
                  <MessageCircle className="w-4 h-4" />
                  Consultar por WhatsApp
                </a>

                {telefono && (
                  <a
                    href={`tel:${telefono.replace(/\s/g, '')}`}
                    className="flex items-center justify-center gap-2 w-full border border-blue-900 text-blue-900 hover:bg-blue-50 font-semibold py-3 rounded-xl transition-colors text-sm"
                  >
                    <Phone className="w-4 h-4" />
                    Llamar ahora
                  </a>
                )}

                <div className="flex flex-col gap-2 pt-2 border-t border-gray-100">
                  <div className="flex items-center gap-2 text-xs text-gray-400">
                    <Clock className="w-3.5 h-3.5" /> Respondemos en menos de 1 hora
                  </div>
                  <div className="flex items-center gap-2 text-xs text-gray-400">
                    <Shield className="w-3.5 h-3.5" /> Operación segura y transparente
                  </div>
                </div>
              </div>

            </div>
          </div>
        </div>

        {/* PROPIEDADES RELACIONADAS */}
        {relacionadas.length > 0 && (
          <div className="mt-16">
            <h2 className="text-xl font-bold text-blue-900 mb-6">Otras propiedades disponibles</h2>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
              {relacionadas.map(p => {
                const imgSrc = p.fotoPrincipalUrl ? `${API_URL}${p.fotoPrincipalUrl}` : null
                return (
                  <Link key={p.id} to={`/propiedades/${p.id}`}
                    className="bg-white rounded-2xl overflow-hidden shadow-sm border border-gray-100 hover:shadow-md transition-shadow group">
                    <div className="relative h-44 bg-gray-100 overflow-hidden">
                      {imgSrc
                        ? <img src={imgSrc} alt={p.direccion} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
                        : <div className="w-full h-full flex items-center justify-center"><Home className="w-10 h-10 text-gray-300" /></div>
                      }
                      <span className={`absolute top-2 left-2 px-2 py-0.5 rounded-full text-xs font-bold ${p.operacionNombre === 'Venta' || p.operacionNombre === 'AlquilerOVenta' ? 'bg-blue-900 text-white' : 'bg-yellow-400 text-blue-900'}`}>
                        {p.operacionNombre === 'AlquilerOVenta' ? 'Alq./Venta' : p.operacionNombre}
                      </span>
                    </div>
                    <div className="p-4">
                      <p className="font-semibold text-gray-800 text-sm truncate">{p.direccion}</p>
                      {p.barrio && <p className="text-xs text-gray-400 mt-0.5">{p.barrio}</p>}
                      <p className="text-blue-900 font-bold text-sm mt-2">{formatPrecio(p).lineas[0]}</p>
                    </div>
                  </Link>
                )
              })}
            </div>
          </div>
        )}
      </div>

      {/* WhatsApp flotante */}
      <a href={whatsapp} target="_blank" rel="noopener noreferrer"
        className="fixed bottom-6 right-6 z-50 w-14 h-14 bg-green-500 hover:bg-green-600 rounded-full shadow-lg flex items-center justify-center transition-transform hover:scale-110"
        title="Contactar por WhatsApp">
        <svg className="w-7 h-7 text-white" fill="currentColor" viewBox="0 0 24 24">
          <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
        </svg>
      </a>
    </div>
  )
}
