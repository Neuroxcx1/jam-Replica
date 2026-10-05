using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField] GameObject victoryPanel;
    [SerializeField] Animator victoryAnimator;
    bool completed = false;

    void Awake()
    {
        victoryPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (completed) return;

        if (other.TryGetComponent<Player>(out _))
        {
            completed = true;

            victoryPanel.SetActive(true);
            victoryAnimator.SetTrigger("Open");
        }
    }


}
