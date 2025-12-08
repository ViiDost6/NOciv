using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("General")]
    public AudioClip musicClip; 
    public AudioClip buttonClip; 
    public AudioClip hoverClip; 
    public AudioClip moveClip; 
    public AudioClip captureAIClip; 
    public AudioClip capturePlayerClip; 
    public AudioClip victory; 
    public AudioClip defeat; 
    public AudioClip playerTurnStart; 

    [Header("Select")]
    public AudioClip playerUnitSelect; 
    public AudioClip BaseSelect; 

    [Header("Attack")]
    public AudioClip infantryAttack; 
    public AudioClip heavyInfantryAttack; 
    public AudioClip artilleryAttack; 

    [Header("AI Events")] 
    public AudioClip aiUnitTurnStart; 
    public AudioClip commanderSpawnUnit; 

    [Header("Player Events")] // NUEVO
    public AudioClip playerSpawnUnit; // Arrastra aquí 'spawn2.mp3'

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}