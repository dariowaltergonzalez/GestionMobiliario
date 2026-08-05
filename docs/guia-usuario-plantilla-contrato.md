# Guía de usuario — Plantilla de Contrato

Esta pantalla permite administrar las cláusulas que se incluyen en el contrato de locación PDF. Los cambios que hagas acá se aplican automáticamente la próxima vez que generes un contrato.

---

## La pantalla principal

![Listado de cláusulas](../screenshots/plantilla-listado.png)

La tabla muestra todas las cláusulas en el orden en que van a aparecer en el PDF. Cada fila tiene:

| Columna | Descripción |
|---------|-------------|
| **#** | Número de orden en el contrato |
| **Número** | Nombre ordinal de la cláusula (ej: PRIMERA, SEGUNDA) |
| **Título** | Título en mayúsculas (ej: PARTES, OBJETO, PLAZO) |
| **Estado** | Verde = incluida en el PDF / Gris = no se incluye |
| **Acciones** | Botones para reordenar, activar/desactivar, editar y eliminar |

---

## Acciones disponibles en cada cláusula

Los botones de la columna **Acciones** (de izquierda a derecha):

| Ícono | Función |
|-------|---------|
| ↑ (flecha arriba) | Sube la cláusula una posición en el contrato |
| ↓ (flecha abajo) | Baja la cláusula una posición en el contrato |
| 👁 (ojo) | Activa o desactiva la cláusula. Si está desactivada no aparece en el PDF |
| ✏️ (lápiz) | Abre el editor para modificar el texto |
| 🗑️ (papelera) | Elimina la cláusula |

---

## Agregar una cláusula nueva

1. Hacé clic en el botón **+ Nueva cláusula** (arriba a la derecha)
2. Completá los campos:
   - **Número**: nombre ordinal en mayúsculas (ej: `VIGÉSIMA SEXTA`)
   - **Título**: tema de la cláusula en mayúsculas (ej: `GARANTÍA ADICIONAL`)
   - **Texto**: contenido completo de la cláusula
3. Hacé clic en **Crear cláusula**

La nueva cláusula se agrega al final de la lista. Podés reordenarla con las flechas ↑↓.

---

## Editar el texto de una cláusula

1. Hacé clic en el ícono ✏️ de la cláusula que querés modificar
2. Se abre el panel **Editar cláusula**
3. Modificá el texto libremente en el área de texto
4. Hacé clic en **Guardar cambios**

### Insertar datos del contrato en el texto

En lugar de escribir nombres fijos, podés usar **variables** que se reemplazan automáticamente con los datos reales cuando se genera el PDF.

**Cómo insertar una variable:**

1. Posicioná el cursor en el texto donde querés insertar el dato
2. Hacé clic en el botón **`{ }` Insertar variable** (aparece arriba del área de texto)
3. Se despliega el selector de variables con pestañas por entidad:
   - **Locador** — datos del propietario
   - **Locatario** — datos del inquilino
   - **Propiedad** — datos del inmueble
   - **Garante** — datos del garante/fiador
   - **Contrato** — montos, fechas, condiciones
   - **Empresa** — datos de la inmobiliaria
4. Elegí la pestaña que corresponde y hacé clic en el campo que necesitás
5. La variable se inserta automáticamente en el cursor

**Ejemplo:** si hacés clic en `{locador.telefono}` dentro del texto, al generar el PDF ese texto se reemplaza por el número de teléfono real del propietario.

---

## Activar o desactivar una cláusula

- Una cláusula **activa** (badge verde) se incluye en el PDF
- Una cláusula **inactiva** (badge gris) se omite del PDF pero no se elimina

Para cambiar el estado: hacé clic en el ícono 👁 de la fila.

También podés cambiarlo desde el editor: abrís la cláusula con ✏️ y marcás o desmarcás el checkbox **"Cláusula activa (incluida en el PDF)"**.

Esto es útil para tener cláusulas opcionales que activás o desactivás según el contrato.

---

## Primera vez: cargar las cláusulas predeterminadas

Si la lista aparece vacía, podés cargar automáticamente las **25 cláusulas estándar de la Ley 27551**:

1. Hacé clic en **Cargar cláusulas predeterminadas (Ley 27551)**
2. Se cargan todas las cláusulas listas para usar
3. Después podés editarlas, reordenarlas o agregar las tuyas

> ⚠️ Este botón solo aparece cuando no hay ninguna cláusula cargada.

---

## Consejos

- **Orden importa**: las cláusulas aparecen en el PDF exactamente en el orden que ves en la tabla. Usá las flechas ↑↓ para acomodarlas.
- **Guardá antes de cerrar**: si cerrás el modal sin hacer clic en Guardar, los cambios se pierden.
- **Las variables distinguen mayúsculas**: `{locador.email}` funciona, `{Locador.Email}` no.
- **Podés combinar texto libre y variables**: el texto puede tener cualquier redacción, con variables intercaladas donde necesites los datos del contrato.
