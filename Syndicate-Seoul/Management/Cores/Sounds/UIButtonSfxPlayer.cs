using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonSfxPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private Button button;
    private AudioSource fallbackAudioSource;

    private void Awake()
    {
        ResolveButton();
        ResolveFallbackAudioSource();
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
    }

    private void OnDestroy()
    {
        UnbindButton();
    }

    public void PlayClickSfx()
    {
        if (clickClip == null)
            return;

        SoundManager manager = SoundManager.GetOrCreate();
        if (manager != null)
        {
            manager.PlaySfx(clickClip, volume);
            return;
        }

        ResolveFallbackAudioSource();
        if (fallbackAudioSource != null)
            fallbackAudioSource.PlayOneShot(clickClip, volume);
    }

    private void BindButton()
    {
        ResolveButton();

        if (button == null)
            return;

        button.onClick.RemoveListener(PlayClickSfx);
        button.onClick.AddListener(PlayClickSfx);
    }

    private void UnbindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(PlayClickSfx);
    }

    private void ResolveButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void ResolveFallbackAudioSource()
    {
        if (fallbackAudioSource == null)
            fallbackAudioSource = GetComponent<AudioSource>();

        if (fallbackAudioSource == null)
            fallbackAudioSource = gameObject.AddComponent<AudioSource>();

        fallbackAudioSource.playOnAwake = false;
        fallbackAudioSource.loop = false;
    }
}
