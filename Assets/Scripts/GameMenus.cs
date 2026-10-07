using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu de pausa (Esc o Start del mando) y de opciones (volumen general, de la musica y de los efectos).
// Es el prefab Resources/Menus: se crea solo al empezar el juego y sigue entre escenas, no hay que ponerlo en ninguna.
// Los volumenes van al mezclador Sounds/Mezclador (grupos Master, Musica y Efectos) y se guardan para la proxima vez.
// La musica (Music Source) suena por el grupo Musica; los sonidos de los prefabs van por Efectos.
// Sonidos de los menus: Move al cambiar de opcion (o pasar el raton), Select al elegir y Back al volver.
public class GameMenus : MonoBehaviour
{
    public static GameMenus Instance { get; private set; }
    public static bool Paused { get; private set; }

    public enum UiSound { Move, Select, Back }

    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioSource musicSource;

    [Header("Sonidos de los menus")]
    [SerializeField] AudioSource uiSource;
    [SerializeField] AudioClip moveSound;
    [SerializeField] AudioClip selectSound;
    [SerializeField] AudioClip backSound;

    [Header("Pausa")]
    [SerializeField] GameObject background;
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button resumeButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button menuButton;
    [SerializeField] Button checkpointButton;


    [Header("Opciones")]
    [SerializeField] GameObject optionsPanel;
    [SerializeField] Slider master;
    [SerializeField] Slider music;
    [SerializeField] Slider effects;
    [SerializeField] TMP_Text masterValue;
    [SerializeField] TMP_Text musicValue;
    [SerializeField] TMP_Text effectsValue;
    [SerializeField] Button backButton;

    Action onOptionsClosed;
    GameObject lastSelected;
    float lastMoveSound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        Paused = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var prefab = Resources.Load<GameMenus>("Menus");
        if (Instance == null && prefab != null) Instantiate(prefab);
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        resumeButton.onClick.AddListener(Resume);
        checkpointButton.onClick.AddListener(ToCheckpoint);
        optionsButton.onClick.AddListener(() => OpenOptions(() => Select(optionsButton)));
        menuButton.onClick.AddListener(ToMainMenu);
        backButton.onClick.AddListener(CloseOptions);
        Setup(master, masterValue, "Master");
        Setup(music, musicValue, "Musica");
        Setup(effects, effectsValue, "Efectos");
        Show(null);
    }

    void Start()
    {
        // el mezclador no hace caso a SetFloat en Awake
        SetVolume("Master", master.value);
        SetVolume("Musica", music.value);
        SetVolume("Efectos", effects.value);
        if (musicSource.clip != null) musicSource.Play();
    }

    void Update()
    {
        bool pause = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                     || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        bool back = pause || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (optionsPanel.activeSelf)
        {
            if (back) CloseWithSound();
        }
        else if (Paused)
        {
            if (back)
            {
                PlayUi(UiSound.Back);
                Resume();
            }
        }
        else if (pause && CanPause())
        {
            PlayUi(UiSound.Select);
            Pause();
        }

        // al cambiar de opcion (con teclado, mando o raton) suena Move
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != lastSelected && selected != null && lastSelected != null) PlayUi(UiSound.Move);
        lastSelected = selected;

        // por si algo (la pausa del golpe al morir) devuelve el tiempo mientras esta el menu
        if (Paused) Time.timeScale = 0f;
        Rumble.Tick();
    }

    // solo jugando: no en el menu del principio, ni en la cinematica, ni con el panel de victoria
    static bool CanPause()
    {
        Player player = FindAnyObjectByType<Player>();
        return player != null && player.enabled;
    }

    void Pause()
    {
        Rumble.Stop();

        checkpointButton.interactable = GameProgress.HasSave;

        Paused = true;
        Time.timeScale = 0f;
        Show(pausePanel);
        Select(resumeButton);
    }

    void Resume()
    {
        Paused = false;
        Time.timeScale = 1f;
        Show(null);
    }

    // tambien lo abre el menu del principio; al cerrarlo se llama a onClosed
    public void OpenOptions(Action onClosed)
    {
        onOptionsClosed = onClosed;
        Show(optionsPanel);
        Select(master);
    }

    void CloseWithSound()
    {
        PlayUi(UiSound.Back);
        CloseOptions();
    }

    void CloseOptions()
    {
        Show(Paused ? pausePanel : null);
        onOptionsClosed?.Invoke();
    }

    void ToMainMenu()
    {
        Resume();
        IntroCinematic.ShowMenuAgain();
        SceneManager.LoadScene("Intro");
    }

    void Show(GameObject panel)
    {
        pausePanel.SetActive(panel == pausePanel);
        optionsPanel.SetActive(panel == optionsPanel);
        background.SetActive(panel != null);
    }

    void ToCheckpoint()
    {
        if (!GameProgress.HasSave)
        return;

        Show(null);
        
        Paused = false;
        Time.timeScale = 1f;
        
        GameProgress.ContinueGame();
    }

    // elige una opcion sin que suene Move (al abrir un menu o al volver a el)
    public void Select(Selectable selectable)
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        lastSelected = selectable.gameObject;
    }

    // al salir del juego el mando no se queda vibrando
    void OnDisable() => Rumble.Off();

    public void PlayUi(UiSound sound)
    {
        AudioClip clip = sound == UiSound.Move ? moveSound : sound == UiSound.Select ? selectSound : backSound;
        // con el mando, un toque suave al moverse y algo mas al elegir o volver
        Rumble.Pulse(sound == UiSound.Move ? 0.12f : 0.25f, sound == UiSound.Move ? 0.05f : 0.09f);
        // el de moverse mas bajo: suena mucho
        if (clip != null) uiSource.PlayOneShot(clip, sound == UiSound.Move ? 0.4f : 0.7f);
    }

    // cada barra guarda su volumen (de 0 a 1) y lo pone en el mezclador. El parametro se llama como el grupo
    void Setup(Slider slider, TMP_Text value, string group)
    {
        slider.value = PlayerPrefs.GetFloat("Volumen" + group, 1f);
        value.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            // un toque al moverla, sin que al arrastrar con el raton suene sin parar
            if (Time.unscaledTime - lastMoveSound > 0.08f)
            {
                lastMoveSound = Time.unscaledTime;
                PlayUi(UiSound.Move);
            }
            PlayerPrefs.SetFloat("Volumen" + group, v);
            SetVolume(group, v);
            value.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }

    void SetVolume(string group, float volume)
    {
        mixer.SetFloat(group, volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f);
    }
}
