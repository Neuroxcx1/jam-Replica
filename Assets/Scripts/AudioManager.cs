using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // Esta es la "puerta global" para que los prefabs lo encuentren
    public static AudioManager Instance { get; private set; }

    private AudioSource audioSource;

    void Awake()
    {
        // Configuramos el acceso global
        if (Instance == null)
        {
            Instance = this;
            // Obtenemos el componente aquí mismo para que esté listo de inmediato
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            // Si por error pones dos AudioManagers en la escena, esto destruye la copia
            Destroy(gameObject);
        }
    }

    public void ReproducirSonido(AudioClip audio)
    {
        if (audio != null)
        {
            audioSource.PlayOneShot(audio);
        }
    }
}