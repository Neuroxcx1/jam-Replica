using UnityEngine;

// Cientifico de la sala de observacion: pasea detras de la ventana y de vez en cuando se para a mirar
// al especimen (tu). Con el apagon sale corriendo y desaparece detras de la pared.
public class Scientist : MonoBehaviour
{
    [SerializeField] Sprite[] frames;          // 0 quieto, el resto andando
    [SerializeField] Vector2 walkRange;        // x minima y maxima del paseo
    [SerializeField] float lookAtX;            // donde esta el tanque
    [SerializeField] float walkSpeed = 1.1f;
    [SerializeField] float runSpeed = 4.5f;

    SpriteRenderer sr;
    float targetX;
    float pause;
    float step;
    bool fleeing;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        targetX = Random.Range(walkRange.x, walkRange.y);
        pause = Random.Range(0f, 1.5f);
    }

    public void Flee(float exitX)
    {
        fleeing = true;
        targetX = exitX;
        // cada uno reacciona a su tiempo
        pause = Random.Range(0f, 0.4f);
    }

    void Update()
    {
        float x = transform.position.x;
        if (pause > 0f)
        {
            pause -= Time.deltaTime;
            sr.sprite = frames[0];
            if (!fleeing) sr.flipX = lookAtX < x;
            return;
        }

        float speed = fleeing ? runSpeed : walkSpeed;
        float newX = Mathf.MoveTowards(x, targetX, speed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        sr.flipX = targetX < x;
        step += Time.deltaTime * speed * 5f;
        sr.sprite = frames[1 + (int)step % (frames.Length - 1)];

        if (newX != targetX) return;
        if (fleeing)
        {
            gameObject.SetActive(false);
            return;
        }

        pause = Random.Range(1f, 2.5f);
        targetX = Random.Range(walkRange.x, walkRange.y);
    }
}
