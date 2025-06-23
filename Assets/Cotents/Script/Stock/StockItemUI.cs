using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class StockItemUI : MonoBehaviour
{
    [Header("기본 UI 요소들")]
    public TextMeshProUGUI stockNameText;   // 종목명
    public TextMeshProUGUI priceText;       // 현재가
    public TextMeshProUGUI changeRateText;  // 변동률
    public Image sectorTagBG;               // 섹터 태그 배경 이미지
    public TextMeshProUGUI sectorTagText;   // 섹터 태그 텍스트 (TECH, SEM 등)

    [Header("카드 전용 UI 요소들")]
    public Button buyButton;                // 매수 버튼
    public GameObject ownedIndicator;       // "보유중" 표시 오브젝트
    public TextMeshProUGUI holdingsText;    // 보유량 표시 (선택사항)
    public Image cardBackground;            // 카드 배경 (보유중일 때 색상 변경용)

    [Header("색상 설정")]
    public Color upColor = Color.red;       // 상승 (빨강)
    public Color downColor = Color.blue;    // 하락 (파랑)  
    public Color sameColor = Color.gray;    // 보합 (회색)

    [Header("섹터 색상")]
    public Color techColor = Color.blue;    // TECH (파랑)
    public Color semColor = Color.yellow;   // SEM (노랑)
    public Color evColor = Color.green;     // EV (초록)
    public Color cryptoColor = Color.red;   // CRYPTO (주황/빨강)
    public Color corpColor = Color.magenta; // CORP (자홍)

    [Header("카드 상태 색상")]
    public Color normalCardColor = Color.white;     // 일반 카드 배경색
    public Color ownedCardColor = Color.green;      // 보유중 카드 배경색 (연한 초록)

    private StockData stockData;
    private int currentHoldings = 0;  // 현재 보유량

    // 매수 버튼 클릭 이벤트
    public event Action<StockData> OnBuyButtonClicked;

    void Start()
    {
        // 프리팹 생성시 로컬라이징 적용
        ApplyLocalization();

        // 언어 변경 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // 언어 변경시 호출
    void OnLanguageChanged(Language newLanguage)
    {
        ApplyLocalization();
        UpdateUI(); // 전체 UI 다시 업데이트
    }

    // 로컬라이징 적용
    void ApplyLocalization()
    {
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
            return;

        // 매수 버튼 텍스트 로컬라이징
        UpdateBuyButton();

        // 보유중 표시 로컬라이징 (있다면)
        if (ownedIndicator != null)
        {
            var ownedText = ownedIndicator.GetComponentInChildren<TextMeshProUGUI>();
            if (ownedText != null)
            {
                ownedText.text = CSVLocalizationManager.Instance.GetLocalizedText("ui_owned");
            }
        }
    }

    // 데이터 설정 및 UI 업데이트
    public void SetStockData(StockData data, int holdings = 0)
    {
        stockData = data;
        currentHoldings = holdings;
        UpdateUI();
    }

    // UI 전체 업데이트
    public void UpdateUI()
    {
        if (stockData == null) return;

        // 기본 정보 업데이트
        UpdateBasicInfo();

        // 변동률 업데이트 (색상 포함)
        UpdateChangeRate();

        // 섹터 태그 색상 업데이트
        UpdateSectorTag();

        // 보유 상태 업데이트
        UpdateOwnershipStatus();

        // 매수 버튼 상태 업데이트 (로컬라이징 포함)
        UpdateBuyButton();
    }

    // 기본 정보 업데이트
    private void UpdateBasicInfo()
    {
        stockNameText.text = stockData.displayName;
        priceText.text = $"₩{stockData.currentPrice:N0}";
    }

    // 변동률 텍스트 및 색상 업데이트
    private void UpdateChangeRate()
    {
        string arrow = "";
        Color targetColor = sameColor;

        if (stockData.changeRate > 0)
        {
            arrow = "▲ ";
            targetColor = upColor;
        }
        else if (stockData.changeRate < 0)
        {
            arrow = "▼ ";
            targetColor = downColor;
        }
        else
        {
            arrow = "— ";
            targetColor = sameColor;
        }

        changeRateText.text = $"{arrow}{stockData.changeRate:F1}%";
        changeRateText.color = targetColor;
    }

    // 섹터 태그 배경색 및 텍스트 업데이트
    private void UpdateSectorTag()
    {
        // 섹터별 색상 설정
        switch (stockData.sector)
        {
            case StockSector.TECH:
                sectorTagBG.color = techColor;
                break;
            case StockSector.SEM:
                sectorTagBG.color = semColor;
                break;
            case StockSector.EV:
                sectorTagBG.color = evColor;
                break;
            case StockSector.CRYPTO:
                sectorTagBG.color = cryptoColor;
                break;
            case StockSector.CORP:
                sectorTagBG.color = corpColor;
                break;
        }

        // 현재 언어에 맞는 섹터명 표시
        if (CSVLocalizationManager.Instance != null)
        {
            sectorTagText.text = CSVLocalizationManager.Instance.GetSectorName(stockData.sector);
        }
        else
        {
            sectorTagText.text = stockData.sector.ToString();
        }

        // 텍스트 색상을 흰색으로 (배경과 대비)
        sectorTagText.color = Color.white;
    }

    // 보유 상태 업데이트 (보유중 표시, 카드 배경색 등)
    private void UpdateOwnershipStatus()
    {
        bool isOwned = currentHoldings > 0;

        // 보유중 인디케이터 표시/숨김
        if (ownedIndicator != null)
        {
            ownedIndicator.SetActive(isOwned);
        }

        // 보유량 텍스트 업데이트 (있는 경우)
        if (holdingsText != null)
        {
            if (isOwned)
            {
                // 로컬라이징된 단위 사용
                string unit = "주"; // 기본값
                if (CSVLocalizationManager.Instance != null)
                {
                    unit = CSVLocalizationManager.Instance.GetLocalizedText("ui_shares_unit");
                }

                holdingsText.text = $"{currentHoldings}{unit}";
                holdingsText.gameObject.SetActive(true);
            }
            else
            {
                holdingsText.gameObject.SetActive(false);
            }
        }

        // 카드 배경색 변경
        if (cardBackground != null)
        {
            cardBackground.color = isOwned ? ownedCardColor : normalCardColor;
        }
    }

    // 매수 버튼 상태 업데이트 (로컬라이징 포함)
    private void UpdateBuyButton()
    {
        if (buyButton == null) return;

        // 보유중인 경우와 미보유인 경우 버튼 텍스트 변경 (로컬라이징)
        TextMeshProUGUI buttonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (CSVLocalizationManager.Instance != null)
            {
                if (currentHoldings > 0)
                {
                    buttonText.text = CSVLocalizationManager.Instance.GetLocalizedText("ui_buy_more");
                }
                else
                {
                    buttonText.text = CSVLocalizationManager.Instance.GetLocalizedText("ui_buy");
                }
            }
            else
            {
                // 로컬라이징 매니저가 없으면 기본 한국어
                if (currentHoldings > 0)
                {
                    buttonText.text = "추가 매수";
                }
                else
                {
                    buttonText.text = "매수하기";
                }
            }
        }

        // 매수 버튼 클릭 이벤트 설정
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    /// <summary>
    /// 매수 버튼 클릭 처리 - 팝업 버전 (완전 간소화)
    /// 모든 데이터는 팝업에서 알아서 가져가므로 종목 정보만 전달
    /// </summary>
    private void OnBuyClicked()
    {
        Debug.Log($"{stockData.stockName} 매수 버튼 클릭!");

        // 매수 팝업 찾기 및 열기
        if (PurchaseQuantityPopup.Instance != null)
        {
            // 간단하게 종목 정보만 전달 (나머지는 팝업에서 알아서 처리)
            PurchaseQuantityPopup.Instance.ShowPurchasePopup(stockData);
        }
        else
        {
            Debug.LogWarning("⚠️ PurchaseQuantityPopup을 찾을 수 없습니다. 기본 매수 처리합니다.");

            // 폴백: 기존 방식 (1개 매수)
            if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
            {
                GameManager.Instance.BuyStockWithFee(stockData.stockKey, 1);
            }
            else
            {
                OnBuyButtonClicked?.Invoke(stockData);
            }
        }
    }


    // 외부에서 보유량 업데이트할 때 사용
    public void UpdateHoldings(int newHoldings)
    {
        currentHoldings = newHoldings;
        UpdateOwnershipStatus();
        UpdateBuyButton();
    }

    // 카드 전체 클릭 처리 (매수 버튼과 동일한 기능)
    public void OnCardClicked()
    {
        OnBuyClicked();
    }

    // 현재 보유량 반환
    public int GetCurrentHoldings()
    {
        return currentHoldings;
    }

    // 현재 종목 데이터 반환
    public StockData GetStockData()
    {
        return stockData;
    }

}