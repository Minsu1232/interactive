using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 매매내역 아이템 UI 컴포넌트
/// 프리팹에 부착해서 사용
/// </summary>
public class TradeHistoryItemUI : MonoBehaviour
{
    [Header("UI 컴포넌트들")]
    public TextMeshProUGUI turnNumberText;      // "1턴"
    public TextMeshProUGUI tradeTypeText;       // "매수" / "매도"
    public Image tradeTypeIcon;                 // 매수/매도 아이콘 (빨강/파랑)
    public TextMeshProUGUI stockNameText;       // "SmartTech"
    public TextMeshProUGUI quantityText;        // "5주"
    public TextMeshProUGUI priceText;           // "₩45,000"
    public TextMeshProUGUI feeText;           // priceText * 0.25% (수수료)
    public TextMeshProUGUI totalAmountText;     // "₩225,563" totalAmountText = priceText - feeText

    /// <summary>
    /// 매매내역 데이터 설정
    /// </summary>
    public void SetData(TradeRecord trade)
    {
        var loc = CSVLocalizationManager.Instance;

        // 턴 번호
        if (turnNumberText != null)
        {
            if(trade.turnNumber >= 11)
            {
                Debug.LogWarning("Invalid turn number: " + trade.turnNumber);
                trade.turnNumber = 10; // 기본값으로 설정
            }
            string turnFormat = loc?.GetLocalizedText("result_turn_format") ?? "{0}턴";
            turnNumberText.text = string.Format(turnFormat, trade.turnNumber);
        }

        // 매수/매도 구분
        if (tradeTypeText != null)
        {
            string tradeTypeKey = trade.tradeType == TradeType.Buy ? "button_buy" : "button_sell";
            tradeTypeText.text = loc?.GetLocalizedText(tradeTypeKey) ??
                                (trade.tradeType == TradeType.Buy ? "매수" : "매도");
            tradeTypeText.color = trade.tradeType == TradeType.Buy ? Color.red : Color.blue;
            tradeTypeIcon.color = trade.tradeType == TradeType.Buy ? Color.red : Color.blue;
        }

        // 종목명
        if (stockNameText != null)
            stockNameText.text = trade.stockName;

        // 수량
        if (quantityText != null)
        {
            string quantityFormat = loc?.GetLocalizedText("trade_quantity_format") ?? "{0}주";
            quantityText.text = string.Format(quantityFormat, trade.quantity);
        }

        // 단가
        if (priceText != null)
        {
            string currencyFormat = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
            priceText.text = string.Format(currencyFormat, trade.price);
        }
        if (feeText != null)
        {
            float feeRate = GameManager.Instance?.TradingFeeRate ?? 0.25f;
            int totalTradeAmount = trade.price * trade.quantity;
            int fee = Mathf.RoundToInt(totalTradeAmount * (feeRate / 100f));

            // ✅ ui_money_format 사용 - 언어별 자동 변환!
            string feeFormat = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
            feeText.text = string.Format(feeFormat, fee);
        }
        // 총 금액 (손익 표시 + 색상)
        if (totalAmountText != null)
        {
            // 수수료율 가져오기
            float feeRate = GameManager.Instance?.TradingFeeRate ?? 0.25f;

            // 기본 거래금액과 수수료 계산
            int baseAmount = trade.price * trade.quantity;
            int fee = Mathf.RoundToInt(baseAmount * (feeRate / 100f));

            int totalAmount;
            string prefix;
            Color textColor;

            if (trade.tradeType == TradeType.Buy)
            {
                // 매수: 지출 (-)
                totalAmount = -(baseAmount + fee);  // 음수로 표시
                prefix = "-";
                textColor = Color.blue;  // 파란색 (지출)
            }
            else
            {
                // 매도: 수입 (+)
                totalAmount = baseAmount - fee;  // 양수로 표시
                prefix = "+";
                textColor = Color.red;   // 빨간색 (수입)
            }

            // 텍스트 색상 설정
            totalAmountText.color = textColor;

            // 금액 포맷팅 (절댓값 사용)
            string currencyFormat = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
            string formattedAmount = string.Format(currencyFormat, Mathf.Abs(totalAmount));

            // 최종 텍스트: +₩123,456 또는 -₩123,456
            totalAmountText.text = prefix + formattedAmount;
        }
    }
}