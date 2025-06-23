using UnityEngine;
using TMPro;

/// <summary>
/// 매매내역 헤더 로컬라이징 스크립트 (간단 버전)
/// </summary>
public class TradeHistoryHeaderLocalizer : MonoBehaviour
{
    [Header("헤더 텍스트들")]
    public TextMeshProUGUI turnHeaderText;
    public TextMeshProUGUI tradeHeaderText;
    public TextMeshProUGUI stockNameHeaderText;
    public TextMeshProUGUI quantityHeaderText;
    public TextMeshProUGUI priceHeaderText;
    public TextMeshProUGUI feeHeaderText;
    public TextMeshProUGUI totalPriceHeaderText;

    void Start()
    {
        LocalizeHeaders();
    }

    /// <summary>
    /// 헤더들을 현재 언어에 맞게 로컬라이징
    /// </summary>
    public void LocalizeHeaders()
    {
        var loc = CSVLocalizationManager.Instance;

        // 각 헤더 텍스트 설정 (기본값 포함)
        if (turnHeaderText != null)
            turnHeaderText.text = loc?.GetLocalizedText("ui_turn") ?? "턴";

        if (tradeHeaderText != null)
            tradeHeaderText.text = loc?.GetLocalizedText("ui_trade") ?? "거래";

        if (stockNameHeaderText != null)
            stockNameHeaderText.text = loc?.GetLocalizedText("ui_stock_name") ?? "주식이름";

        if (quantityHeaderText != null)
            quantityHeaderText.text = loc?.GetLocalizedText("ui_quantity") ?? "주식갯수";

        if (priceHeaderText != null)
            priceHeaderText.text = loc?.GetLocalizedText("ui_price") ?? "가격";

        if (feeHeaderText != null)
            feeHeaderText.text = loc?.GetLocalizedText("trading_fee_label") ?? "수수료";

        if (totalPriceHeaderText != null)
            totalPriceHeaderText.text = loc?.GetLocalizedText("ui_total_price") ?? "총합";
    }

    /// <summary>
    /// 언어 변경 시 호출
    /// </summary>
    [ContextMenu("헤더 다시 로컬라이징")]
    public void RefreshHeaders()
    {
        LocalizeHeaders();
    }
}