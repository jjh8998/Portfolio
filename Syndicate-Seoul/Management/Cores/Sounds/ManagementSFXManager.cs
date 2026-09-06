using UnityEngine;

/// <summary>
/// Management 씬 SFX 매니저.
/// Resources/Audio/SFX/Management 폴더의 효과음을 로드하여 재생한다.
/// </summary>
public class ManagementSFXManager : MonoBehaviour
{
    public static ManagementSFXManager Instance { get; private set; }

    [Header("SFX 개별 볼륨")]
    [Range(0f, 1f)] public float hoverVolume = 0.7f;
    [Range(0f, 1f)] public float arrowVolume = 0.7f;
    [Range(0f, 1f)] public float addToDeckVolume = 0.8f;
    [Range(0f, 1f)] public float removeFromDeckVolume = 0.8f;
    [Range(0f, 1f)] public float negativeVolume = 0.8f;
    [Range(0f, 1f)] public float deckBuilderOpenVolume = 0.8f;
    [SerializeField] private AudioClip cityClickClip;
    [Range(0f, 1f)] public float cityClickVolume = 0.8f;

    private AudioSource audioSource;
    private AudioClip hoverClip;
    private AudioClip arrowClip;
    private AudioClip addToDeckClip;
    private AudioClip removeFromDeckClip;
    private AudioClip negativeClip;
    private AudioClip deckBuilderOpenClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // BGMManager와 AudioSource를 공유하지 않도록 별도 추가
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        SoundManager.GetOrCreate().ApplySfxSource(audioSource);

        hoverClip          = Resources.Load<AudioClip>("Audio/SFX/Management/UI/버튼에 마우스 갖다댈때 소리");
        arrowClip          = Resources.Load<AudioClip>("Audio/SFX/Management/UI/화살표 클릭");
        addToDeckClip      = Resources.Load<AudioClip>("Audio/SFX/Management/Deck/덱에 카드 삽입");
        removeFromDeckClip = Resources.Load<AudioClip>("Audio/SFX/Management/Deck/덱에서 카드 제거");
        negativeClip       = Resources.Load<AudioClip>("Audio/SFX/Management/Feedback/조건 미충족,혹은 부정적 상황");
        deckBuilderOpenClip = Resources.Load<AudioClip>("Audio/SFX/Management/UI/덱빌더 열릴때");

        if (hoverClip == null)          Debug.LogWarning("[ManagementSFXManager] 'SFX/버튼에 마우스 갖다댈때 소리' 클립을 찾을 수 없습니다.");
        if (arrowClip == null)          Debug.LogWarning("[ManagementSFXManager] 'SFX/화살표 클릭' 클립을 찾을 수 없습니다.");
        if (addToDeckClip == null)      Debug.LogWarning("[ManagementSFXManager] 'SFX/덱에 카드 삽입' 클립을 찾을 수 없습니다.");
        if (removeFromDeckClip == null) Debug.LogWarning("[ManagementSFXManager] 'SFX/덱에서 카드 제거' 클립을 찾을 수 없습니다.");
        if (negativeClip == null)       Debug.LogWarning("[ManagementSFXManager] 'SFX/조건 미충족,혹은 부정적 상황' 클립을 찾을 수 없습니다.");
        if (deckBuilderOpenClip == null) Debug.LogWarning("[ManagementSFXManager] 'SFX/덱빌더 열릴때' 클립을 찾을 수 없습니다.");
    }

    /// <summary>버튼 위에 마우스를 올렸을 때 재생</summary>
    public void PlayHover()
    {
        Play(hoverClip, hoverVolume);
    }

    /// <summary>화살표(페이지 넘기기) 버튼 클릭 시 재생</summary>
    public void PlayArrow()
    {
        Play(arrowClip, arrowVolume);
    }

    /// <summary>보유 중인 카드를 덱에 추가할 때 재생</summary>
    public void PlayAddToDeck()
    {
        Play(addToDeckClip, addToDeckVolume);
    }

    /// <summary>덱에서 카드를 제거할 때 재생</summary>
    public void PlayRemoveFromDeck()
    {
        Play(removeFromDeckClip, removeFromDeckVolume);
    }

    /// <summary>미보유 카드 클릭 등 조건 미충족 시 재생</summary>
    public void PlayNegative()
    {
        Play(negativeClip, negativeVolume);
    }

    /// <summary>덱 빌더 패널이 열릴 때 재생</summary>
    public void PlayDeckBuilderOpen()
    {
        Play(deckBuilderOpenClip, deckBuilderOpenVolume);
    }

    public void PlayCityClick()
    {
        Play(cityClickClip, cityClickVolume);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        SoundManager manager = SoundManager.GetOrCreate();
        if (manager != null)
        {
            manager.PlaySfx(clip, volume);
            return;
        }

        if (audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
