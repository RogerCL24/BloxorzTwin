# Proyecto Bloxorz - VJ (FIB)

**Integrantes:**
* Roger
* Fardin

---

## 🚀 Tareas Pendientes (Milestones)

Esta es nuestra guía de trabajo, ordenada por prioridad. Marcamos una casilla cuando la funcionalidad esté terminada y subida a Git.

### Hito 1: Setup Inicial y Assets
* [x] **Diseño del Bloque:** Crear/Descargar el modelo 3D del bloque (jugador) y los tiles básicos (suelo, meta, botón, etc.).
* [X] **Cámara Ortográfica:** Configurar la `Main Camera` como Ortográfica en el Inspector.
* [X] **Crear Escenas:** Crear las 3 escenas (`Menu`, `Level_1`, `Credits`).
* [X] **Navegación:** Script simple para que los botones del Menú carguen las escenas `Level_1` y `Credits`.
* [ ] **Volver al Menú:** Añadir un botón en la escena de juego para volver al `Menu`.

### Hito 2: El Movimiento del Bloque (Núcleo)
* [ ] **Control de Input:** Leer las teclas WASD / Flechas.
* [ ] **Cálculo de Pivote:** Calcular el punto correcto (arista) sobre el que el bloque debe rotar.
* [ ] **Rotación:** Implementar la rotación de 90 grados alrededor del pivote (no un deslizamiento).
* [ ] **Control de Estado:** Bloquear el input del jugador mientras la rotación se está ejecutando (para evitar movimientos dobles).

### Hito 3: Ganar y Perder (Lógica de Juego)
* [X] **Caída (Perder):** Detectar si el bloque cae de la plataforma.
* [X] **Reiniciar Nivel:** Recargar la escena actual cuando el jugador pierde.
* [X] **Tile de Meta (Ganar):** Crear un Prefab para la meta.
* [ ] **Detección de Victoria:** Detectar si el bloque está en **vertical** sobre la meta.
* [ ] **Cargar Siguiente Nivel:** Cargar el `Level_2` (y así sucesivamente) al ganar.

### Hito 4: Tiles Especiales (Interacciones)
* [ ] **Botones Redondos:** Se activan al tocarlos.
* [ ] **Puentes:** Lógica para que los botones redondos activen/desactiven puentes.
* [ ] **Botones en Cruz:** Se activan solo si el bloque está en vertical sobre ellos.
* [ ] **Tiles Naranjas:** El bloque cae si está en vertical sobre ellos.
* [ ] **Tile de División:** Divide el bloque en dos cubos.
* [ ] **Control Dividido:** Poder cambiar entre los dos cubos con la tecla Espacio.
* [ ] **Re-Unión:** Si los dos cubos se tocan, vuelven a ser un bloque.

### Hito 5: Contenido y Pulido
* [X] **Diseño de 10 Niveles:** Crear 10 escenas de nivel con dificultad creciente.
* [ ] **UI (Movimientos):** Mostrar el contador de movimientos en la interfaz.
* [ ] **Atajos de Nivel:** Teclas '0' al '9' para cargar niveles directamente.
* [X] **Fondos:** Añadir un fondo no sólido a los niveles.
* [ ] **Sonido y Música:** Implementar música de fondo y efectos de sonido.
* [ ] **Feedback (Animación):**
    * [ ] Animación de tiles apareciendo al inicio del nivel.
    * [ ] Animación de victoria (girar y subir).
    * [ ] Animación de derrota (caer).
* [ ] **Feedback (Efectos):** Añadir partículas y sonidos a las interacciones (coger ítems, botones, etc.).

### Hito 6: Entregables
* [ ] **Escribir `memoria.pdf`:** Redactar el documento final siguiendo todos los puntos del enunciado.
* [ ] **Generar Build (`Binari`):** Crear el ejecutable del juego.
* [ ] **Grabar `demo.avi`:** Grabar un vídeo demo de 1 minuto (y comprimirlo).
* [ ] **Limpiar Proyecto:** Eliminar la carpeta `Library` y assets no usados antes de comprimir.

---

## 🛠️ Recursos Útiles (Links del PDF)

### Para ENCONTRAR Assets 3D
* [TurboSquid](http://www.turbosquid.com/)
* [Free3D](https://free3d.com/)
* [Unity Asset Store](http://www.assetstore.unity3d.com)
* [BlenderKit](https://www.blenderkit.com/)

### Para CREAR Assets 3D
* **Vóxels:** [MagicaVoxel](https://ephtracy.github.io/)
* **Modelado 3D:** [Blockbench](https://www.blockbench.net/)
* **Modelado 3D (Avanzado):** [Blender](https://www.blender.org/)