using UnityEngine;

[System.Serializable]
public class StockData
{
    [Header("�⺻ ����")]
    public string stockKey;         // "SmartTech" (���ö���¡ Ű)
    public string stockName;        // "SmartTech" (������)
    public string displayName;      // ���� ǥ�õ� �̸� (�� ���� ����)
    public StockSector sector;      // TECH, SEM, EV, CRYPTO, CORP

    [Header("���� ����")]
    public int currentPrice;        // ���簡
    public int previousPrice;       // ���� �� ����
    public float changeRate;        // ����� (%)

    [Header("���� ����")]
    public int currentRank;         // ���� ����
    public int previousRank;        // ���� ����
    public RankChange rankChange;   // ���� ����

    // ������
    public StockData(string key, string name, StockSector sectorType, int startPrice)
    {
        stockKey = key;
        stockName = name;
        displayName = name; // �ʱⰪ�� ������
        sector = sectorType;
        currentPrice = startPrice;
        previousPrice = startPrice;
        changeRate = 0f;
        currentRank = 1;
        previousRank = 1;
        rankChange = RankChange.SAME;
    }

    // ǥ�ø� ������Ʈ (��� �����)
    public void UpdateDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }

    // ���� ������Ʈ
    public void UpdatePrice(float newChangeRate)
    {
        previousPrice = currentPrice;
        changeRate = newChangeRate;
        currentPrice = Mathf.RoundToInt(currentPrice * (1 + changeRate / 100f));
    }

    // ���� ������Ʈ
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
    TECH,   // ����� (�Ķ�)
    SEM,    // �ݵ�ü (���)
    EV,     // ������/������ (�ʷ�)
    CRYPTO, // �����ڻ� (��Ȳ)
    CORP    // ������� (����)
}

public enum RankChange
{
    UP,     // ��
    DOWN,   // ��
    SAME    // ��
}