using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerInfoUIScript : MonoBehaviour
{
    public FactionManager factionManager;
    public TMP_Text creditText;
    public TMP_Text rpText;
    public TMP_Text powerText;

    private static readonly Color powerWarningColor = new Color(1f, 0.55f, 0f);
    private Color defaultPowerTextColor;
    private bool hasDefaultPowerTextColor;

    void OnEnable()
    {
        if (factionManager == null) return;

        factionManager.CreditChanged += UpdateCreditText;
        factionManager.RPChanged += OnRPChanged;
        factionManager.PowerChanged += OnPowerChanged;
        factionManager.ResearchStateChanged += RefreshResearchStateText;
        
        UpdateCreditText(factionManager.GetCredit);
        UpdateRPText(factionManager.GetRPIncome);
        UpdatePowerText(factionManager.GetTotalPowerProduction, factionManager.GetTotalPowerConsumption, factionManager.GetNetPower);
    }

    void OnDisable()
    {
        if (factionManager == null) return;

        factionManager.CreditChanged -= UpdateCreditText;
        factionManager.RPChanged -= OnRPChanged;
        factionManager.PowerChanged -= OnPowerChanged;
        factionManager.ResearchStateChanged -= RefreshResearchStateText;
    }

    public void RefreshNow()
    {
        if (factionManager == null)
            return;

        UpdateCreditText(factionManager.GetCredit);
        UpdateRPText(factionManager.GetRPIncome);
        UpdatePowerText(factionManager.GetTotalPowerProduction, factionManager.GetTotalPowerConsumption, factionManager.GetNetPower);
    }

    void UpdateCreditText(int _credit)
    {
        if (creditText == null) return;

        int creditIncome = factionManager != null
            ? ManagementResourceCalculator.CalculateFaction(factionManager).creditIncome
            : 0;
        string incomeText = creditIncome >= 0 ? $"+{creditIncome:N0}" : $"{creditIncome:N0}";

        creditText.text = $"{_credit:N0} ({incomeText})";
        ManagementUIDesignSystem.StyleResourceValue(creditText);
    }

    void RefreshCreditText()
    {
        if (factionManager == null) return;

        UpdateCreditText(factionManager.GetCredit);
    }

    void UpdateRPText(int _rp)
    {
        if (rpText == null) return;

        int rpIncome = factionManager != null ? factionManager.GetRPIncome : _rp;
        rpText.text = $"+{rpIncome:N0}";
        ManagementUIDesignSystem.StyleResourceValue(rpText);
    }

    void RefreshRPText()
    {
        if (factionManager == null) return;
        UpdateRPText(factionManager.GetRPIncome);
    }

    void RefreshResearchStateText()
    {
        RefreshCreditText();
        RefreshRPText();
    }

    void OnRPChanged(int _rp)
    {
        UpdateRPText(_rp);
        RefreshCreditText();
    }

    void OnPowerChanged(int _production, int _consumption, int _netPower)
    {
        UpdatePowerText(_production, _consumption, _netPower);
        RefreshCreditText();
        RefreshRPText();
    }

    void UpdatePowerText(int _production, int _consumption, int _netPower)
    {
        if (powerText == null) return;

        int offset = factionManager != null ? factionManager.GetTradePowerOffset : 0;
        int totalPower = _production + offset;
        string offsetText = offset > 0 ? $"(+{offset})" : string.Empty;
        float powerSatisfactionRate = ManagementResourceCalculator.CalculatePowerSatisfactionRate(totalPower, _consumption);
        string satisfactionText = powerSatisfactionRate < 1f
            ? $" ({Mathf.FloorToInt(powerSatisfactionRate * 100f)}%)"
            : string.Empty;
        powerText.text = $"{_consumption} / {totalPower}{offsetText}{satisfactionText}";
        ManagementUIDesignSystem.StyleResourceValue(powerText);
        ApplyPowerTextColor(totalPower, _consumption);
    }

    private void ApplyPowerTextColor(int _production, int _consumption)
    {
        if (powerText == null)
            return;

        if (!hasDefaultPowerTextColor)
        {
            defaultPowerTextColor = powerText.color;
            hasDefaultPowerTextColor = true;
        }

        if (_consumption <= 0 || _production >= _consumption)
        {
            powerText.color = defaultPowerTextColor;
            return;
        }

        float shortageRatio = (float)(_consumption - _production) / _consumption;
        powerText.color = shortageRatio > 0.5f ? Color.red : powerWarningColor;
    }
}
