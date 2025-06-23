using UnityEngine;

/// <summary>
/// ���� ��� ���� ������ ����ü��
/// </summary>

/// <summary>
/// �� ���� ������
/// </summary>
public enum TabType
{
    FinalResult,        // �������
    TradeHistory,       // �Ÿų���
    SectorPerformance,  // ���ͼ���
    MajorEvents         // �ֿ��̺�Ʈ
}

/// <summary>
/// �Ÿ� ���� ������
/// </summary>
[System.Serializable]
public class TradeRecord
{
    public int turnNumber;          // �� ��ȣ
    public TradeType tradeType;     // �ż�/�ŵ�
    public string stockName;        // �����
    public string stockId;          // ���� ID
    public int quantity;            // ����
    public int price;               // �ܰ�
    public System.DateTime timestamp; // �ŷ� �ð�
}

/// <summary>
/// �ŷ� ���� ������
/// </summary>
public enum TradeType
{
    Buy,    // �ż�
    Sell    // �ŵ�
}

/// <summary>
/// ���� ���� ������
/// </summary>
[System.Serializable]
public class SectorPerformance
{
    public StockSector sector;          // ����
    public float investmentRatio;       // ���� ���� (%)
    public float returnRate;            // ���ͷ� (%)
    public float investedAmount;        // ���� �ݾ�
    public float currentValue;          // ���� ��ġ
}
/// <summary>
/// ���ͺ� ��踦 �����ϴ� ������ Ŭ����
/// </summary>
public class SectorStats
{
    public float totalBuyAmount = 0f;      // �� �ż� �ݾ�
    public float totalSellAmount = 0f;     // �� �ŵ� �ݾ�  
    public float currentHoldingValue = 0f; // ���� ���� ��ġ
}
/// <summary>
/// �̺�Ʈ ��� ������
/// </summary>
[System.Serializable]
public class EventRecord
{
    public int turnNumber;              // �߻� ��
    public string eventName;            // �̺�Ʈ��
    public float impactPercent;         // ����� (%)
}

/// <summary>
/// ���ͺ� ���� ������ (���� ����)
/// </summary>
public class SectorInvestmentData
{
    public float totalInvested = 0f;    // �� ���� �ݾ�
    public float currentValue = 0f;     // ���� �� ��ġ
}