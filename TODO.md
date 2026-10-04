# Jam Replica — TODO

## 0. Dirección del juego (prioridad)
Contexto: un mutante que escapa de un laboratorio. Sin mecánicas nuevas para el jugador (E y R).
Lo que lo diferencia: **el escenario reacciona a la masa de tus cuerpos**.

### Elementos del laboratorio (elegir 3–4 para la jam)
- [ ] **Peso**: rejillas, cristales y pasarelas que se rompen con N cuerpos encima.
      Ascensor de contrapeso. Se hace contando cuerpos en una zona (OverlapBox/trigger), sin física real.
- [ ] **Presión**: sala sellada con manómetro en la pared. Al llenarla de clones revienta (cristal o puerta).
- [ ] **Bloquear**: cuerpo congelado que atasca una prensa, corta un láser o deja abierta una compuerta.
- [ ] **Señuelos**: torretas o drones que disparan a lo primero que ven → las réplicas como cebo.
- [ ] **Reacción en cadena**: tanque de gas que explota al chocar un clon y rompe una pared.

### Niveles
- [ ] Salas "realistas" (pozos, ventanas altas, escaleras rotas). Las plataformas las crea el jugador con sus cuerpos.
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
- [ ] Bajar la Global Light 2D y poner Light2D en jugador, checkpoints y meta.
- [ ] Volume con Bloom (para que brillen holograma e hielo), Vignette y color grading.
- [ ] Fondo con parallax.

## 2. Pasar a pixel art 16x16
- [ ] Sprites a 16 PPU (1 unidad = 16 px), filtro Point, sin compresión.
- [ ] Personaje de 16x16 con animaciones por estado: Idle, Run, Jump, Fall, Dead.
      Encaja con la máquina de estados: cada `Enter()` reproduce su animación (como `play()` en Godot).
- [ ] Nivel con Tilemap + Rule Tiles (el paquete 2D Tilemap Extras ya está instalado).
- [x] Pixel Perfect Camera (URP 2D) con resolución de referencia **320x180**: escala exacta x6 en 1080p
      y x8 en 1440p.
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
- Si tu arte no usa 16 píxeles por unidad, cambia `PixelsPerUnit` en los materiales Replica_* y en la Pixel Perfect Camera.
- Hebras al replicarse: ajusta `strandWidth` y `spread` en el prefab **CloneSplit** al tamaño del personaje.

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
