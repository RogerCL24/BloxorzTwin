using UnityEngine;

// Simple global audio manager. Add once in the scene (or it will auto-create itself).
public class GlobalAudio : MonoBehaviour
{
    public static GlobalAudio Instance { get; private set; }

    [Header("Clips")]
    public AudioClip[] moveClips;
    public AudioClip bridgeToggleClip;
    public AudioClip mapChangeClip;

    [Header("Levels")]
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D
    }

    public void PlayMove()
    {
        if (moveClips == null || moveClips.Length == 0) return;
        int idx = Random.Range(0, moveClips.Length);
        PlayOneShot(moveClips[idx]);
    }

    public void PlayBridgeToggle()
    {
        if (bridgeToggleClip == null) return;
        PlayOneShot(bridgeToggleClip);
    }

    public void PlayMapChange()
    {
        if (mapChangeClip == null) return;
        PlayOneShot(mapChangeClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        audioSource.PlayOneShot(clip, sfxVolume);
    }
}
