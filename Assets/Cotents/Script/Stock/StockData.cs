using UnityEngine;

[System.Serializable]
public class StockData
{
    [Header("기본 정보")]
    public string stockKey;         // "SmartTech" (로컬라이징 키)
    public string stockName;        // "SmartTech" (영문명)
    public string displayName;      // 현재 표시될 이름 (언어에 따라 변경)
    public StockSector sector;      // TECH, SEM, EV, CRYPTO, CORP

    [Header("가격 정보")]
    public int currentPrice;        // 현재가
    public int previousPrice;       // 이전 턴 가격
    public float changeRate;        // 등락률 (%)

    [Header("순위 정보")]
    public int currentRank;         // 현재 순위
    public int previousRank;        // 이전 순위
    public RankChange rankChange;   // 순위 변동

    // 생성자
    public StockData(string key, string name, StockSector sectorType, int startPrice)
    {
        stockKey = key;
        stockName = name;
        displayName = name; // 초기값은 영문명
        sector = sectorType;
        currentPrice = startPrice;
        previousPrice = startPrice;
        changeRate = 0f;
        currentRank = 1;
        previousRank = 1;
        rankChange = RankChange.SAME;
    }

    // 표시명 업데이트 (언어 변경시)
    public void UpdateDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }

    // 가격 업데이트
    public void UpdatePrice(float newChangeRate)
    {
        previousPrice = currentPrice;
        changeRate = newChangeRate;
        currentPrice = Mathf.RoundToInt(currentPrice * (1 + changeRate / 100f));
    }

    // 순위 업데이트
    public void UpdateRank(int newRank)
    {
        previousRank = currentRank;
        currentRank = newRank;

        if (previousRank > currentRank)
            rankChange = RankChange.UP;
        else if (previousRank < currentRank)
            rankChange = RankChange.DOWN;
        else
            rankChange = RankChange.SAME;
    }
}

public enum StockSector
{
    TECH,   // 기술주 (파랑)
    SEM,    // 반도체 (노랑)
    EV,     // 전기차/에너지 (초록)
    CRYPTO, // 가상자산 (주황)
    CORP    // 전통대기업 (빨강)
}

public enum RankChange
{
    UP,     // ↑
    DOWN,   // ↓
    SAME    // →
}