using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 섹터 성과 아이템 UI 컴포넌트
/// 섹터성과 패널에서 각 섹터의 투자 성과를 표시하는 개별 아이템
/// 구성: [수익률 멘트] [섹터 아이콘] [섹터명] [수익률] [상승/하락 아이콘]
/// 
/// 🎯 수익률별 멘트 시스템:
/// +15% 이상 → "대성공! / Excellent!" 
/// +5~15% → "성공! / Success!"
/// +0~5% → "소폭 수익 / Small Win"
/// -5~0% → "소폭 손실 / Small Loss" 
/// -15~-5% → "손실 / Loss"
/// -15% 이하 → "큰 손실 / Big Loss"
/// </summary>
public class SectorPerformanceItemUI : MonoBehaviour
{
    [Header("UI 컴포넌트들")]
    public TextMeshProUGUI commentText;         // 왼쪽: 수익률별 멘트 (기존 섹터명 대신)
    public Image sectorIcon;                    // 섹터 아이콘 (TECH=파랑, EV=초록 등)
    public TextMeshProUGUI sectorNameText;      // 섹터명 ("기술주", "전기차" 등)
    public TextMeshProUGUI returnRateText;      // 수익률 텍스트 ("-8.3%")
    public TextMeshProUGUI investmentAmountText; // 투자금액 ("1,200,000원")
    public TextMeshProUGUI currentValueText;    // 현재가치 ("1,104,000원")
    public TextMeshProUGUI trendEmoji;          // 상승/하락 트렌드 이모지 (📈📉📊)

    [Header("색상 설정")]
    public Color profitColor = Color.red;     // 수익 색상
    public Color lossColor = Color.blue;         // 손실 색상
    public Color neutralColor = Color.gray;     // 중립 색상

    [Header("섹터별 아이콘 색상")]
    public Color techColor = Color.blue;        // 기술주 색상
    public Color semColor = Color.cyan;         // 반도체 색상
    public Color evColor = Color.green;         // 전기차 색상
    public Color corpColor = Color.yellow;      // 대기업 색상
    public Color cryptoColor = Color.magenta;   // 가상자산 색상

    private SectorPerformance sectorData;       // 섹터 성과 데이터

    /// <summary>
    /// 섹터 성과 데이터 설정 및 UI 업데이트
    /// InvestmentResultManager에서 호출됨
    /// </summary>
    public void SetData(SectorPerformance data)
    {
        sectorData = data;
        UpdateUI();
    }

    /// <summary>
    /// 전체 UI 업데이트
    /// 섹터 데이터를 바탕으로 모든 UI 요소를 갱신
    /// </summary>
    void UpdateUI()
    {
        if (sectorData == null) return;

        // 각 UI 요소별로 업데이트
        UpdateCommentText();        // 수익률별 멘트
        UpdateSectorIcon();         // 섹터 아이콘 색상
        UpdateSectorName();         // 섹터명 (로컬라이징)
        UpdateReturnRateText();     // 수익률 텍스트
        UpdateInvestmentInfo();     // 투자 정보 (금액, 현재가치)
        UpdateTrendEmoji();          // 트렌드 이모지
    }

    /// <summary>
    /// 🆕 수익률별 멘트 업데이트
    /// 기존 섹터명 자리에 수익률에 따른 멘트를 표시
    /// 예: "대성공! / Excellent!", "손실 / Loss" 등
    /// </summary>
    void UpdateCommentText()
    {
        if (commentText == null) return;

        float returnRate = sectorData.returnRate;

        // 수익률에 따른 멘트 생성
        string comment = GetReturnRateComment(returnRate);
        Color commentColor = GetCommentColor(returnRate);

        // UI에 반영
        commentText.text = comment;
        commentText.color = commentColor;
    }

    /// <summary>
    /// 수익률에 따른 멘트 문자열 생성
    /// 로컬라이징을 고려하여 먼저 CSV에서 찾고, 없으면 기본값 사용
    /// </summary>
    string GetReturnRateComment(float returnRate)
    {
        // 로컬라이징 우선 시도
        var loc = CSVLocalizationManager.Instance;
        string commentKey = GetCommentLocalizationKey(returnRate);
        string localizedComment = loc?.GetLocalizedText(commentKey);

        // 로컬라이징된 텍스트가 있으면 사용
        if (!string.IsNullOrEmpty(localizedComment) && localizedComment != commentKey)
        {
            return localizedComment;
        }

        // 폴백: 기본 한글/영어 멘트
        return GetDefaultComment(returnRate);
    }

    /// <summary>
    /// 수익률 구간별 로컬라이징 키 생성
    /// CSV 파일에서 해당 키를 찾아 다국어 지원
    /// </summary>
    string GetCommentLocalizationKey(float returnRate)
    {
        if (returnRate >= 15f) return "sector_comment_excellent";      // "대성공! / Excellent!"
        else if (returnRate >= 5f) return "sector_comment_success";    // "성공! / Success!"
        else if (returnRate >= 0f) return "sector_comment_small_win";  // "소폭 수익 / Small Win"
        else if (returnRate >= -5f) return "sector_comment_small_loss";// "소폭 손실 / Small Loss"
        else if (returnRate >= -15f) return "sector_comment_loss";     // "손실 / Loss"
        else return "sector_comment_big_loss";                        // "큰 손실 / Big Loss"
    }

    /// <summary>
    /// 기본 멘트 반환 (로컬라이징 실패시 폴백)
    /// 확정된 멘트 세트 사용
    /// </summary>
    string GetDefaultComment(float returnRate)
    {
        if (returnRate >= 15f) return "대성공! / Excellent!";
        else if (returnRate >= 5f) return "성공! / Success!";
        else if (returnRate >= 0f) return "소폭 수익 / Small Win";
        else if (returnRate >= -5f) return "소폭 손실 / Small Loss";
        else if (returnRate >= -15f) return "손실 / Loss";
        else return "큰 손실 / Big Loss";
    }

    /// <summary>
    /// 멘트 텍스트 색상 결정
    /// 수익률에 따라 초록(수익), 빨강(손실), 회색(중립) 적용
    /// </summary>
    Color GetCommentColor(float returnRate)
    {
        if (returnRate > 0f)
        {
            // 수익: 초록색 계열
            return profitColor;
        }
        else if (returnRate < 0f)
        {
            // 손실: 빨간색 계열
            return lossColor;
        }
        else
        {
            // 무손익: 회색
            return neutralColor;
        }
    }

    /// <summary>
    /// 섹터 아이콘 업데이트
    /// 섹터별로 다른 색상 적용 (TECH=파랑, EV=초록 등)
    /// </summary>
    void UpdateSectorIcon()
    {
        if (sectorIcon == null) return;

        // 섹터별 색상 적용
        sectorIcon.color = GetSectorIconColor(sectorData.sector);
    }

    /// <summary>
    /// 섹터명 업데이트
    /// 로컬라이징을 통해 다국어 섹터명 표시
    /// </summary>
    void UpdateSectorName()
    {
        if (sectorNameText == null) return;

        var loc = CSVLocalizationManager.Instance;
        string sectorKey = GetSectorLocalizationKey(sectorData.sector);
        string localizedName = loc?.GetLocalizedText(sectorKey) ?? sectorData.sector.ToString();

        sectorNameText.text = localizedName;
    }

    /// <summary>
    /// 수익률 텍스트 업데이트
    /// "+12.5%" 또는 "-8.3%" 형식으로 표시
    /// </summary>
    void UpdateReturnRateText()
    {
        if (returnRateText == null) return;

        // 수익률 텍스트 포맷팅 (부호 포함)
        returnRateText.text = $"{sectorData.returnRate:+0.0;-0.0}%";

        // 수익률에 따른 색상 적용
        returnRateText.color = sectorData.returnRate >= 0 ? profitColor : lossColor;
    }

    /// <summary>
    /// 투자 정보 업데이트 (투자금액, 현재가치)
    /// 필요에 따라 사용하는 옵션 UI
    /// </summary>
    void UpdateInvestmentInfo()
    {
        var loc = CSVLocalizationManager.Instance;
        string currencyFormat = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";

        // 투자금액 표시
        if (investmentAmountText != null)
        {
            investmentAmountText.text = string.Format(currencyFormat, (int)sectorData.investedAmount);
        }

        // 현재가치 표시
        if (currentValueText != null)
        {
            currentValueText.text = string.Format(currencyFormat, (int)sectorData.currentValue);
            currentValueText.color = sectorData.returnRate >= 0 ? profitColor : lossColor;
        }
    }

    /// <summary>
    /// 트렌드 이모지 업데이트
    /// 상승(📈), 하락(📉), 보합(📊) 이모지로 트렌드 표시
    /// </summary>
    void UpdateTrendEmoji()
    {
        if (trendEmoji == null) return;

        if (sectorData.returnRate > 5f)
        {
            // 큰 상승: 📈 (초록색)
            trendEmoji.text = "📈";
            trendEmoji.color = profitColor;
        }
        else if (sectorData.returnRate > 0f)
        {
            // 소폭 상승: 📊 (초록색)
            trendEmoji.text = "📊";
            trendEmoji.color = profitColor;
        }
        else if (sectorData.returnRate < -5f)
        {
            // 큰 하락: 📉 (빨간색)
            trendEmoji.text = "📉";
            trendEmoji.color = lossColor;
        }
        else if (sectorData.returnRate < 0f)
        {
            // 소폭 하락: 📊 (빨간색)
            trendEmoji.text = "📊";
            trendEmoji.color = lossColor;
        }
        else
        {
            // 보합: 📊 (회색)
            trendEmoji.text = "📊";
            trendEmoji.color = neutralColor;
        }
    }

    /// <summary>
    /// 섹터별 아이콘 색상 반환
    /// 각 섹터마다 고유한 색상으로 구분
    /// </summary>
    Color GetSectorIconColor(StockSector sector)
    {
        return sector switch
        {
            StockSector.TECH => techColor,      // 기술주: 파랑
            StockSector.SEM => semColor,        // 반도체: 하늘색
            StockSector.EV => evColor,          // 전기차: 초록
            StockSector.CORP => corpColor,      // 대기업: 노랑
            StockSector.CRYPTO => cryptoColor,  // 가상자산: 보라
            _ => neutralColor                   // 기타: 회색
        };
    }

    /// <summary>
    /// 섹터 로컬라이징 키 생성
    /// 섹터명의 다국어 지원을 위한 키 반환
    /// </summary>
    string GetSectorLocalizationKey(StockSector sector)
    {
        return sector switch
        {
            StockSector.TECH => "sector_tech",
            StockSector.SEM => "sector_sem",
            StockSector.EV => "sector_ev",
            StockSector.CORP => "sector_corp",
            StockSector.CRYPTO => "sector_crypto",
            _ => "sector_unknown"
        };
    }

    /// <summary>
    /// 디버그: 현재 섹터 데이터 로그 출력
    /// 개발 중 데이터 확인용
    /// </summary>
    [ContextMenu("섹터 데이터 출력")]
    void DebugLogSectorData()
    {
        if (sectorData == null)
        {
            Debug.Log("섹터 데이터가 없습니다.");
            return;
        }

        Debug.Log($"=== {sectorData.sector} 섹터 정보 ===");
        Debug.Log($"수익률: {sectorData.returnRate:F1}%");
        Debug.Log($"투자금액: {sectorData.investedAmount:N0}원");
        Debug.Log($"현재가치: {sectorData.currentValue:N0}원");
        Debug.Log($"손익: {sectorData.currentValue - sectorData.investedAmount:N0}원");
        Debug.Log($"멘트: {GetDefaultComment(sectorData.returnRate)}");
    }
}