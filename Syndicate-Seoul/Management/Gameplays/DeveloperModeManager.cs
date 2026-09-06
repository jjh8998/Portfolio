using UnityEngine;
using UnityEngine.UI;

public class DeveloperModeManager : MonoBehaviour
{
    [Header("War Options")]
    [SerializeField] private bool allowWarWithoutShare;
    [SerializeField] private bool playerAsDefenderWarTest;
    [SerializeField] private bool blockAIWarDeclarationToPlayer;

    [Header("Resource Target")]
    [SerializeField] private FactionManager target;

    [Header("Resource Amounts")]
    [SerializeField] private int creditAmount;
    [SerializeField] private int rpAmount;
    [SerializeField] private int powerAmount;

    [Header("Resource Buttons")]
    [SerializeField] private Button applyResourceButton;
    [SerializeField] private Button resetResourceButton;

    public bool AllowWarWithoutShare => allowWarWithoutShare;
    public bool PlayerAsDefenderWarTest => playerAsDefenderWarTest;
    public bool BlockAIWarDeclarationToPlayer => blockAIWarDeclarationToPlayer;

    private bool hasOriginalResources;
    private FactionManager originalTarget;
    private int originalCredit;
    private int originalRPStock;
    private int originalTradePowerOffset;

    private void OnEnable()
    {
        BindResourceButtons();
    }

    private void OnDisable()
    {
        UnbindResourceButtons();
    }

    public void SetAllowWarWithoutShare(bool _isOn)
    {
        allowWarWithoutShare = _isOn;
        Debug.Log($"[DeveloperModeManager] Allow war without share: {allowWarWithoutShare}");
    }

    public void ToggleAllowWarWithoutShare()
    {
        SetAllowWarWithoutShare(!allowWarWithoutShare);
    }

    public void SetPlayerAsDefenderWarTest(bool _isOn)
    {
        playerAsDefenderWarTest = _isOn;
        Debug.Log($"[DeveloperModeManager] Player as defender war test: {playerAsDefenderWarTest}");
    }

    public void TogglePlayerAsDefenderWarTest()
    {
        SetPlayerAsDefenderWarTest(!playerAsDefenderWarTest);
    }

    public void SetBlockAIWarDeclarationToPlayer(bool _isOn)
    {
        blockAIWarDeclarationToPlayer = _isOn;
        Debug.Log($"[DeveloperModeManager] Block AI war declaration to player: {blockAIWarDeclarationToPlayer}");
    }

    public void ToggleBlockAIWarDeclarationToPlayer()
    {
        SetBlockAIWarDeclarationToPlayer(!blockAIWarDeclarationToPlayer);
    }

    public void ApplyDeveloperResources()
    {
        if (target == null)
        {
            Debug.LogWarning("[DeveloperModeManager] Target faction is null.");
            return;
        }

        if (!hasOriginalResources)
            SaveOriginalResources(target);
        else if (originalTarget != target)
        {
            Debug.LogWarning("[DeveloperModeManager] Reset developer resources before changing target faction.");
            return;
        }

        int creditDelta = Mathf.Max(creditAmount, -target.GetCredit);
        if (creditDelta != 0 && !target.ChangeCredit(creditDelta))
            Debug.LogWarning("[DeveloperModeManager] Failed to change target credit.");

        int nextRPStock = Mathf.Max(0, target.GetRPStock + rpAmount);
        target.ForceSetRPStock(nextRPStock);

        int powerDelta = Mathf.Max(powerAmount, -target.GetTradePowerOffset);
        if (powerDelta != 0)
            target.ForceChangeTradePower(powerDelta);
    }

    public void ResetDeveloperResources()
    {
        if (!hasOriginalResources)
            return;

        if (originalTarget == null)
        {
            Debug.LogWarning("[DeveloperModeManager] Original target faction is null.");
            ClearOriginalResources();
            return;
        }

        int creditDelta = originalCredit - originalTarget.GetCredit;
        if (creditDelta != 0 && !originalTarget.ChangeCredit(creditDelta))
            Debug.LogWarning("[DeveloperModeManager] Failed to reset target credit.");

        originalTarget.ForceSetRPStock(originalRPStock);

        int powerDelta = originalTradePowerOffset - originalTarget.GetTradePowerOffset;
        if (powerDelta != 0)
            originalTarget.ForceChangeTradePower(powerDelta);

        ClearOriginalResources();
    }

    private void BindResourceButtons()
    {
        if (applyResourceButton != null)
        {
            applyResourceButton.onClick.RemoveListener(ApplyDeveloperResources);
            applyResourceButton.onClick.AddListener(ApplyDeveloperResources);
        }
        else
        {
            Debug.LogWarning("[DeveloperModeManager] Apply resource button is null.");
        }

        if (resetResourceButton != null)
        {
            resetResourceButton.onClick.RemoveListener(ResetDeveloperResources);
            resetResourceButton.onClick.AddListener(ResetDeveloperResources);
        }
        else
        {
            Debug.LogWarning("[DeveloperModeManager] Reset resource button is null.");
        }
    }

    private void UnbindResourceButtons()
    {
        if (applyResourceButton != null)
            applyResourceButton.onClick.RemoveListener(ApplyDeveloperResources);

        if (resetResourceButton != null)
            resetResourceButton.onClick.RemoveListener(ResetDeveloperResources);
    }

    private void SaveOriginalResources(FactionManager _target)
    {
        originalTarget = _target;
        originalCredit = _target.GetCredit;
        originalRPStock = _target.GetRPStock;
        originalTradePowerOffset = _target.GetTradePowerOffset;
        hasOriginalResources = true;
    }

    private void ClearOriginalResources()
    {
        originalTarget = null;
        originalCredit = 0;
        originalRPStock = 0;
        originalTradePowerOffset = 0;
        hasOriginalResources = false;
    }
}
