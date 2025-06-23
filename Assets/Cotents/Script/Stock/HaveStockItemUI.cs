using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HaveStockItemUI : MonoBehaviour
{
    [Header("기본 UI 요소들")]
    public TextMeshProUGUI stockNameText;       // 종목명
    public TextMeshProUGUI holdingsText;        // 보유량 (예: "5주")
    public TextMeshProUGUI currentPriceText;    // 현재가
    public TextMeshProUGUI totalValueText;      // 총 평가액 (보유량 × 현재가)
    public TextMeshProUGUI profitLossText;      // 개별 손익
    public Image sectorTagBG;                   // 섹터 태그 배경
    public TextMeshProUGUI sectorTagText;       // 섹터 태그 텍스트
    public TextMeshProUGUI averagePriceText;    // 평단가 표시용 ✅ 추가된 변수

    [Header("매도 버튼")]
    public Button sellButton;                   // 매도 버튼
    public TextMeshProUGUI sellButtonText;      // 매도 버튼 텍스트

    [Header("색상 설정")]
    public Color profitColor = Color.red;       // 수익 색상 (빨강)
    public Color lossColor = Color.blue;        // 손실 색상 (파랑)
    public Color neutralColor = Color.gray;     // 중립 색상 (회색)

    [Header("섹터 색상")]
    public Color techColor = Color.blue;        // TECH (파랑)
    public Color semColor = Color.yellow;       // SEM (노랑)
    public Color evColor = Color.green;         // EV (초록)
    public Color cryptoColor = Color.red;       // CRYPTO (주황/빨강)
    public Color corpColor = Color.magenta;     // CORP (자홍)

    private StockData stockData;
    private int currentHoldings = 0;
    private float averagePurchasePrice = 0f;    // 평균 매수가

    // 매도 버튼 클릭 이벤트
    public event Action<StockData, int> OnSellButtonClicked;

    /// <summary>
    /// 데이터 설정 및 UI 업데이트
    /// </summary>
    /// <param name="data">주식 데이터</param>
    /// <param name="holdings">보유 수량</param>
    /// <param name="avgPrice">평균 매수가</param>
    public void SetStockData(StockData data, int holdings, float avgPrice = 0f)
    {
        stockData = data;
        currentHoldings = holdings;
        averagePurchasePrice = avgPrice;
        UpdateUI();
    }

    /// <summary>
    /// UI 전체 업데이트 메서드
    /// 기본 정보, 섹터 태그, 손익, 평단가, 매도 버튼을 모두 업데이트
    /// </summary>
    public void UpdateUI()
    {
        if (stockData == null || currentHoldings <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 기본 정보 업데이트
        UpdateBasicInfo();

        // 섹터 태그 업데이트
        UpdateSectorTag();

        // ✅ 평단가 업데이트 (새로 추가)
        UpdateAveragePrice();

        // 손익 계산 및 업데이트
        UpdateProfitLoss();

        // 매도 버튼 설정
        UpdateSellButton();
    }

    /// <summary>
    /// 기본 정보 업데이트 (종목명, 현재가, 총 평가액)
    /// </summary>
    private void UpdateBasicInfo()
    {
        // 종목명
        if (stockNameText != null)
            stockNameText.text = stockData.displayName;

        // 현재가
        if (currentPriceText != null)
            currentPriceText.text = $"₩{stockData.currentPrice:N0}";

        // 총 평가액
        if (totalValueText != null)
        {
            int totalValue = stockData.currentPrice * currentHoldings;
            totalValueText.text = $"₩{totalValue:N0}";
        }
    }

    /// <summary>
    /// ✅ 평단가 표시 업데이트 메서드 (새로 추가)
    /// CSV 로컬라이징을 사용하여 평단가를 표시
    /// </summary>
    private void UpdateAveragePrice()
    {
        if (averagePriceText == null) return;

        // 평단가가 있는 경우만 표시
        if (averagePurchasePrice > 0)
        {
            // 로컬라이징된 평단가 라벨 가져오기
            string avgPriceLabel = "평단가"; // 기본값
            if (CSVLocalizationManager.Instance != null)
            {
                avgPriceLabel = CSVLocalizationManager.Instance.GetLocalizedText("portfolio_averageprice");
            }

            averagePriceText.text = $"{avgPriceLabel}: ₩{averagePurchasePrice:N0}";
            averagePriceText.gameObject.SetActive(true);
        }
        else
        {
            // 평단가 정보가 없으면 숨김
            averagePriceText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 섹터 태그 업데이트 (배경색 및 텍스트)
    /// </summary>
    private void UpdateSectorTag()
    {
        // 섹터별 색상 설정
        Color sectorColor = techColor;
        switch (stockData.sector)
        {
            case StockSector.TECH:
                sectorColor = techColor;
                break;
            case StockSector.SEM:
                sectorColor = semColor;
                break;
            case StockSector.EV:
                sectorColor = evColor;
                break;
            case StockSector.CRYPTO:
                sectorColor = cryptoColor;
                break;
            case StockSector.CORP:
                sectorColor = corpColor;
                break;
        }

        if (sectorTagBG != null)
            sectorTagBG.color = sectorColor;

        // 섹터명 텍스트
        if (sectorTagText != null)
        {
            if (CSVLocalizationManager.Instance != null)
            {
                sectorTagText.text = CSVLocalizationManager.Instance.GetSectorName(stockData.sector);
            }
            else
            {
                sectorTagText.text = stockData.sector.ToString();
            }
            sectorTagText.color = Color.white;
        }
    }

    /// <summary>
    /// 손익 계산 및 업데이트 메서드
    /// 평단가 기준으로 수익/손실을 계산하고 색상 적용
    /// </summary>
    private void UpdateProfitLoss()
    {
        if (profitLossText == null || averagePurchasePrice <= 0) return;

        // 현재 평가액
        int currentTotalValue = stockData.currentPrice * currentHoldings;

        // 총 매수 금액 (평단가 × 보유수량)
        int totalPurchaseValue = (int)(averagePurchasePrice * currentHoldings);

        // 손익 계산
        int profitLoss = currentTotalValue - totalPurchaseValue;
        float profitLossRate = totalPurchaseValue > 0 ? ((float)profitLoss / totalPurchaseValue) * 100f : 0f;

        // 텍스트 및 색상 설정
        if (profitLoss > 0)
        {
            profitLossText.text = $"+₩{profitLoss:N0} (+{profitLossRate:F1}%)";
            profitLossText.color = profitColor;
        }
        else if (profitLoss < 0)
        {
            profitLossText.text = $"-₩{Mathf.Abs(profitLoss):N0} ({profitLossRate:F1}%)";
            profitLossText.color = lossColor;
        }
        else
        {
            profitLossText.text = "±₩0 (0.0%)";
            profitLossText.color = neutralColor;
        }
    }

    /// <summary>
    /// 매도 버튼 설정 및 로컬라이징
    /// </summary>
    private void UpdateSellButton()
    {
        if (sellButton == null) return;

        TextMeshProUGUI buttonText = sellButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            // ✅ CSV에서 가져오기
            if (CSVLocalizationManager.Instance != null)
            {
                buttonText.text = CSVLocalizationManager.Instance.GetLocalizedText("ui_sell");
            }
            else
            {
                buttonText.text = "매도"; // 폴백
            }
        }

        // 보유량
        if (holdingsText != null)
        {
            // ✅ 단위도 로컬라이징
            string unit = "주";
            if (CSVLocalizationManager.Instance != null)
            {
                unit = CSVLocalizationManager.Instance.GetLocalizedText("ui_shares_unit");
            }
            holdingsText.text = $"{currentHoldings}{unit}";
        }

        // 클릭 이벤트 설정
        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(OnSellClicked);

        // 버튼 활성화
        sellButton.interactable = true;
    }

    /// <summary>
    /// 매도 버튼 클릭 처리 - 팝업 버전으로 수정
    /// 기존의 전량 매도 대신 수량 선택 팝업을 열어줌
    /// </summary>
    private void OnSellClicked()
    {
        Debug.Log($"📉 매도 요청: {stockData.displayName} (보유: {currentHoldings}주)");

        // 매도 팝업 찾기 및 열기
        if (SellQuantityPopup.Instance != null)
        {
            // 간단하게 종목 정보만 전달 (나머지는 팝업에서 알아서 처리)
            SellQuantityPopup.Instance.ShowSellPopup(stockData);
        }
        else
        {
            Debug.LogWarning("⚠️ SellQuantityPopup을 찾을 수 없습니다. 기본 매도 처리합니다.");

            // 폴백: 기존 방식 (전량 매도)
            if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
            {
                GameManager.Instance.SellStockWithFee(stockData.stockKey, currentHoldings);
            }
            else
            {
                OnSellButtonClicked?.Invoke(stockData, currentHoldings);
            }
        }
    }

    /// <summary>
    /// 외부에서 평균 매수가 업데이트할 때 사용
    /// PortfolioManager에서 호출
    /// </summary>
    /// <param name="avgPrice">새로운 평균 매수가</param>
    public void UpdateAveragePurchasePrice(float avgPrice)
    {
        averagePurchasePrice = avgPrice;
        UpdateAveragePrice(); // ✅ 평단가 UI 업데이트
        UpdateProfitLoss();   // 손익도 다시 계산
    }

    // 현재 데이터 반환
    public StockData GetStockData() => stockData;
    public int GetHoldings() => currentHoldings;
    public float GetAveragePurchasePrice() => averagePurchasePrice;

    // 게임오브젝트 활성화/비활성화 제어
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}