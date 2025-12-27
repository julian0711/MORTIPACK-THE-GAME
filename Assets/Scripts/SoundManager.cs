using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip defaultBGM;

    [Header("SE Settings")]
    [SerializeField] private AudioSource seSource;

    [Header("Audio Clips (SE)")]
    // General Interaction
    public AudioClip searchSE;  // Nothing found
    public AudioClip getSE;     // Item obtained
    public AudioClip walkSE;
    public AudioClip bumpSE;
    
    // Additional SEs found in folder
    public AudioClip doorSE;
    public AudioClip deadSE;
    public AudioClip warpSE;
    public AudioClip radioSE;
    public AudioClip radarSE;
    public AudioClip migawariSE;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Auto-create sources if missing
            if (bgmSource == null) 
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
            if (seSource == null)
            {
                seSource = gameObject.AddComponent<AudioSource>();
                seSource.loop = false;
                seSource.playOnAwake = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return; // Already playing
        
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySE(AudioClip clip)
    {
        if (clip != null)
        {
            seSource.PlayOneShot(clip);
        }
    }
   
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-assign known clips from Assets/AssetsAudioBGM
        if (searchSE == null) searchSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/searchSE.mp3");
        if (getSE == null) getSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/getSE.mp3");
        
        if (doorSE == null) doorSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/doorSE.mp3");
        if (deadSE == null) deadSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/deadSE.mp3");
        if (warpSE == null) warpSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/warpSE.mp3");
        if (radioSE == null) radioSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/radioSE.mp3");
        if (radarSE == null) radarSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/radarSE.mp3");
        if (migawariSE == null) migawariSE = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/migawariSE.mp3");
        
        // BGM
        if (defaultBGM == null) defaultBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AssetsAudioBGM/stageBGM01.mp3");
    }
#endif
}
