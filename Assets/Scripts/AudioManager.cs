using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    public AudioClip musicClip; 
    public AudioClip buttonClip; // implementar
    public AudioClip hoverClip; // testear
    public AudioClip moveClip; // testear
    public AudioClip captureAIClip; // testear
    public AudioClip capturePlayerClip; // testear
    public AudioClip victory; // testear
    public AudioClip defeat; // testear
    public AudioClip playerTurnStart; // testear

    [Header("Select")]
    public AudioClip playerUnitSelect; // testear
    public AudioClip BaseSelect; // implementar
    public AudioClip ResourceSelect; // implementar

    [Header("Attack")]
    public AudioClip infantryAttack; // testear
    public AudioClip heavyInfantryAttack; // testear
    public AudioClip artilleryAttack; // testear 



    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
}
