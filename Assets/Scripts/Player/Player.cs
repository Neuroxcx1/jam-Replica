using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class Player : MonoBehaviour
{
    public float moveSpeed = 7f;

    [Header("Salto")]
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.12f;

    [Header("Suelo")]
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
    [SerializeField] LayerMask groundLayer;

    [Header("Replica")]
    [SerializeField] int maxReplicas = 6;
    [SerializeField] float replicateCooldown = 0.3f;
    [SerializeField] Clone clonePrefab;
    [SerializeField] GameObject bodyPrefab;
    [SerializeField] Transform visual;

    [Header("Efectos")]
    [SerializeField] SplitEffect splitEffect;
    [SerializeField] GameObject deathEffect;
    [SerializeField] BodyEffect freezeEffect;
    [SerializeField] BodyEffect corpseEffect;
    [SerializeField] float replicateShake = 4f;

    [Header("Reinicio")]
    [SerializeField] float holdToRestartAll = 1f;

    public Rigidbody2D Rb { get; private set; }
    public Vector3 Feet => groundCheck.position;
    public Vector3 Checkpoint => checkpoint;
    public float MoveInput { get; private set; }
    public bool JumpReleased => jumpAction.WasReleasedThisFrame();
    public bool IsDead { get; private set; }
    public int ReplicasLeft { get; private set; }
    public int MaxReplicas => maxReplicas;

    InputAction moveAction;
    InputAction jumpAction;
    InputAction replicateAction;
    InputAction dieAction;
    InputAction restartAction;

    StateMachine stateMachine;
    Collider2D col;
    Vector3 checkpoint;
    int facing = 1;
    float checkpointTime;
    float restartHeld;
    float carry;
    float coyoteTimer;
    float jumpBufferTimer;
    float replicateTimer;

    void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        stateMachine = GetComponentInChildren<StateMachine>();
        checkpoint = transform.position;
        ReplicasLeft = maxReplicas;

        InputSystem.actions.FindActionMap("Player").Enable();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        replicateAction = InputSystem.actions.FindAction("Replicate");
        dieAction = InputSystem.actions.FindAction("Die");
        restartAction = InputSystem.actions.FindAction("Restart");
    }

    void Update()
    {
        // T: vuelve al principio de la zona y quita los cuerpos que has dejado en ella (por si alguno tapa el camino).
        // Mantenida un segundo reinicia el nivel entero
        if (restartAction.IsPressed())
        {
            restartHeld += Time.unscaledDeltaTime;
            if (restartHeld >= holdToRestartAll)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
        }
        else if (restartHeld > 0f)
        {
            restartHeld = 0f;
            RestartZone();
            return;
        }

        if (IsDead) return;

        MoveInput = moveAction.ReadValue<Vector2>().x;
        if (MoveInput != 0)
        {
            facing = MoveInput > 0 ? 1 : -1;
            visual.localScale = new Vector3(facing, 1, 1);
        }

        // coyote time: un momento de gracia para saltar despues de dejar el suelo
        if (IsGrounded() && Rb.linearVelocity.y <= 0.01f) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // buffer: guarda el salto aunque lo pulses un poco antes de tocar el suelo
        if (jumpAction.WasPressedThisFrame()) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        // espera entre replicas para que no salgan una encima de otra
        replicateTimer -= Time.deltaTime;
        if (replicateAction.WasPressedThisFrame() && replicateTimer <= 0 && ReplicasLeft > 0) Replicate();
        if (dieAction.WasPressedThisFrame()) Die(frozen: true);
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    public bool CanJump() => jumpBufferTimer > 0 && coyoteTimer > 0;

    public void ClearJumpTimers()
    {
        jumpBufferTimer = 0;
        coyoteTimer = 0;
    }

    public void Move(float direction)
    {
        Rb.linearVelocity = new Vector2(direction * moveSpeed + carry, Rb.linearVelocity.y);
        carry = 0f;
    }

    // la cinta transportadora te arrastra
    public void Carry(float speed) => carry = speed;

    void Replicate()
    {
        replicateTimer = replicateCooldown;
        ReplicasLeft--;
        Clone clone = Instantiate(clonePrefab, transform.position, Quaternion.identity);
        clone.Init(facing, col);

        if (splitEffect != null)
            Instantiate(splitEffect, transform.position, Quaternion.identity).Init(transform, clone.transform);
        CameraFollow.Shake(replicateShake, 0.15f);
    }

    // frozen = true con la R: el cuerpo se congela donde estas, aunque sea en el aire.
    // Al morir por otra cosa (pinchos) el cuerpo se queda pero no se congela.
    public void Die(bool frozen = false)
    {
        if (IsDead) return;

        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);

        // morir tambien gasta una replica. Sin replicas vuelves al checkpoint pero no dejas cuerpo
        if (ReplicasLeft > 0)
        {
            ReplicasLeft--;
            GameObject body = Instantiate(bodyPrefab, transform.position, Quaternion.identity);
            if (frozen) body.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

            BodyEffect effect = frozen ? freezeEffect : corpseEffect;
            if (effect != null) Instantiate(effect, transform.position, Quaternion.identity).Attach(body);
        }
        stateMachine.ChangeState("dead");
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
    }

    public void SetCheckpoint(Vector3 position)
    {
        // solo un checkpoint nuevo recarga las replicas, reaparecer en el mismo no
        if (position == checkpoint) return;
        checkpoint = position;
        checkpointTime = Time.time;
        ReplicasLeft = maxReplicas;
    }

    void RestartZone()
    {
        // de atras hacia delante: al destruir un cuerpo se quita el solo de la lista
        for (int i = Body.All.Count - 1; i >= 0; i--)
            if (Body.All[i].CreatedAt >= checkpointTime) Destroy(Body.All[i].gameObject);
        foreach (Clone clone in FindObjectsByType<Clone>())
            Destroy(clone.gameObject);

        ReplicasLeft = maxReplicas;
        Respawn();
        stateMachine.ChangeState("idle");
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
