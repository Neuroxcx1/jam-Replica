using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class Player : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 9f;
    // acelera y frena en vez de cambiar de golpe; en el aire frena menos para conservar el impulso
    [SerializeField] float groundAcceleration = 90f;
    [SerializeField] float groundDeceleration = 120f;
    [SerializeField] float airAcceleration = 70f;
    [SerializeField] float airDeceleration = 45f;
    [SerializeField] float gravity = 3.4f;

    [Header("Salto")]
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.15f;

    [Header("Suelo")]
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
    [SerializeField] LayerMask groundLayer;

    [Header("Replica")]
    [SerializeField] int maxReplicas = 6;
    [SerializeField] float replicateCooldown = 0.25f;
    [SerializeField] Clone clonePrefab;
    [SerializeField] GameObject bodyPrefab;
    [SerializeField] Transform visual;

    [Header("Efectos")]
    [SerializeField] SplitEffect splitEffect;
    [SerializeField] GameObject deathEffect;
    [SerializeField] BodyEffect freezeEffect;
    [SerializeField] GameObject recallEffect;
    [SerializeField] RecallGhost recallGhost;
    [SerializeField] float replicateShake = 5f;

    public Rigidbody2D Rb { get; private set; }
    public Vector3 Feet => groundCheck.position;
    public Vector3 Checkpoint => checkpoint;
    // al volver al checkpoint despues de morir (el cristal se recompone con esto)
    public event System.Action Respawned;
    public float MoveInput { get; private set; }
    public float BaseGravity => gravity;
    public bool JumpHeld => jumpAction.IsPressed();
    public bool JumpReleased => jumpAction.WasReleasedThisFrame();
    public bool IsDead { get; private set; }
    public int ReplicasLeft { get; private set; }
    public int MaxReplicas => maxReplicas;

    InputAction moveAction;
    InputAction jumpAction;
    InputAction replicateAction;
    InputAction freezeAction;
    InputAction recallAction;
    InputAction restartAction;

    // las replicas que has puesto en esta zona (clones, cuerpos y copias congeladas), de la mas antigua a la mas nueva
    readonly List<GameObject> placed = new List<GameObject>();
    // las de zonas anteriores: ya no cuentan, pero al morir tambien desaparecen
    readonly List<GameObject> leftBehind = new List<GameObject>();
    readonly Collider2D[] groundHits = new Collider2D[8];

    StateMachine stateMachine;
    Collider2D col;
    Vector3 checkpoint;
    int facing = 1;
    float carry;
    float lastCarry;
    float coyoteTimer;
    float jumpBufferTimer;
    float jumpLockTimer;
    float replicateTimer;

    void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Rb.gravityScale = gravity;
        col = GetComponent<Collider2D>();
        stateMachine = GetComponentInChildren<StateMachine>();
        checkpoint = transform.position;
        ReplicasLeft = maxReplicas;

        InputSystem.actions.FindActionMap("Player").Enable();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        replicateAction = InputSystem.actions.FindAction("Replicate");
        freezeAction = InputSystem.actions.FindAction("Freeze");
        recallAction = InputSystem.actions.FindAction("Recall");
        restartAction = InputSystem.actions.FindAction("Restart");
    }

    void Update()
    {
        if (GameMenus.Paused) return;

        // K: reinicia el nivel entero
        if (restartAction.WasPressedThisFrame())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (IsDead) return;

        // el stick del mando rebota un poco hacia el otro lado al soltarlo: solo cuenta pasada la mitad,
        // y entonces a tope, como las teclas (si no, al soltarlo yendo a la izquierda te giraba a la derecha)
        float x = moveAction.ReadValue<Vector2>().x;
        MoveInput = Mathf.Abs(x) < 0.5f ? 0f : Mathf.Sign(x);
        if (MoveInput != 0)
        {
            facing = MoveInput > 0 ? 1 : -1;
            visual.localScale = new Vector3(facing, 1, 1);
        }

        // coyote time: un momento de gracia para saltar despues de dejar el suelo.
        // No se mira si subes (en el montacargas o la prensa subes sin saltar): solo se ignora el suelo justo al saltar
        jumpLockTimer -= Time.deltaTime;
        if (IsGrounded() && jumpLockTimer <= 0) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // buffer: guarda el salto aunque lo pulses un poco antes de tocar el suelo
        if (jumpAction.WasPressedThisFrame()) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        // Shift: lanza un clon. Ctrl: deja una copia congelada. Q: recupera la replica mas antigua
        replicateTimer -= Time.deltaTime;
        if (replicateAction.WasPressedThisFrame() && replicateTimer <= 0 && ReplicasLeft > 0) Replicate();
        if (freezeAction.WasPressedThisFrame() && ReplicasLeft > 0) Freeze();
        if (recallAction.WasPressedThisFrame()) Recall();
    }

    public bool IsGrounded()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        int count = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, filter, groundHits);

        // la copia congelada que estas atravesando no cuenta como suelo
        for (int i = 0; i < count; i++)
            if (!Physics2D.GetIgnoreCollision(col, groundHits[i])) return true;
        return false;
    }

    public bool CanJump() => jumpBufferTimer > 0 && coyoteTimer > 0;

    public void ClearJumpTimers()
    {
        jumpBufferTimer = 0;
        coyoteTimer = 0;
        jumpLockTimer = 0.1f;
    }

    public void Move(float direction)
    {
        bool grounded = IsGrounded();
        float rate = direction != 0
            ? (grounded ? groundAcceleration : airAcceleration)
            : (grounded ? groundDeceleration : airDeceleration);

        // se parte de la velocidad real (si chocas con una pared ya es 0) sin lo que puso la cinta
        float run = Mathf.MoveTowards(Rb.linearVelocity.x - lastCarry, direction * moveSpeed, rate * Time.fixedDeltaTime);
        Rb.linearVelocity = new Vector2(run + carry, Rb.linearVelocity.y);
        lastCarry = carry;
        carry = 0f;
    }

    // la cinta transportadora te arrastra
    public void Carry(float speed) => carry = speed;

    void Replicate()
    {
        replicateTimer = replicateCooldown;
        ReplicasLeft--;
        Clone clone = Instantiate(clonePrefab, transform.position, Quaternion.identity);
        clone.Init(facing, this);
        placed.Add(clone.gameObject);

        if (splitEffect != null)
            Instantiate(splitEffect, transform.position, Quaternion.identity).Init(transform, clone.transform, facing);
        CameraFollow.Shake(replicateShake, 0.15f);
    }

    // deja una copia congelada (el cubo) donde tienes los pies y te subes encima de ella.
    // Si no cabes encima (techo justo arriba), la atraviesas y caes
    void Freeze()
    {
        // la posicion de la fisica: la del sprite va un poco por detras (interpolacion) y al caer rapido se nota
        float half = bodyPrefab.GetComponent<BoxCollider2D>().size.y / 2f;
        Vector2 at = Rb.position + Vector2.down * (col.bounds.extents.y - half);
        ReplicasLeft--;
        GameObject body = Instantiate(bodyPrefab, at, Quaternion.identity);
        body.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        body.GetComponent<Body>().Frozen = true;
        if (freezeEffect != null) Instantiate(freezeEffect, at, Quaternion.identity).Attach(body);
        placed.Add(body);

        Vector2 top = at + Vector2.up * (half + col.bounds.extents.y + 0.02f);
        if (Physics2D.OverlapBox(top, col.bounds.size * 0.95f, 0f, groundLayer) == null)
        {
            Rb.position = top;
            transform.position = top;
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, 0f);
        }
        else body.GetComponent<Body>().IgnoreUntilApart(col);
    }

    // muestra de mutageno: replicas de mas, tambien en el maximo
    public void AddReplicas(int amount)
    {
        maxReplicas += amount;
        ReplicasLeft += amount;
    }

    // te devuelve la replica mas antigua que pusiste, en el mismo orden en que las pusiste
    void Recall()
    {
        // las que ya no existen (por ejemplo, compactadas por la prensa) no cuentan
        placed.RemoveAll(piece => piece == null);
        if (placed.Count == 0) return;

        Return(placed[0]);
    }

    // esa copia vuelve como replica (con Q la mas antigua; el montacargas devuelve el hielo que rompe)
    public void Return(GameObject piece, bool ghost = true)
    {
        if (!placed.Remove(piece)) return;
        if (recallEffect != null) Instantiate(recallEffect, piece.transform.position, Quaternion.identity);
        if (ghost && recallGhost != null) Instantiate(recallGhost).Fly(piece.GetComponentInChildren<SpriteRenderer>(), transform);
        Destroy(piece);
        ReplicasLeft = Mathf.Min(maxReplicas, ReplicasLeft + 1);
    }

    // cuando un clon se convierte en cuerpo, el cuerpo ocupa su sitio en la cola
    public void Replace(GameObject oldPiece, GameObject newPiece)
    {
        int index = placed.IndexOf(oldPiece);
        if (index >= 0) placed[index] = newPiece;
        index = leftBehind.IndexOf(oldPiece);
        if (index >= 0) leftBehind[index] = newPiece;
    }

    // al morir (laser, torreta, prensa...) no dejas nada: vuelves al checkpoint
    public void Die()
    {
        if (IsDead) return;

        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);
        // todas tus copias (clones, cuerpos y hielo) desaparecen y vuelven como replicas: no queda nada tuyo por ahi
        placed.RemoveAll(piece => piece == null);
        foreach (GameObject piece in placed.ToArray()) Return(piece, false);
        foreach (GameObject piece in leftBehind)
        {
            if (piece == null) continue;
            if (recallEffect != null) Instantiate(recallEffect, piece.transform.position, Quaternion.identity);
            Destroy(piece);
        }
        leftBehind.Clear();
        stateMachine.ChangeState("dead");
    }

    // la animacion del sprite (la piden los estados al entrar)
    public void Animate(Sprite[] frames, float fps, bool loop)
    {
        if (frames != null && frames.Length > 0) visual.GetComponent<SpriteLoop>().Play(frames, loop, fps);
    }

    public void SetAlive(bool alive)
    {
        IsDead = !alive;
        Rb.simulated = alive;
        visual.gameObject.SetActive(alive);
    }

    public void Respawn()
    {
        transform.position = checkpoint;
        Rb.linearVelocity = Vector2.zero;
        ClearJumpTimers();
        SetAlive(true);
        Respawned?.Invoke();
    }

    public void SetCheckpoint(Vector3 position)
    {
        // solo un checkpoint nuevo recarga las replicas, reaparecer en el mismo no.
        // Lo que dejaste en la zona anterior se queda ahi hasta que mueras
        if (position == checkpoint) return;
        checkpoint = position;
        ReplicasLeft = maxReplicas;
        leftBehind.AddRange(placed);
        placed.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
