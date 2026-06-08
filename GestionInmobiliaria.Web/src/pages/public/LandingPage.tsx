import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { MapPin, Home, Search, Phone, Mail, ChevronRight, Building2, TrendingUp, Users, Maximize2, BedDouble, Bath } from 'lucide-react'
import { getConfiguracionPublica, type ConfiguracionPublicaDto } from '../../api/configuracion'
import { getPropiedadesPublicas, type PropiedadPublicaDto } from '../../api/propiedades'

const API_URL = 'http://localhost:5005'

const stats = [
  { icono: Building2, valor: '500+', label: 'Propiedades' },
  { icono: Users, valor: '1.200+', label: 'Clientes satisfechos' },
  { icono: TrendingUp, valor: '15 años', label: 'De experiencia' },
]

function formatPrecio(p: PropiedadPublicaDto): string {
  if (p.precioAlquiler && p.precioVenta)
    return `Alq. $${p.precioAlquiler.toLocaleString('es-AR')} · Vta. USD ${p.precioVenta.toLocaleString('es-AR')}`
  if (p.precioAlquiler) return `$${p.precioAlquiler.toLocaleString('es-AR')} / mes`
  if (p.precioVenta) return `USD ${p.precioVenta.toLocaleString('es-AR')}`
  return 'Consultar precio'
}

function Logo({ config, className = '' }: { config: ConfiguracionPublicaDto | null; className?: string }) {
  if (config?.logoUrl) {
    return (
      <img
        src={config.logoUrl}
        alt={config.nombreComercial}
        className={`h-11 w-auto object-contain ${className}`}
        onError={e => { e.currentTarget.style.display = 'none' }}
      />
    )
  }
  return (
    <div className="w-8 h-8 bg-blue-900 rounded-lg flex items-center justify-center shrink-0">
      <Home className="w-4 h-4 text-yellow-400" />
    </div>
  )
}

export default function LandingPage() {
  const [busqueda, setBusqueda] = useState('')
  const [tipo, setTipo] = useState('todos')
  const [config, setConfig] = useState<ConfiguracionPublicaDto | null>(null)
  const [propiedades, setPropiedades] = useState<PropiedadPublicaDto[]>([])

  useEffect(() => {
    getConfiguracionPublica()
      .then(res => {
        if (res.success && res.data) {
          setConfig(res.data)
          return getPropiedadesPublicas(res.data.tenantSlug)
        }
      })
      .then(res => { if (res?.success && res.data) setPropiedades(res.data) })
      .catch(() => { /* usa defaults si falla */ })
  }, [])

  const nombre = config?.nombreComercial || 'Inmobiliaria'
  const slogan = config?.slogan || 'Alquiler, venta y tasaciones online.'
  const telefono = config?.telefono || '+54 11 1234-5678'
  const whatsapp = config?.whatsApp ? `https://wa.me/${config.whatsApp.replace(/\D/g, '')}` : 'https://wa.me/5491112345678'
  const email = config?.email || 'info@garciapropiedades.com.ar'

  return (
    <div className="min-h-screen bg-white font-sans">

      {/* NAVBAR */}
      <nav className="fixed top-0 left-0 right-0 z-50 bg-white/95 backdrop-blur border-b border-gray-100 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center gap-2">
              <Logo config={config} />
              {!config?.logoUrl && (
                <span className="text-xl font-bold text-blue-900">{nombre}</span>
              )}
              {config?.logoUrl && (
                <span className="text-xl font-bold text-blue-900">{nombre}</span>
              )}
            </div>
            <div className="hidden md:flex items-center gap-8 text-sm text-gray-600 font-medium">
              <a href="#propiedades" className="hover:text-blue-900 transition-colors">Propiedades</a>
              <a href="#tasacion" className="hover:text-blue-900 transition-colors">Tasaciones</a>
              <a href="#contacto" className="hover:text-blue-900 transition-colors">Contacto</a>
            </div>
            <Link
              to="/login"
              className="flex items-center gap-2 bg-blue-900 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-800 transition-colors"
            >
              Ingresar
              <ChevronRight className="w-4 h-4" />
            </Link>
          </div>
        </div>
      </nav>

      {/* HERO */}
      <section className="relative min-h-screen flex items-center pt-16">
        <div
          className="absolute inset-0 bg-cover bg-center bg-no-repeat"
          style={{ backgroundImage: "url('https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=1600&q=90')" }}
        >
          <div className="absolute inset-0 bg-gradient-to-r from-blue-950/90 via-blue-900/75 to-blue-900/40" />
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-24">
          <div className="max-w-2xl">
            <span className="inline-block bg-yellow-400 text-blue-900 text-xs font-bold uppercase tracking-widest px-3 py-1 rounded-full mb-6">
              Tu inmobiliaria de confianza
            </span>

            {/* Logo grande en el hero si existe */}
            {config?.logoUrl && (
              <div className="mb-8">
                <img
                  src={config.logoUrl}
                  alt={nombre}
                  className="h-32 w-auto object-contain drop-shadow-2xl"
                  onError={e => { e.currentTarget.style.display = 'none' }}
                />
              </div>
            )}

            <h1 className="text-5xl md:text-6xl font-bold text-white leading-tight mb-6">
              Encontrá tu<br />
              <span className="text-yellow-400">próximo hogar</span>
            </h1>
            <p className="text-xl text-blue-100 mb-10 leading-relaxed">
              {slogan}
            </p>

            {/* BUSCADOR */}
            <div className="bg-white rounded-2xl p-4 shadow-2xl">
              <div className="flex gap-2 mb-3">
                {['todos', 'venta', 'alquiler'].map((t) => (
                  <button
                    key={t}
                    onClick={() => setTipo(t)}
                    className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-colors capitalize ${
                      tipo === t
                        ? 'bg-blue-900 text-white'
                        : 'text-gray-500 hover:bg-gray-100'
                    }`}
                  >
                    {t === 'todos' ? 'Todos' : t.charAt(0).toUpperCase() + t.slice(1)}
                  </button>
                ))}
              </div>
              <div className="flex gap-2">
                <div className="flex-1 flex items-center gap-2 border border-gray-200 rounded-xl px-3">
                  <Search className="w-4 h-4 text-gray-400 shrink-0" />
                  <input
                    type="text"
                    placeholder="Barrio, zona o dirección..."
                    value={busqueda}
                    onChange={(e) => setBusqueda(e.target.value)}
                    className="w-full py-3 text-sm text-gray-700 outline-none placeholder-gray-400"
                  />
                </div>
                <button className="bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-bold px-6 py-3 rounded-xl transition-colors text-sm">
                  Buscar
                </button>
              </div>
            </div>
          </div>
        </div>

        <div className="absolute bottom-8 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2 text-white/60 text-xs">
          <span>Ver más</span>
          <div className="w-px h-8 bg-white/30 animate-pulse" />
        </div>
      </section>

      {/* STATS */}
      <section className="bg-blue-900 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
            {stats.map(({ icono: Icon, valor, label }) => (
              <div key={label}>
                <Icon className="w-6 h-6 text-yellow-400 mx-auto mb-2" />
                <div className="text-3xl font-bold text-white">{valor}</div>
                <div className="text-blue-300 text-sm mt-1">{label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* PROPIEDADES DESTACADAS */}
      <section id="propiedades" className="py-20 bg-gray-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <span className="text-yellow-600 font-semibold text-sm uppercase tracking-wider">Disponibles ahora</span>
            <h2 className="text-3xl font-bold text-blue-900 mt-2">Propiedades disponibles</h2>
          </div>

          {propiedades.length === 0 ? (
            <p className="text-center text-gray-400 py-10">No hay propiedades disponibles en este momento.</p>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              {propiedades
                .filter(p => {
                  if (tipo === 'venta') return p.operacionNombre === 'Venta' || p.operacionNombre === 'AlquilerOVenta'
                  if (tipo === 'alquiler') return p.operacionNombre === 'Alquiler' || p.operacionNombre === 'AlquilerOVenta'
                  return true
                })
                .filter(p => {
                  if (!busqueda) return true
                  const q = busqueda.toLowerCase()
                  return p.direccion.toLowerCase().includes(q)
                    || p.barrio?.toLowerCase().includes(q)
                    || p.ciudad?.toLowerCase().includes(q)
                })
                .slice(0, 9)
                .map((p) => {
                  const imgSrc = p.fotoPrincipalUrl ? `${API_URL}${p.fotoPrincipalUrl}` : null
                  const esVenta = p.operacionNombre === 'Venta' || p.operacionNombre === 'AlquilerOVenta'
                  return (
                    <Link key={p.id} to={`/propiedades/${p.id}`} className="bg-white rounded-2xl overflow-hidden shadow-md hover:shadow-xl transition-shadow group cursor-pointer block">
                      <div className="relative overflow-hidden h-52 bg-gray-100">
                        {imgSrc ? (
                          <img
                            src={imgSrc}
                            alt={`${p.tipoNombre} en ${p.direccion}`}
                            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                          />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center">
                            <Home className="w-12 h-12 text-gray-300" />
                          </div>
                        )}
                        <span className={`absolute top-3 left-3 px-3 py-1 rounded-full text-xs font-bold ${
                          esVenta ? 'bg-blue-900 text-white' : 'bg-yellow-400 text-blue-900'
                        }`}>
                          {p.operacionNombre === 'AlquilerOVenta' ? 'Alq. / Venta' : p.operacionNombre}
                        </span>
                        <span className="absolute top-3 right-3 bg-white/90 text-gray-700 px-2 py-1 rounded-lg text-xs font-medium">
                          {p.tipoNombre}
                        </span>
                      </div>
                      <div className="p-5">
                        <div className="flex items-start gap-1 text-gray-700 font-semibold mb-1 leading-snug">
                          {p.direccion}
                        </div>
                        {(p.barrio || p.ciudad) && (
                          <div className="flex items-center gap-1 text-gray-400 text-sm mb-3">
                            <MapPin className="w-3.5 h-3.5 shrink-0" />
                            <span>{[p.barrio, p.ciudad].filter(Boolean).join(', ')}</span>
                          </div>
                        )}
                        <div className="flex items-center gap-3 text-xs text-gray-500 mb-4 flex-wrap">
                          {p.ambientes && (
                            <span className="flex items-center gap-1">
                              <Home className="w-3.5 h-3.5" /> {p.ambientes} amb.
                            </span>
                          )}
                          {p.dormitorios && (
                            <span className="flex items-center gap-1">
                              <BedDouble className="w-3.5 h-3.5" /> {p.dormitorios} dorm.
                            </span>
                          )}
                          {p.banios && (
                            <span className="flex items-center gap-1">
                              <Bath className="w-3.5 h-3.5" /> {p.banios} baño(s)
                            </span>
                          )}
                          {p.superficieTotal && (
                            <span className="flex items-center gap-1">
                              <Maximize2 className="w-3.5 h-3.5" /> {p.superficieTotal} m²
                            </span>
                          )}
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-base font-bold text-blue-900">{formatPrecio(p)}</span>
                          <span className="text-yellow-600 text-sm font-medium flex items-center gap-1">
                            Ver más <ChevronRight className="w-4 h-4" />
                          </span>
                        </div>
                      </div>
                    </Link>
                  )
                })}
            </div>
          )}
        </div>
      </section>

      {/* TASACIÓN CTA */}
      <section id="tasacion" className="py-20 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-gradient-to-br from-blue-900 to-blue-800 rounded-3xl overflow-hidden">
            <div className="grid md:grid-cols-2 gap-0">
              <div className="p-12 flex flex-col justify-center">
                <span className="text-yellow-400 font-semibold text-sm uppercase tracking-wider mb-4">Servicio gratuito</span>
                <h2 className="text-3xl font-bold text-white mb-4 leading-tight">
                  ¿Querés saber cuánto<br />vale tu propiedad?
                </h2>
                <p className="text-blue-200 mb-8 leading-relaxed">
                  Completá el formulario con los datos de tu propiedad y un agente especializado
                  te contactará para coordinar una tasación presencial u online.
                </p>
                <div className="flex flex-col sm:flex-row gap-3">
                  <Link
                    to="/tasacion"
                    className="inline-flex items-center justify-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-blue-900 font-bold px-8 py-4 rounded-xl transition-colors"
                  >
                    Solicitar tasación gratuita
                    <ChevronRight className="w-5 h-5" />
                  </Link>
                  <a
                    href={whatsapp}
                    className="inline-flex items-center justify-center gap-2 border-2 border-white/30 text-white hover:border-white px-8 py-4 rounded-xl transition-colors text-sm font-medium"
                  >
                    <Phone className="w-4 h-4" />
                    Llamar ahora
                  </a>
                </div>
              </div>
              <div className="hidden md:block relative">
                <img
                  src="https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&q=80"
                  alt="Tasación de propiedades"
                  className="w-full h-full object-cover"
                />
                <div className="absolute inset-0 bg-blue-900/20" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CONTACTO */}
      <section id="contacto" className="py-20 bg-gray-50">
        <div className="max-w-3xl mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold text-blue-900 mb-4">¿Necesitás ayuda?</h2>
          <p className="text-gray-500 mb-10">Nuestro equipo está disponible de lunes a sábados de 9 a 18 hs.</p>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
            <a href={`tel:${telefono.replace(/\s/g, '')}`} className="flex items-center gap-4 bg-white p-6 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center shrink-0">
                <Phone className="w-5 h-5 text-blue-900" />
              </div>
              <div className="text-left">
                <div className="text-xs text-gray-400 mb-1">Teléfono</div>
                <div className="font-semibold text-gray-800">{telefono}</div>
              </div>
            </a>
            <a href={whatsapp} target="_blank" rel="noopener noreferrer" className="flex items-center gap-4 bg-white p-6 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-green-100 rounded-xl flex items-center justify-center shrink-0">
                <svg className="w-5 h-5 text-green-600" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/>
                </svg>
              </div>
              <div className="text-left">
                <div className="text-xs text-gray-400 mb-1">WhatsApp</div>
                <div className="font-semibold text-gray-800">{config?.whatsApp || telefono}</div>
              </div>
            </a>
            <a href={`mailto:${email}`} className="flex items-center gap-4 bg-white p-6 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-yellow-100 rounded-xl flex items-center justify-center shrink-0">
                <Mail className="w-5 h-5 text-yellow-600" />
              </div>
              <div className="text-left">
                <div className="text-xs text-gray-400 mb-1">Email</div>
                <div className="font-semibold text-gray-800">{email}</div>
              </div>
            </a>
          </div>
        </div>
      </section>

      {/* BOTÓN FLOTANTE WHATSAPP */}
      <a
        href={whatsapp}
        target="_blank"
        rel="noopener noreferrer"
        className="fixed bottom-6 right-6 z-50 w-14 h-14 bg-green-500 hover:bg-green-600 rounded-full shadow-lg flex items-center justify-center transition-transform hover:scale-110"
        title="Contactar por WhatsApp"
      >
        <svg className="w-7 h-7 text-white" fill="currentColor" viewBox="0 0 24 24">
          <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/>
        </svg>
      </a>

      {/* FOOTER */}
      <footer className="bg-blue-950 text-blue-300 py-10">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-6">

            {/* Logo y nombre */}
            <div className="flex items-center gap-2">
              {config?.logoUrl ? (
                <img
                  src={config.logoUrl}
                  alt={nombre}
                  className="h-6 w-auto object-contain opacity-90"
                  onError={e => { e.currentTarget.style.display = 'none' }}
                />
              ) : (
                <div className="w-6 h-6 bg-yellow-400 rounded flex items-center justify-center">
                  <Home className="w-3 h-3 text-blue-900" />
                </div>
              )}
              <span className="text-white font-semibold text-sm">{nombre}</span>
            </div>

            {/* Redes sociales */}
            {(config?.instagram || config?.facebook || config?.twitter) && (
              <div className="flex items-center gap-3">
                {config.instagram && (
                  <a href={config.instagram.startsWith('http') ? config.instagram : `https://instagram.com/${config.instagram.replace('@', '')}`}
                    target="_blank" rel="noopener noreferrer"
                    className="w-9 h-9 rounded-full bg-blue-900 hover:bg-pink-600 flex items-center justify-center transition-colors">
                    <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 24 24"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2.163c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/></svg>
                  </a>
                )}
                {config.facebook && (
                  <a href={config.facebook.startsWith('http') ? config.facebook : `https://facebook.com/${config.facebook}`}
                    target="_blank" rel="noopener noreferrer"
                    className="w-9 h-9 rounded-full bg-blue-900 hover:bg-blue-600 flex items-center justify-center transition-colors">
                    <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 24 24"><path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/></svg>
                  </a>
                )}
                {config.twitter && (
                  <a href={config.twitter.startsWith('http') ? config.twitter : `https://twitter.com/${config.twitter.replace('@', '')}`}
                    target="_blank" rel="noopener noreferrer"
                    className="w-9 h-9 rounded-full bg-blue-900 hover:bg-sky-500 flex items-center justify-center transition-colors">
                    <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 24 24"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.746l7.73-8.835L1.254 2.25H8.08l4.253 5.622zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>
                  </a>
                )}
              </div>
            )}

            {/* Copyright */}
            <span className="text-xs">© {new Date().getFullYear()} Todos los derechos reservados.</span>
          </div>
        </div>
      </footer>

    </div>
  )
}
