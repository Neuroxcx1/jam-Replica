using UnityEngine;

// Al replicarte: unas hebras de carne unen al jugador con la replica, se estiran y se rompen
// (primero las de fuera, la del medio la ultima).
public class SplitEffect : MonoBehaviour
{
    [SerializeField] LineRenderer[] strands;
    [SerializeField] ParticleSystem droplets;
    [SerializeField] float snapDistance = 1.4f;
    [SerializeField] float maxTime = 0.4f;
    [SerializeField] float strandWidth = 0.3f;
    [SerializeField] float spread = 0.3f;

    Transform from;
    Transform to;
    float timer;
    bool[] snapped;

    public void Init(Transform from, Transform to)
    {
        this.from = from;
        this.to = to;
        snapped = new bool[strands.Length];
        UpdateStrands();
    }

    void Update()
    {
        if (snapped == null) return;
        timer += Time.deltaTime;
        UpdateStrands();
    }

    void UpdateStrands()
    {
        for (int s = 0; s < strands.Length; s++)
        {
            if (snapped[s]) continue;

            float offset = Mathf.Lerp(spread, -spread, s / (strands.Length - 1f));
            if (from == null || to == null)
            {
                Snap(s, transform.position);
                continue;
            }

            Vector3 start = from.position + Vector3.up * offset;
            Vector3 end = to.position + Vector3.up * offset;
            float distance = Vector3.Distance(start, end);

            // las hebras de fuera aguantan menos
            float limit = snapDistance * (1f - Mathf.Abs(offset) / spread * 0.3f);
            if (distance > limit || timer > maxTime)
            {
                Snap(s, (start + end) / 2);
                continue;
            }

            // cuanto mas se estira, mas fina en el medio y mas cuelga
            float stretch = distance / limit;
            LineRenderer strand = strands[s];
            for (int i = 0; i < strand.positionCount; i++)
            {
                float t = i / (strand.positionCount - 1f);
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y -= Mathf.Sin(t * Mathf.PI) * 0.12f * stretch;
                strand.SetPosition(i, point);
            }

            float middle = Mathf.Lerp(strandWidth * 0.9f, 1f / 16f, stretch);
            strand.widthCurve = new AnimationCurve(
                new Keyframe(0f, strandWidth), new Keyframe(0.5f, middle), new Keyframe(1f, strandWidth));
        }
    }

    void Snap(int s, Vector3 point)
    {
        snapped[s] = true;
        strands[s].enabled = false;
        droplets.transform.position = point;
        droplets.Emit(4);
    }
}
