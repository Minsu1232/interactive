using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 매도 수량 선택 팝업 UI 관리
/// 기존 시스템과 완전 연동, 평단가 기준 손익 계산
/// </summary>
public class SellQuantityPopup : MonoBehaviour
{
    [Header("팝업 컨트롤")]
    public GameObject popupPanel;               // 실제 팝업 패널 (자식 오브젝트)
    public Button overlayButton;                // 배경 클릭으로 닫기용

    [Header("종목 정보 표시")]
    public TextMeshProUGUI stockNameText;       // 종목명
    public TextMeshProUGUI stockPriceText;      // 현재가
    public TextMeshProUGUI currentHoldingsText; // 현재 보유 정보
    public Image sectorTagImage;                // 섹터 태그 배경
    public TextMeshProUGUI sectorTagText;       // 섹터 텍스트

    [Header("수량 선택 컨트롤")]
    public Button decreaseButton;               // - 버튼
    public Button increaseButton;               // + 버튼
    public TextMeshProUGUI quantityDisplay;     // 수량 표시
    public Button[] quickSelectButtons;         // 빠른 선택 버튼들 (1주, 5주, 10주, 최대)

    [Header("계산 정보 표시 - 라벨들")]
    public TextMeshProUGUI pricePerShareLabelText;  // "주당 가격:" 라벨
    public TextMeshProUGUI quantityLabelText;       // "매도 수량:" 라벨
    public TextMeshProUGUI feeLabelText;            // "매매 수수료:" 라벨
    public TextMeshProUGUI totalAmountLabelText;    // "받을 금액:" 라벨

    [Header("계산 정보 표시 - 값들")]
    public TextMeshProUGUI pricePerShareText;       // 주당 가격 값
    public TextMeshProUGUI quantityText;            // 매도 수량 값
    public TextMeshProUGUI feeText;                 // 매매 수수료 값
    public TextMeshProUGUI totalAmountText;         // 받을 금액 값

    [Header("손익 정보 표시")]
    public TextMeshProUGUI avgPriceLabelText;       // "평단가:" 라벨
    public TextMeshProUGUI avgPriceText;            // 평단가 값
    public TextMeshProUGUI profitLossLabelText;     // "예상 손익:" 라벨
    public TextMeshProUGUI profitLossText;          // 예상 손익 값

    [Header("UI 라벨들 (로컬라이징용)")]
    public TextMeshProUGUI popupTitleText;          // "매도 주문" 제목
    public TextMeshProUGUI quantitySectionTitleText; // "매도 수량 선택" 섹션 제목

    [Header("경고 메시지")]
    public GameObject warningPanel;             // 경고 메시지 패널
    public TextMeshProUGUI warningText;         // 경고 텍스트

    [Header("액션 버튼")]
    public Button cancelButton;                 // 취소 버튼
    public Button confirmButton;                // 매도 확정 버튼

    [Header("섹터별 색상 설정")]
    public Color techColor = Color.blue;        // TECH (파랑)
    public Color semColor = Color.yellow;       // SEM (노랑)
    public Color evColor = Color.green;         // EV (초록)
    public Color cryptoColor = Color.red;       // CRYPTO (빨강)
    public Color corpColor = Color.magenta;     // CORP (자홍)

    [Header("손익 색상 설정")]
    public Color profitColor = Color.red;       // 수익 색상 (빨강)
    public Color lossColor = Color.blue;        // 손실 색상 (파랑)
    public Color neutralColor = Color.gray;     // 중립 색상 (회색)

    [Header("디버그")]
    public bool enableDebugLog = true;

    // 현재 상태
    private StockData currentStock;
    private int currentQuantity = 1;
    private int maxSellableQuantity = 1;        // 최대 매도 가능 수량 (보유량)
    private float averagePurchasePrice = 0f;    // 평단가

    // 싱글톤 패턴
    private static SellQuantityPopup instance;
    public static SellQuantityPopup Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SellQuantityPopup>();

                if (instance == null)
                {
                    Debug.LogError("⚠️ SellQuantityPopup이 씬에 없습니다! 수동으로 생성하거나 Prefab을 배치하세요.");
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
           
        }
        else if (instance != this)
        {
            Debug.LogWarning($"⚠️ SellQuantityPopup 중복 인스턴스 감지! {gameObject.name} 삭제");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (instance != this)
        {
            Debug.LogWarning("⚠️ 중복 인스턴스이므로 Start() 건너뜀");
            return;
        }

        SetupButtonEvents();
        HidePopup();

        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        if (enableDebugLog)
            Debug.Log("✅ SellQuantityPopup 초기화 완료");
    }

    void OnDestroy()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    void OnLanguageChanged(Language newLanguage)
    {
        UpdateLocalization();

        if (enableDebugLog)
            Debug.Log($"🌍 매도 팝업 언어 변경: {newLanguage}");
    }

    /// <summary>
    /// 버튼 이벤트 설정 메서드
    /// </summary>
    void SetupButtonEvents()
    {
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(() => ChangeQuantity(-1));

        if (increaseButton != null)
            increaseButton.onClick.AddListener(() => ChangeQuantity(1));

        // 빠른 선택 버튼들 (+1, +5, +10, 최대)
        if (quickSelectButtons != null && quickSelectButtons.Length >= 4)
        {
            if (quickSelectButtons[0] != null)
                quickSelectButtons[0].onClick.AddListener(() => AddQuantity(1));

            if (quickSelectButtons[1] != null)
                quickSelectButtons[1].onClick.AddListener(() => AddQuantity(5));

            if (quickSelectButtons[2] != null)
                quickSelectButtons[2].onClick.AddListener(() => AddQuantity(10));

            if (quickSelectButtons[3] != null)
                quickSelectButtons[3].onClick.AddListener(SetMaxQuantity);
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HidePopup);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSell);

        if (overlayButton != null)
            overlayButton.onClick.AddListener(HidePopup);

        if (enableDebugLog)
            Debug.Log("🔘 매도 팝업 버튼 이벤트 설정 완료");
    }

    /// <summary>
    /// 매도 팝업 표시 메서드
    /// </summary>
    /// <param name="stockData">매도할 종목 정보</param>
    public void ShowSellPopup(StockData stockData)
    {
        if (stockData == null)
        {
            Debug.LogError("❌ stockData가 null입니다!");
            return;
        }

        // 보유량 체크
        int holdings = GetCurrentHoldings(stockData.stockKey);
        if (holdings <= 0)
        {
            Debug.LogWarning($"⚠️ {stockData.displayName} 보유량이 없어서 매도 팝업을 열 수 없습니다.");
            return;
        }

        currentStock = stockData;
        currentQuantity = 1;
        maxSellableQuantity = holdings;
        averagePurchasePrice = GetAveragePurchasePrice(stockData.stockKey);

        UpdateLocalization();
        UpdateStockInfo();
        UpdateCalculation();

        if (popupPanel != null)
            popupPanel.SetActive(true);

        gameObject.SetActive(true);

        if (enableDebugLog)
            Debug.Log($"📉 매도 팝업 열림: {stockData.displayName} (보유: {holdings}주, 평단가: {averagePurchasePrice:N0}원)");
    }

    /// <summary>
    /// 팝업 숨김 메서드
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (enableDebugLog)
            Debug.Log("📝 매도 팝업 닫힘");
    }

    /// <summary>
    /// 로컬라이징 업데이트 메서드
    /// </summary>
    public void UpdateLocalization()
    {
        var locManager = CSVLocalizationManager.Instance;
        if (locManager == null) return;

        // 팝업 제목
        if (popupTitleText != null)
            popupTitleText.text = locManager.GetLocalizedText("sell_order");

        // 수량 선택 섹션 제목
        if (quantitySectionTitleText != null)
            quantitySectionTitleText.text = locManager.GetLocalizedText("sell_select_quantity");

        // 계산 정보 라벨들
        if (pricePerShareLabelText != null)
            pricePerShareLabelText.text = locManager.GetLocalizedText("ui_current_price") + ":";

        if (quantityLabelText != null)
            quantityLabelText.text = locManager.GetLocalizedText("button_sell") + " " + locManager.GetLocalizedText("ui_shares_unit") + ":";

        if (feeLabelText != null)
        {
            float feeRate = GetTradingFeeRate();
            string feeLabel = locManager.GetLocalizedText("trading_fee_label");
            feeLabelText.text = $"{feeLabel} ({feeRate}%):";
        }

        if (totalAmountLabelText != null)
            totalAmountLabelText.text = locManager.GetLocalizedText("sell_receive_amount") + ":";

        // 손익 정보 라벨들
        if (avgPriceLabelText != null)
            avgPriceLabelText.text = locManager.GetLocalizedText("portfolio_averageprice") + ":";

        if (profitLossLabelText != null)
            profitLossLabelText.text = locManager.GetLocalizedText("sell_expected_profit") + ":";

        UpdateButtonTexts(locManager);
        UpdateQuickSelectButtons();

        if (enableDebugLog)
            Debug.Log("🌍 매도 팝업 로컬라이징 업데이트 완료");
    }

    /// <summary>
    /// 버튼 텍스트들 업데이트 메서드
    /// </summary>
    void UpdateButtonTexts(CSVLocalizationManager locManager)
    {
        if (cancelButton != null)
        {
            var cancelButtonText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (cancelButtonText != null)
                cancelButtonText.text = locManager.GetLocalizedText("cancel");
        }

        if (confirmButton != null)
        {
            var confirmButtonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmButtonText != null)
                confirmButtonText.text = locManager.GetLocalizedText("button_sell");
        }
    }

    /// <summary>
    /// 빠른 선택 버튼 텍스트 업데이트 메서드
    /// </summary>
    void UpdateQuickSelectButtons()
    {
        var locManager = CSVLocalizationManager.Instance;
        if (locManager == null || quickSelectButtons == null) return;

        string shareUnit = locManager.GetLocalizedText("ui_shares_unit");
        string maxText = locManager.GetLocalizedText("quick_max");

        for (int i = 0; i < quickSelectButtons.Length && i < 4; i++)
        {
            if (quickSelectButtons[i] == null) continue;

            var buttonText = quickSelectButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                switch (i)
                {
                    case 0: buttonText.text = $"+1{shareUnit}"; break;
                    case 1: buttonText.text = $"+5{shareUnit}"; break;
                    case 2: buttonText.text = $"+10{shareUnit}"; break;
                    case 3: buttonText.text = maxText; break;
                }
            }
        }
    }

    /// <summary>
    /// 종목 정보 업데이트 메서드
    /// </summary>
    void UpdateStockInfo()
    {
        if (currentStock == null) return;

        var locManager = CSVLocalizationManager.Instance;

        // 종목명
        if (stockNameText != null)
            stockNameText.text = currentStock.displayName;

        // 현재가 및 변동률
        UpdateStockPrice(locManager);

        // 섹터 태그
        UpdateSectorTag();

        // 현재 보유 정보
        UpdateCurrentHoldings(locManager);
    }

    /// <summary>
    /// 주식 가격 정보 업데이트 메서드
    /// </summary>
    void UpdateStockPrice(CSVLocalizationManager locManager)
    {
        if (stockPriceText == null || currentStock == null) return;

        string arrow = currentStock.changeRate > 0 ? "▲ " :
                      currentStock.changeRate < 0 ? "▼ " : "— ";

        string currencyFormat = locManager?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
        string priceText = string.Format(currencyFormat, currentStock.currentPrice);

        stockPriceText.text = $"{priceText} ({arrow}{currentStock.changeRate:F1}%)";
    }

    /// <summary>
    /// 섹터 태그 업데이트 메서드
    /// </summary>
    void UpdateSectorTag()
    {
        if (currentStock == null) return;

        Color sectorColor = GetSectorColor(currentStock.sector);

        if (sectorTagImage != null)
            sectorTagImage.color = sectorColor;

        if (sectorTagText != null)
        {
            if (CSVLocalizationManager.Instance != null)
                sectorTagText.text = CSVLocalizationManager.Instance.GetSectorName(currentStock.sector);
            else
                sectorTagText.text = currentStock.sector.ToString();

            sectorTagText.color = Color.white;
        }
    }

    /// <summary>
    /// 섹터별 색상 가져오기 메서드
    /// </summary>
    Color GetSectorColor(StockSector sector)
    {
        switch (sector)
        {
            case StockSector.TECH: return techColor;
            case StockSector.SEM: return semColor;
            case StockSector.EV: return evColor;
            case StockSector.CRYPTO: return cryptoColor;
            case StockSector.CORP: return corpColor;
            default: return techColor;
        }
    }

    /// <summary>
    /// 현재 보유 정보 업데이트 메서드
    /// </summary>
    void UpdateCurrentHoldings(CSVLocalizationManager locManager)
    {
        if (currentHoldingsText == null) return;

        string avgPriceLabel = locManager?.GetLocalizedText("portfolio_averageprice") ?? "평단가";
        string sharesUnit = locManager?.GetLocalizedText("ui_shares_unit") ?? "주";
        string currencyFormat = locManager?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
        string currentHoldingsLabel = locManager?.GetLocalizedText("current_holdings") ?? "현재 보유";
        string avgPriceText = string.Format(currencyFormat, (int)averagePurchasePrice);
        currentHoldingsText.text = $"{currentHoldingsLabel}: {maxSellableQuantity}{sharesUnit} ({avgPriceLabel}: {avgPriceText})";
    }

    /// <summary>
    /// 수량 더하기 메서드 (최대치 제한)
    /// </summary>
    void AddQuantity(int addAmount)
    {
        int newQuantity = currentQuantity + addAmount;

        if (newQuantity > maxSellableQuantity)
        {
            newQuantity = maxSellableQuantity;

            if (enableDebugLog)
                Debug.Log($"⚠️ 최대 매도량 제한: {addAmount}주 추가 요청 → 최대치 {maxSellableQuantity}주로 제한");
        }

        SetQuantity(newQuantity);
    }

    /// <summary>
    /// 수량 변경 메서드
    /// </summary>
    void ChangeQuantity(int delta)
    {
        int newQuantity = currentQuantity + delta;

        if (newQuantity > maxSellableQuantity)
        {
            newQuantity = maxSellableQuantity;

            if (enableDebugLog)
                Debug.Log($"⚠️ + 버튼 최대치 제한: {maxSellableQuantity}주");
        }

        SetQuantity(newQuantity);
    }

    /// <summary>
    /// 수량 설정 메서드
    /// </summary>
    void SetQuantity(int quantity)
    {
        currentQuantity = Mathf.Clamp(quantity, 1, maxSellableQuantity);
        UpdateCalculation();
        UpdateButtonStates();

        if (enableDebugLog)
            Debug.Log($"📊 매도 수량 설정: {currentQuantity}주 (보유: {maxSellableQuantity}주)");
    }

    /// <summary>
    /// 최대 매도 가능 수량으로 설정
    /// </summary>
    void SetMaxQuantity()
    {
        SetQuantity(maxSellableQuantity);

        if (enableDebugLog)
            Debug.Log($"📉 전량 매도 설정: {maxSellableQuantity}주");
    }

    /// <summary>
    /// 계산 정보 업데이트 메서드
    /// </summary>
    void UpdateCalculation()
    {
        if (currentStock == null) return;

        var locManager = CSVLocalizationManager.Instance;

        // 기본 계산
        int stockPrice = currentStock.currentPrice;
        int sellAmount = stockPrice * currentQuantity;
        int fee = CalculateTradingFee(sellAmount);
        int netAmount = sellAmount - fee; // 실제 받을 금액

        // 포맷팅
        string currencyFormat = locManager?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
        string sharesUnit = locManager?.GetLocalizedText("ui_shares_unit") ?? "주";

        // UI 업데이트
        if (quantityDisplay != null)
            quantityDisplay.text = currentQuantity.ToString();

        if (pricePerShareText != null)
            pricePerShareText.text = string.Format(currencyFormat, stockPrice);

        if (quantityText != null)
            quantityText.text = $"{currentQuantity}{sharesUnit}";

        if (feeText != null)
            feeText.text = string.Format(currencyFormat, fee);

        if (totalAmountText != null)
            totalAmountText.text = string.Format(currencyFormat, netAmount);

        // 평단가 정보
        if (avgPriceText != null)
            avgPriceText.text = string.Format(currencyFormat, (int)averagePurchasePrice);

        // 손익 계산 및 표시
        UpdateProfitLoss(netAmount, currencyFormat);

        UpdateButtonStates();
    }

    /// <summary>
    /// 손익 계산 및 업데이트 메서드
    /// </summary>
    void UpdateProfitLoss(int netAmount, string currencyFormat)
    {
        if (profitLossText == null || averagePurchasePrice <= 0) return;

        // 평단가 기준 매수 금액
        int purchaseAmount = (int)(averagePurchasePrice * currentQuantity);

        // 실현 손익 계산
        int profitLoss = netAmount - purchaseAmount;
        float profitLossRate = purchaseAmount > 0 ? ((float)profitLoss / purchaseAmount) * 100f : 0f;

        // 텍스트 및 색상 설정
        if (profitLoss > 0)
        {
            profitLossText.text = $"+{string.Format(currencyFormat, profitLoss)} (+{profitLossRate:F1}%)";
            profitLossText.color = profitColor; // 수익: 빨간색
        }
        else if (profitLoss < 0)
        {
            profitLossText.text = $"-{string.Format(currencyFormat, Mathf.Abs(profitLoss))} ({profitLossRate:F1}%)";
            profitLossText.color = lossColor; // 손실: 파란색
        }
        else
        {
            profitLossText.text = $"±{string.Format(currencyFormat, 0)} (0.0%)";
            profitLossText.color = neutralColor;
        }
    }

    /// <summary>
    /// 버튼 상태 업데이트 메서드
    /// </summary>
    void UpdateButtonStates()
    {
        bool isAtMax = (currentQuantity >= maxSellableQuantity);
        bool isAtMin = (currentQuantity <= 1);

        if (increaseButton != null)
            increaseButton.interactable = !isAtMax;

        if (decreaseButton != null)
            decreaseButton.interactable = !isAtMin;

        if (quickSelectButtons != null)
        {
            for (int i = 0; i < quickSelectButtons.Length && i < 4; i++)
            {
                if (quickSelectButtons[i] != null)
                {
                    if (i < 3) // +1, +5, +10 버튼들
                    {
                        quickSelectButtons[i].interactable = !isAtMax;
                    }
                    else // 최대 버튼
                    {
                        quickSelectButtons[i].interactable = true;
                    }
                }
            }
        }

        if (enableDebugLog && isAtMax)
            Debug.Log($"🔒 최대 매도량 도달: {currentQuantity}주 - 증가 버튼들 비활성화");
    }

    /// <summary>
    /// 매도 확정 메서드
    /// </summary>
    void ConfirmSell()
    {
        if (currentStock == null)
        {
            Debug.LogError("❌ currentStock이 null입니다!");
            return;
        }

        // ✅ 한번에 전체 수량 매도 (기존 SellStockWithFee 사용)
        bool success = GameManager.Instance.SellStockWithFee(currentStock.stockKey, currentQuantity);

        if (success)
        {
            if (enableDebugLog)
                Debug.Log($"✅ 매도 완료: {currentStock.displayName} {currentQuantity}주");
            HidePopup();
        }
        else
        {
            Debug.LogError("❌ 매도 실패!");
        }
    }

    #region 기존 시스템 연동 메서드들

    int GetCurrentHoldings(string stockKey)
    {
        return StockManager.Instance?.GetHoldingAmount(stockKey) ?? 0;
    }

    float GetAveragePurchasePrice(string stockKey)
    {
        return PortfolioManager.Instance?.GetAveragePurchasePrice(stockKey) ?? 0f;
    }

    float GetTradingFeeRate()
    {
        return GameManager.Instance?.TradingFeeRate ?? 1f;
    }

    int CalculateTradingFee(int amount)
    {
        return GameManager.Instance?.CalculateTradingFee(amount) ?? Mathf.RoundToInt(amount * 0.01f);
    }

    #endregion

    public bool IsPopupOpen => popupPanel != null && popupPanel.activeInHierarchy;
    public StockData CurrentStock => currentStock;
    public int CurrentQuantity => currentQuantity;
    public int MaxSellableQuantity => maxSellableQuantity;
}