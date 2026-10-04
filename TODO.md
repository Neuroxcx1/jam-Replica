# Jam Replica — TODO

## 0. Dirección del juego (prioridad)
Contexto: un mutante que escapa de un laboratorio. Sin mecánicas nuevas para el jugador (E y R).
Lo que lo diferencia: **el escenario reacciona a la masa de tus cuerpos**.

### Elementos del laboratorio (elegir 3–4 para la jam)
- [ ] **Peso**: rejillas, cristales y pasarelas que se rompen con N cuerpos encima.
      Ascensor de contrapeso. Se hace contando cuerpos en una zona (OverlapBox/trigger), sin física real.
- [ ] **Presión**: sala sellada con manómetro en la pared. Al llenarla de clones revienta (cristal o puerta).
- [x] **Bloquear**: un cuerpo encima del emisor corta el láser; un cuerpo delante tapa la torreta.
- [x] **Señuelos**: la torreta dispara a lo más cercano que ve → las réplicas como cebo.
- [ ] **Reacción en cadena**: tanque de gas que explota al chocar un clon y rompe una pared.

### Niveles
- [x] Salas "realistas" (pozos, ventanas altas, escaleras rotas). Las plataformas las crea el jugador con sus cuerpos.
- [ ] Estructura: salas de prueba al principio → mantenimiento, conductos y oficinas al escapar.
- [ ] El objetivo de cada sala es **salir rompiendo algo**, no tocar una bandera.
- [ ] Final: replicarte hasta reventar el laboratorio entero.

### Historia y presentación
- [ ] Alarmas, luces rojas y mensajes de megafonía en texto que reaccionan a lo que haces.
- [ ] La R justificada como **campo de estasis** del laboratorio. El contador como pantalla del laboratorio.
- [ ] El laboratorio se va llenando de tus cuerpos y de masa mutante.
- [ ] Paleta: laboratorio estéril (blanco, gris azulado) frente a un mutante orgánico (magenta o verde ácido).
- [ ] Rompibles legibles: grietas por fases, carteles "CARGA MÁX. 3", manómetros.

## 1. VFX / "juice" (maquillaje de lo que ya hay)

Idea general: cada acción tiene que **verse y sentirse** distinta. Sobre todo que se distinga a simple vista
qué es fantasma (réplica, no choca), qué es sólido (cuerpo congelado) y qué mata (pinchos).

### Saltar (`JumpState.Enter`)
- [x] Polvo al despegar: ParticleSystem en los pies, ráfaga de 6–10 partículas cuadradas, vida ~0.3 s.
- [ ] Squash & stretch: estirar el sprite (0.8, 1.2) al saltar y volver a (1, 1) suave.
      Ojo: `visual.localScale.x` ya se usa para girar al personaje → meter un hijo "Squash" dentro de Visual.
- [ ] Sonido de salto.

### Aterrizar (`FallState` → `Idle`/`Run`)
- [x] Polvo a los dos lados, más fuerte cuanto más rápido caías.
- [ ] Squash al tocar suelo (1.2, 0.8).
- [ ] Mini temblor de cámara solo en caídas largas. (hecho: temblor leve al saltar, medio al replicarse, fuerte + pausa al congelarse)

### Caminar (`RunState`)
- [x] Polvo cada cierta distancia mientras está en el suelo (Emission → Rate over Distance).
- [ ] Pequeño balanceo del sprite al correr (o animación de correr cuando haya pixel art).

### Replicarse (E → `Player.Replicate`, `Clone`)
- [x] Destello blanco de 1–2 frames en el jugador + píxeles cian que salen de él y forman el clon.
- [x] Shader de **holograma** para la réplica (Shader Graph, Sprite Unlit): líneas que se desplazan,
      transparencia, pequeño glitch. Comunica "no te choca".
- [x] Cuando la réplica muere (3 s o pared): efecto dissolve con ruido → aparece el cuerpo con un flash.
- [ ] Contador: "pop" al gastar una. Si pulsas E con 0: el contador tiembla + sonido de "no".

### Congelarse (R → `Player.Die`)
- [x] Shader de **hielo/cristal** para el cuerpo congelado: tinte azul, brillo diagonal, contorno.
- [x] Partículas de escarcha al congelarse + hit-stop de ~0.06 s (`Time.timeScale` muy bajo un instante).
- [ ] Flash / aberración cromática breve (Volume de URP).
- [x] Un "alma" (partícula con trail) que vuela del cuerpo al checkpoint durante el `respawnDelay`
      → explica visualmente que tú vuelves y el cuerpo se queda.
- [x] Al reaparecer: efecto de materializarse (dissolve al revés / píxeles que se juntan).
- [ ] Con 0 réplicas la R no deja cuerpo → efecto distinto (te desvaneces) para que se entienda.

### Morir en pinchos (`Hazard`)
- [x] Muerte no violenta: el cuerpo se queda sin congelar (material Replica_Dead) + nube suave + bolita al checkpoint.
- [ ] El cuerpo que queda con grietas (variante del shader de hielo).

### Checkpoint, meta y reinicio
- [ ] Checkpoint al activarse: partículas + Light2D que se enciende + "+6" volando al contador.
- [ ] Meta: partículas/confeti + sonido.
- [ ] T (reinicio): transición pixelada (fade/wipe) y que los cuerpos se rompan antes de recargar.

### Ambiente
- [x] Bajar la Global Light 2D y poner Light2D en jugador, lámparas, alarmas, láseres, puertas y desechos.
- [x] Volume con Bloom, Vignette y color grading (`Assets/Settings/Laboratorio_Postproceso`).
- [ ] Fondo con parallax.

## 2. Pixel art a 32 px por unidad (tileset Sci-Fi Labs de 32x32, personaje de 32 px)
- [x] Sprites a 32 PPU (1 unidad = 1 tile = 32 px), filtro Point, sin compresión.
- [ ] Personaje de 32 px de alto con animaciones por estado: Idle, Run, Jump, Fall, Dead.
      Encaja con la máquina de estados: cada `Enter()` reproduce su animación (como `play()` en Godot).
- [x] Nivel con Tilemap + Rule Tiles (Tile Palette "Laboratorio").
- [x] Pixel Perfect Camera (URP 2D) con resolución de referencia **640x360**: escala exacta x2 en 720p,
      x3 en 1080p, x4 en 1440p y x6 en 4K.
- [x] Partículas cuadradas sin suavizado para que no rompan el estilo.
- [ ] Ajustar colliders a medidas en píxeles.

## 3. Mejoras de diseño (aprendidas de juegos parecidos)
- [ ] **Encajar los cuerpos a una rejilla** (p. ej. medio tile) al congelarse. Lo que más se critica en
      The Swapper y Leave Me a Clone es tener que colocar el cuerpo con precisión exacta.
- [ ] **Deshacer el último cuerpo** (p. ej. Z, devuelve la réplica) en vez de solo el reinicio total.
      A REPL4CEABLE le criticaron la repetición de los reinicios completos.
- [ ] **Reaparición segura**: no reaparecer dentro de un cuerpo ni caerse nada más reaparecer
      (otra queja de REPL4CEABLE).
- [ ] **Cámara que deje ver lo de abajo** al caer y con algo de adelanto en la dirección de movimiento
      (en REPL4CEABLE no se veían las plataformas de debajo).
- [x] Física predecible: los cuerpos no se deslizan (ya hecho, se congelan). En Life Goes On frustraba
      que los cadáveres se movieran.
- [ ] Enseñar cada mecánica sola en su nivel antes de combinarlas. Evitar soluciones "rebuscadas"
      (crítica a Life Goes On).
- [ ] Medallas por usar pocas réplicas/cuerpos (como las estrellas de Leave Me a Clone) → rejugabilidad.
- [ ] Interfaz clara y legible (contador grande, contraste).

## Usar tu personaje real (cuando tengas el arte)
Los efectos no dependen del cuadrado. Para cambiarlo:
- **Player**: pon tu sprite (o un Animator) en el hijo `Visual`. Ajusta el BoxCollider2D y baja/sube `GroundCheck` a los pies.
- **Clone** (prefab): el mismo sprite/Animator en `Visual` y deja el material **Replica_Clone**. Las estelas copian el sprite solas.
- **Body** (prefab): sprite de cuerpo/estatua con el material **Replica_Body**. Al congelarse o morir en pinchos el material cambia solo (Freeze / Corpse).
- En los SpriteRenderer de réplica y cuerpo pon el color en **blanco**: el color lo da el material según las sombras/medios/luces de tu sprite (`ColorDark`, `ColorMid`, `ColorLight`).
- Todos los looks salen del mismo Shader Graph **ReplicaSprite** (`Assets/Effects/Shaders`): borde, líneas de escaneo, brillo, parpadeo y destello se activan desde cada material.
- El sprite siempre en un hijo `Visual` con el componente **PixelSnap** (Player, Clone y Body ya lo tienen). Si pones el sprite en el objeto con el Rigidbody2D el personaje vuelve a temblar al moverse.
- Si tu arte no usa 32 píxeles por unidad, cambia `PixelsPerUnit` en los materiales Replica_*, en la Pixel Perfect Camera, en `PixelSnap` y en `CameraFollow`.
- Hebras al replicarse: ajusta `strandWidth` y `spread` en el prefab **CloneSplit** al tamaño del personaje.

## El laboratorio (escena `Laboratorio`)
Tres zonas seguidas. Cada zona es una sala grande con una salida y varios sistemas que reaccionan a los cuerpos,
para que se pueda resolver de varias formas. Cada puerta de zona se cierra detrás de ti, es el checkpoint y recarga las 6 réplicas.
**T** vuelve al principio de la zona y quita los cuerpos que dejaste en ella (si uno te tapa el camino);
**mantener T** reinicia el nivel entero.

| Zona | Qué es | Obstáculo | Formas de pasarlo |
|---|---|---|---|
| 1 Pabellón de especímenes | Corrales, laboratorio y sala de control (cerrada) con plataforma de observación encima | Suelo electrificado de los corrales (8 de ancho) | Pasar cuando se apaga · subirte a un cuerpo (no conduce) · puente congelado |
| | | Plataforma de observación (+5) | Montacargas: en la cabina, 2 réplicas al contrapeso y sube · escalera de 2 cuerpos congelados |
| 2 Control de seguridad | Pasarela de observación arriba, archivo debajo, sala de vigilancia con garita | Láser fijo | Un cuerpo encima del emisor lo tapa |
| | | Láser intermitente | Pasar cuando se apaga · taparlo · romper el cristal y bajar antes de llegar a él |
| | | Suelo de cristal (aguanta 1) | Tú + una réplica encima → se rompe y bajas al archivo |
| | | Garita: puerta automática con torreta dentro | Réplica de señuelo · un cuerpo deja la puerta abierta y tapa el disparo · saltar la torreta · cuerpo congelado delante del cañón |
| 3 Planta de residuos | Cinta → compactadora, tanque de residuos, pasarela rota y muelle de carga | Vapor sobre la cinta | Pasar entre chorros · un cuerpo congelado debajo de la boca corta el chorro |
| | | Compactadora | Pasar cuando está arriba · subirte y que te suba a la pasarela · atascarla con un cuerpo congelado |
| | | Pasarela rota (6 de hueco sobre el tanque) | Cuerpo congelado en medio · cuerpos flotando en el tanque y escalera de cuerpos |

Todos los sistemas y varias rutas de cada zona están probados con el salto real (prueba automática, 31 comprobaciones).

### Editar el mapa
- **Window > 2D > Tile Palette** → paleta **Laboratorio**. Arriba a la izquierda están los dos Rule Tiles:
  **Terreno** (pone solo los bordes de piedra y los rincones) y **Fondo** (paneles al azar). El resto es el tileset entero.
- En la escena: `Mapa/Terreno` es la capa con colisión (pinta ahí paredes y suelo con el Rule Tile Terreno),
  `Mapa/Fondo` es la pared de atrás (tuberías incluidas, son tiles normales).
- Trampas, máquinas, puertas y decorado son objetos normales en `Trampas y puertas` y `Decorado`: se mueven y se duplican.
  Los láseres y el vapor salen hacia el `up` de su objeto (gíralos para otras direcciones).
- **Replica > Crear laboratorio** regenera la escena desde `LabBuilder.cs` (borra lo retocado a mano).
- Las máquinas (puertas, montacargas, cristal, cinta, prensa, vapor, suelo eléctrico, tanque) son sprites dibujados
  con la paleta del tileset en `Assets/Sprites/Map/Generado`: se pueden cambiar por arte de verdad sin tocar código.

### Sistemas (`Assets/Scripts/World`)
- `ZoneDoor`: puerta de zona (se cierra detrás, checkpoint). `FinalDoor`: salida, funde a negro, sin puntuación.
- `AutoDoor`: puerta automática; se abre con lo que tenga delante (jugador, réplica o cuerpo) y un cuerpo la atasca abierta.
- `CounterweightLift`: montacargas; la cabina sube si el contrapeso carga más que ella.
- `FragileGlass`: suelo de cristal que aguanta `maxLoad`; al límite se raja, con más se rompe.
- `Conveyor`: cinta transportadora; arrastra al jugador, réplicas y cuerpos.
- `HydraulicPress`: compactadora; aplasta, compacta cuerpos normales y se atasca con uno congelado. Encima te sube.
- `SteamVent`: vapor intermitente; quema y lo corta lo que tenga delante.
- `ElectricFloor`: suelo electrificado a pulsos; encima de un cuerpo no te pasa nada.
- `SecurityLaser`, `Turret`, `WastePool` + `Body` (los cuerpos flotan), `LightFlicker`, `SpriteLoop`.
- `Load`: cuenta lo que pesa encima de algo (los cuerpos congelados están en estasis: no pesan).

### Problemas detectados al probarlo
- [x] **Bloqueo por error**: T reinicia solo la zona (quita sus cuerpos) y las salidas importantes son altas o tienen otra ruta.
- [x] **Coherencia**: fuera sierras y cuchillas; cada sala tiene un uso (corrales, sala de control, archivo, garita,
      planta de residuos con tanque y muelle) y cada peligro es de laboratorio (seguridad, maquinaria, residuos).
- [x] **Creatividad**: salas abiertas con varios sistemas que reaccionan a los cuerpos; cada obstáculo tiene 2 o más formas.
- [x] Rincones del terreno: el Rule Tile ya usa piezas de rincón (tiles 8, 9, 18 y 19, generadas desde el tileset).
- [ ] Probarlo jugando: ajustar tiempos (vapor, prensa, suelo eléctrico) y ver qué soluciones salen que no estaban pensadas.

### Ideas siguientes
- [ ] Sala de presión (la idea de reventar una sala llenándola de réplicas).
- [ ] Megafonía en texto que reacciona ("Espécimen fuera de contención").
- [ ] Sonido: zumbido de fluorescentes, láser, torreta, prensa, vapor, cinta.
- [ ] Arte de verdad para las máquinas y vallas de los corrales.

## 4. Pendientes técnicos
- [ ] Borrar `D:\ReplicaTestCopy` (copia de pruebas, ~2 GB).
- [ ] Decidir si se queda `Assets/Editor/PrototypeBuilder.cs` (solo sirve para regenerar el nivel de prueba).

## Referencias
- Life Goes On: Done to Death — https://store.steampowered.com/app/250050/Life_Goes_On_Done_to_Death/
- The Swapper — https://store.steampowered.com/app/231160/The_Swapper/
- Leave Me a Clone — https://jayisgames.com/review/leave-me-a-clone.php
- REPL4CEABLE (16x16) — https://plum92.itch.io/repl4ceable
- Oops, I Froze! (16x16) — https://ilovemath25.itch.io/oops-i-froze
- CloneUp – Stack Yourself — https://lowpixelbyte.itch.io/cloneup-stack-yourself
- Cycle of lives — https://pavlo-konoplov.itch.io/cycle-of-lives
- Juegos de plataformas 16x16 en itch.io — https://itch.io/games/genre-platformer/tag-16x16
