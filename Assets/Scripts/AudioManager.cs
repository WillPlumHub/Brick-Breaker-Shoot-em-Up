using UnityEngine;

public class AudioManager : MonoBehaviour {

    public static AudioManager Instance;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip _backgroundMusic;


    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
        } else {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        InitializeAudio();
    }

    private void InitializeAudio() {
        if (_musicSource == null || _backgroundMusic == null) {
            Debug.LogError("AudioManager: Missing audio references!");
        } else {
            _musicSource.clip = _backgroundMusic;
            _musicSource.Play();
        }
    }
}