using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static GameHistoryManager;

/// <summary>
/// 자산변화 아이템 UI 컴포넌트
/// </summary>
public class AssetChangeItemUI : MonoBehaviour
{
    [Header("UI 컴포넌트들")]
    public TextMeshProUGUI turnNumberText;      // "3턴"
    public TextMeshProUGUI assetAmountText;     // "₩104만원"
    public TextMeshProUGUI changeAmountText;    // "+₩4만원"
    public TextMeshProUGUI changeLabelText;     // "변화"
    public Image trendIcon;                     // 상승/하락 아이콘

    [Header("색상 설정")]
    public Color profitColor = Color.green;     // 수익 색상
    public Color lossColor = Color.red;         // 손실 색상
    public Color neutralColor = Color.gray;     // 중립 색상

    private TurnSnapshot turnData;

    /// <summary>
    /// 턴 스냅샷 데이터 설정
    /// </summary>
    public void SetData(TurnSnapshot snapshot)
    {
        turnData = snapshot;
        UpdateUI();
    }

    /// <summary>
    /// UI 업데이트
    /// </summary>
    void UpdateUI()
    {
        if (turnData == null) return;

        var loc = CSVLocalizationManager.Instance;

        // 턴 번호
        if (turnNumberText != null)
        {
            string turnFormat = loc?.GetLocalizedText("result_turn_format") ?? "{0}턴";
            turnNumberText.text = string.Format(turnFormat, turnData.turnNumber);
        }

        // 자산 금액
        if (assetAmountText != null)
        {
            string currencyFormat = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
            assetAmountText.text = string.Format(currencyFormat, (int)turnData.totalAssets);
        }

        // 변화 금액 계산 및 표시
        UpdateChangeAmount();

        // 변화 라벨
        if (changeLabelText != null)
        {
            changeLabelText.text = loc?.GetLocalizedText("ui_change") ?? "변화";
        }
    }

    /// <summary>
    /// 변화 금액 업데이트
    /// </summary>
    void UpdateChangeAmount()
    {
        if (changeAmountText == null) return;

        // 이전 턴과의 차이 계산 (임시로 초기자금 기준)
        float previousAmount = turnData.turnNumber == 1 ? 1000000f : turnData.totalAssets;
        float change = turnData.totalAssets - (turnData.turnNumber == 1 ? 1000000f : previousAmount);

        var loc = CSVLocalizationManager.Instance;
        string currencyFormat = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";

        if (change > 0)
        {
            changeAmountText.text = "+" + string.Format(currencyFormat, (int)change);
            changeAmountText.color = profitColor;
        }
        else if (change < 0)
        {
            changeAmountText.text = string.Format(currencyFormat, (int)change);
            changeAmountText.color = lossColor;
        }
        else
        {
            changeAmountText.text = "±" + string.Format(currencyFormat, 0);
            changeAmountText.color = neutralColor;
        }

        // 트렌드 아이콘 업데이트
        UpdateTrendIcon(change);
    }

    /// <summary>
    /// 트렌드 아이콘 업데이트
    /// </summary>
    void UpdateTrendIcon(float change)
    {
        if (trendIcon == null) return;

        if (change > 0)
        {
            trendIcon.color = profitColor;
            // 상승 아이콘으로 변경 (스프라이트가 있다면)
        }
        else if (change < 0)
        {
            trendIcon.color = lossColor;
            // 하락 아이콘으로 변경
        }
        else
        {
            trendIcon.color = neutralColor;
            // 보합 아이콘으로 변경
        }
    }
}