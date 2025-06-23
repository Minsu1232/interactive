using UnityEngine;

/// <summary>
/// 투자 결과 관련 데이터 구조체들
/// </summary>

/// <summary>
/// 탭 종류 열거형
/// </summary>
public enum TabType
{
    FinalResult,        // 최종결과
    TradeHistory,       // 매매내역
    SectorPerformance,  // 섹터성과
    MajorEvents         // 주요이벤트
}

/// <summary>
/// 매매 내역 데이터
/// </summary>
[System.Serializable]
public class TradeRecord
{
    public int turnNumber;          // 턴 번호
    public TradeType tradeType;     // 매수/매도
    public string stockName;        // 종목명
    public string stockId;          // 종목 ID
    public int quantity;            // 수량
    public int price;               // 단가
    public System.DateTime timestamp; // 거래 시간
}

/// <summary>
/// 거래 유형 열거형
/// </summary>
public enum TradeType
{
    Buy,    // 매수
    Sell    // 매도
}

/// <summary>
/// 섹터 성과 데이터
/// </summary>
[System.Serializable]
public class SectorPerformance
{
    public StockSector sector;          // 섹터
    public float investmentRatio;       // 투자 비중 (%)
    public float returnRate;            // 수익률 (%)
    public float investedAmount;        // 투자 금액
    public float currentValue;          // 현재 가치
}
/// <summary>
/// 섹터별 통계를 저장하는 간단한 클래스
/// </summary>
public class SectorStats
{
    public float totalBuyAmount = 0f;      // 총 매수 금액
    public float totalSellAmount = 0f;     // 총 매도 금액  
    public float currentHoldingValue = 0f; // 현재 보유 가치
}
/// <summary>
/// 이벤트 기록 데이터
/// </summary>
[System.Serializable]
public class EventRecord
{
    public int turnNumber;              // 발생 턴
    public string eventName;            // 이벤트명
    public float impactPercent;         // 영향률 (%)
}

/// <summary>
/// 섹터별 투자 데이터 (내부 계산용)
/// </summary>
public class SectorInvestmentData
{
    public float totalInvested = 0f;    // 총 투자 금액
    public float currentValue = 0f;     // 현재 총 가치
}