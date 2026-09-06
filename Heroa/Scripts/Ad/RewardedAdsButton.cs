using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using System;
using TMPro;
using System.Collections.Generic;

public class RewardedAdsButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] Button _showAdButton;
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    string _adUnitId = null; // This will remain null for unsupported platforms

    // timer
    [SerializeField] private float _cooldownHours = 2f; // 쿨타임 (시간)
    private static readonly string PREF_KEY_NEXT_AVAILABLE = "RewardedAdNextAvailableUtc";
    private DateTime _nextAvailableUtc;


    [Header("Cooldown UI")]
    [SerializeField] private TextMeshProUGUI _cooldownText;   // 남은 시간 표시할 TMP 텍스트 (선택)
    [SerializeField] private bool _showSeconds = true;        // 초까지 표시할지
    private Coroutine _tickRoutine;

    void Awake()
    {
        // Get the Ad Unit ID for the current platform:
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
#endif

        // Disable the button until the ad is ready to show:
        _showAdButton.interactable = false;

        // timer
        LoadCooldownFromPrefs();
        UpdateButtonInteractable();
        StartTickIfNeeded();

        if (IsOnCooldown() == false)
        {
            LoadAd();
        }
    }

    // Call this public method when you want to get an ad ready to show.
    public void LoadAd()
    {
        // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
        Debug.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    // If the ad successfully loads, add a listener to the button and enable it:
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);

        if (adUnitId.Equals(_adUnitId))
        {
            // Configure the button to call the ShowAd() method when clicked:
            _showAdButton.onClick.AddListener(ShowAd);
            // Enable the button for users to click:
            _showAdButton.interactable = true;
        }
    }

    // Implement a method to execute when the user clicks the button:
    public void ShowAd()
    {
        // Disable the button:
        _showAdButton.interactable = false;
        // Then show the ad:
        Advertisement.Show(_adUnitId, this);
    }

    // Implement the Show Listener's OnUnityAdsShowComplete callback method to determine if the user gets a reward:
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            // Grant a reward.
            GrantReward();
        }
    }

    // Implement Load and Show Listener error callbacks:
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try to load another ad.
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try to load another ad.
    }

    public void OnUnityAdsShowStart(string adUnitId) {
        // timer
        _nextAvailableUtc = DateTime.UtcNow.AddHours(_cooldownHours);
        PlayerPrefs.SetString(PREF_KEY_NEXT_AVAILABLE, _nextAvailableUtc.Ticks.ToString());
        PlayerPrefs.Save();

        UpdateButtonInteractable();
        StartTickIfNeeded(); // 즉시 카운트다운 시작
    }
    public void OnUnityAdsShowClick(string adUnitId) { }

    void OnDestroy()
    {
        // Clean up the button listeners:
        _showAdButton.onClick.RemoveAllListeners();
    }

    public void GrantReward()
    {
        DatabaseManager DB = DatabaseManager.instance;

        DB.PlusGold(300);
        GameManager.instance.ShowAlarmPanel("300원 증정!");
        
    }

    #region Tiemr
    private bool IsOnCooldown()
    {
        return DateTime.UtcNow < _nextAvailableUtc;
    }

    private void LoadCooldownFromPrefs()
    {
        string ticksStr = PlayerPrefs.GetString(PREF_KEY_NEXT_AVAILABLE, "");
        if (!string.IsNullOrEmpty(ticksStr) && long.TryParse(ticksStr, out long ticks))
        {
            _nextAvailableUtc = new DateTime(ticks, DateTimeKind.Utc);
        }
        else
        {
            _nextAvailableUtc = DateTime.MinValue;
        }
    }

    private void UpdateButtonInteractable()
    {
        _showAdButton.interactable = !IsOnCooldown();
    }

    #endregion




    void OnEnable()
    {
        StartTickIfNeeded();
    }

    void OnDisable()
    {
        StopTick();
    }

    private void UpdateCooldownText()
    {
        if (_cooldownText == null) return;

        if (!IsOnCooldown())
        {
            _cooldownText.text = ""; // 또는 "광고 시청 가능"
            return;
        }

        TimeSpan remain = _nextAvailableUtc - DateTime.UtcNow;
        if (remain.TotalSeconds < 0) remain = TimeSpan.Zero;

        // HH:MM(:SS) 형태
        string fmt = _showSeconds ? @"hh\:mm\:ss" : @"hh\:mm";
        _cooldownText.text = remain.ToString(fmt);
    }

    // [추가] 주기적으로 UI 갱신 + 쿨 종료 시 버튼 자동 활성
    private void StartTickIfNeeded()
    {
        if (_tickRoutine == null)
            _tickRoutine = StartCoroutine(Tick());
    }

    private void StopTick()
    {
        if (_tickRoutine != null)
        {
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }
    }

    #region ShowTimer

    private System.Collections.IEnumerator Tick()
    {
        while (true)
        {
            bool wasOnCooldown = IsOnCooldown();
            UpdateCooldownText();

            if (!wasOnCooldown)
            {
                // 쿨타임 끝났으면 버튼 활성 & 종료
                LoadAd();
                _showAdButton.interactable = true;
                _tickRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
#endregion

}