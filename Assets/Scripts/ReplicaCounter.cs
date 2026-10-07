using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class ReplicaCounter : MonoBehaviour
{
    Player player;
    [SerializeField] TextMeshProUGUI label; 

    int shown = -1;


    static void Create()
    {
        if (FindAnyObjectByType<Player>() == null) return;
        new GameObject("ReplicaCounter").AddComponent<ReplicaCounter>();
    }

    void Awake()
    {
        /*var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        label = new GameObject("Label").AddComponent<Text>();
        label.transform.SetParent(transform, false);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 40;
        label.color = Color.white;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -20);
        rect.sizeDelta = new Vector2(500, 60);
        */
    }

    void Update()
    {
        // al reiniciar el nivel hay un Player nuevo
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
            if (player == null) return;
            shown = -1;
        }

        // solo cambia el texto cuando cambia el numero
        if (player.ReplicasLeft == shown) return;
        shown = player.ReplicasLeft;
        label.text = $"{shown:0} / {player.MaxReplicas}";
    }
}
