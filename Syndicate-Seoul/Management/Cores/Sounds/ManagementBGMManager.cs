using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Management 씬 BGM 매니저.
/// Resources/Audio/BGM/Management 폴더의 오디오 클립을 순서대로 반복 재생한다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ManagementBGMManager : MonoBehaviour
{
    private const string ManagementSceneName = "Management";

    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource audioSource;
    private AudioClip[] clips;
    private int currentIndex = 0;
    private Coroutine playRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        SoundManager.GetOrCreate().ApplyBgmSource(audioSource);
        ApplyVolume();

        clips = Resources.LoadAll<AudioClip>("Audio/BGM/Management");

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("ManagementBGMManager: Resources/Audio/BGM/Management 에 오디오 클립이 없습니다.");
            return;
        }

        if (SceneManager.GetActiveScene().name == ManagementSceneName)
        {
            StartBgmIfNeeded();
        }
        else
        {
            StopBgm();
        }
    }

    private void OnValidate()
    {
        if (audioSource != null)
            audioSource.volume = volume;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SoundManager.GetOrCreate().SettingsChanged += ApplyVolume;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (SoundManager.Instance != null)
            SoundManager.Instance.SettingsChanged -= ApplyVolume;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (SoundManager.Instance != null)
            SoundManager.Instance.SettingsChanged -= ApplyVolume;
    }

    private void ApplyVolume()
    {
        if (audioSource == null)
            return;

        SoundManager manager = SoundManager.GetOrCreate();
        audioSource.volume = manager != null
            ? manager.GetBgmSourceVolume(volume)
            : volume;
    }

    private IEnumerator PlaySequential()
    {
        while (true)
        {
            AudioClip clip = clips[currentIndex];
            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitForSeconds(clip.length);

            currentIndex = (currentIndex + 1) % clips.Length;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != ManagementSceneName)
        {
            StopBgm();
            return;
        }

        StartBgmIfNeeded();
    }

    private void StartBgmIfNeeded()
    {
        if (playRoutine != null)
        {
            Debug.Log("[ManagementBGMManager] Management BGM is already playing.");
            return;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning("[ManagementBGMManager] AudioSource is missing. Cannot start management BGM.");
            return;
        }

        if (clips == null || clips.Length == 0)
            clips = Resources.LoadAll<AudioClip>("Audio/BGM/Management");

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[ManagementBGMManager] Management BGM clips are missing. Cannot start management BGM.");
            return;
        }

        SoundManager.GetOrCreate().ApplyBgmSource(audioSource);
        ApplyVolume();
        audioSource.enabled = true;
        audioSource.mute = false;
        playRoutine = StartCoroutine(PlaySequential());
    }

    private void StopBgm()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }
}
