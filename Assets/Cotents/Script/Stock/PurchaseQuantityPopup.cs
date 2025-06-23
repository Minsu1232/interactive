using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 매수 수량 선택 팝업 UI 관리
/// 기존 시스템과 완전 연동, 중복 없는 깔끔한 설계
/// </summary>
public class PurchaseQuantityPopup : MonoBehaviour
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
    public TextMeshProUGUI quantityLabelText;       // "매수 수량:" 라벨
    public TextMeshProUGUI feeLabelText;            // "매매 수수료:" 라벨
    public TextMeshProUGUI totalCostLabelText;      // "총 필요 금액:" 라벨

    [Header("계산 정보 표시 - 값들")]
    public TextMeshProUGUI pricePerShareText;       // 주당 가격 값
    public TextMeshProUGUI quantityText;            // 매수 수량 값
    public TextMeshProUGUI feeText;                 // 매매 수수료 값
    public TextMeshProUGUI totalCostText;           // 총 필요 금액 값

    [Header("UI 라벨들 (로컬라이징용)")]
    public TextMeshProUGUI popupTitleText;          // "매수 주문" 제목
    public TextMeshProUGUI quantitySectionTitleText; // "매수 수량 선택" 섹션 제목

    [Header("경고 메시지")]
    public GameObject warningPanel;             // 경고 메시지 패널
    public TextMeshProUGUI warningText;         // 경고 텍스트

    [Header("액션 버튼")]
    public Button cancelButton;                 // 취소 버튼
    public Button confirmButton;                // 매수 확정 버튼

    [Header("섹터별 색상 설정")]
    public Color techColor = Color.blue;        // TECH (파랑)
    public Color semColor = Color.yellow;       // SEM (노랑)
    public Color evColor = Color.green;         // EV (초록)
    public Color cryptoColor = Color.red;       // CRYPTO (빨강)
    public Color corpColor = Color.magenta;     // CORP (자홍)

    [Header("디버그")]
    public bool enableDebugLog = true;

    // 현재 상태
    private StockData currentStock;
    private int currentQuantity = 1;
    private int maxAffordableQuantity = 1;  // ✅ 추가: 최대 구매 가능 수량 저장

    // 싱글톤 패턴 (수정: 더 안전한 체크)
    private static PurchaseQuantityPopup instance;
    public static PurchaseQuantityPopup Instance
    {
        get
        {
            // 🔧 null 체크를 더 엄격하게
            if (instance == null)
            {
                instance = FindFirstObjectByType<PurchaseQuantityPopup>();

                if (instance == null)
                {
                    Debug.LogError("⚠️ PurchaseQuantityPopup이 씬에 없습니다! 수동으로 생성하거나 Prefab을 배치하세요.");
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        // 🔧 수정: 씬에 이미 인스턴스가 있는지 더 정확하게 체크
        if (instance == null)
        {
            instance = this;
            
        }
        else if (instance != this)
        {
            Debug.LogWarning($"⚠️ PurchaseQuantityPopup 중복 인스턴스 감지! {gameObject.name} 삭제");
            Destroy(gameObject);
            return; // 즉시 리턴하여 Start() 호출 방지
        }
    }

    void Start()
    {
        // 🔧 중복 인스턴스면 Start() 실행 안함
        if (instance != this)
        {
            Debug.LogWarning("⚠️ 중복 인스턴스이므로 Start() 건너뜀");
            return;
        }

        // 버튼 이벤트 설정
        SetupButtonEvents();

        // 초기에는 팝업 숨김
        HidePopup();

        // 로컬라이징 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        if (enableDebugLog)
            Debug.Log("✅ PurchaseQuantityPopup 초기화 완료");
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 언어 변경시 호출되는 메서드
    /// </summary>
    /// <param name="newLanguage">새로운 언어</param>
    void OnLanguageChanged(Language newLanguage)
    {
        UpdateLocalization();

        if (enableDebugLog)
            Debug.Log($"🌍 매수 팝업 언어 변경: {newLanguage}");
    }

    /// <summary>
    /// 버튼 이벤트 설정 메서드
    /// 모든 버튼의 클릭 이벤트를 등록
    /// </summary>
    void SetupButtonEvents()
    {
        // 수량 조절 버튼
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(() => ChangeQuantity(-1));

        if (increaseButton != null)
            increaseButton.onClick.AddListener(() => ChangeQuantity(1));

        // 빠른 선택 버튼들 (1주, 5주, 10주, 최대) - 수정: 더하기 방식
        if (quickSelectButtons != null && quickSelectButtons.Length >= 4)
        {
            if (quickSelectButtons[0] != null)
                quickSelectButtons[0].onClick.AddListener(() => AddQuantity(1));    // +1주

            if (quickSelectButtons[1] != null)
                quickSelectButtons[1].onClick.AddListener(() => AddQuantity(5));    // +5주

            if (quickSelectButtons[2] != null)
                quickSelectButtons[2].onClick.AddListener(() => AddQuantity(10));   // +10주

            if (quickSelectButtons[3] != null)
                quickSelectButtons[3].onClick.AddListener(SetMaxQuantity);          // 최대
        }

        // 액션 버튼
        if (cancelButton != null)
            cancelButton.onClick.AddListener(HidePopup);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmPurchase);

        // 오버레이 클릭으로 닫기
        if (overlayButton != null)
            overlayButton.onClick.AddListener(HidePopup);

        if (enableDebugLog)
            Debug.Log("🔘 매수 팝업 버튼 이벤트 설정 완료");
    }

    /// <summary>
    /// 매수 팝업 표시 메서드
    /// 종목 정보만 받아서 나머지는 기존 매니저들에서 자동으로 가져옴
    /// </summary>
    /// <param name="stockData">매수할 종목 정보</param>
    public void ShowPurchasePopup(StockData stockData)
    {
        if (stockData == null)
        {
            Debug.LogError("❌ stockData가 null입니다!");
            return;
        }

        currentStock = stockData;
        currentQuantity = 1;

        // ✅ 최대 구매 가능량 미리 계산
        maxAffordableQuantity = CalculateMaxAffordableQuantity();

        // 로컬라이징 먼저 업데이트
        UpdateLocalization();

        // 종목 정보 및 계산 정보 업데이트
        UpdateStockInfo();
        UpdateCalculation();

        // 팝업 표시 (자식 패널 활성화)
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }

        // 매니저 오브젝트 자체도 활성화 (필요한 경우)
        gameObject.SetActive(true);

        if (enableDebugLog)
            Debug.Log($"💰 매수 팝업 열림: {stockData.displayName}");
    }

    /// <summary>
    /// 팝업 숨김 메서드
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        if (enableDebugLog)
            Debug.Log("📝 매수 팝업 닫힘");
    }

    /// <summary>
    /// 로컬라이징 업데이트 메서드
    /// 모든 UI 텍스트를 현재 언어에 맞게 업데이트
    /// </summary>
    public void UpdateLocalization()
    {
        var locManager = CSVLocalizationManager.Instance;
        if (locManager == null) return;

        // 팝업 제목
        if (popupTitleText != null)
            popupTitleText.text = locManager.GetLocalizedText("purchase_order");

        // 수량 선택 섹션 제목
        if (quantitySectionTitleText != null)
            quantitySectionTitleText.text = locManager.GetLocalizedText("purchase_select_quantity");

        // 계산 정보 라벨들
        if (pricePerShareLabelText != null)
            pricePerShareLabelText.text = locManager.GetLocalizedText("ui_current_price") + ":";

        if (quantityLabelText != null)
            quantityLabelText.text = locManager.GetLocalizedText("button_buy") + " " + locManager.GetLocalizedText("ui_shares_unit") + ":";

        if (feeLabelText != null)
        {
            float feeRate = GetTradingFeeRate();
            string feeLabel = locManager.GetLocalizedText("trading_fee_label");
            feeLabelText.text = $"{feeLabel} ({feeRate}%):";
        }

        if (totalCostLabelText != null)
            totalCostLabelText.text = locManager.GetLocalizedText("total_amount_label") + ":";

        // 버튼 텍스트들
        UpdateButtonTexts(locManager);

        // 빠른 선택 버튼들
        UpdateQuickSelectButtons();

        if (enableDebugLog)
            Debug.Log("🌍 매수 팝업 로컬라이징 업데이트 완료");
    }

    /// <summary>
    /// 버튼 텍스트들 업데이트 메서드
    /// </summary>
    /// <param name="locManager">로컬라이징 매니저</param>
    void UpdateButtonTexts(CSVLocalizationManager locManager)
    {
        // 취소 버튼
        if (cancelButton != null)
        {
            var cancelButtonText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (cancelButtonText != null)
                cancelButtonText.text = locManager.GetLocalizedText("cancel");
        }

        // 확정 버튼
        if (confirmButton != null)
        {
            var confirmButtonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmButtonText != null)
                confirmButtonText.text = locManager.GetLocalizedText("button_buy");
        }
    }

    /// <summary>
    /// 빠른 선택 버튼 텍스트 업데이트 메서드 (수정: 더하기 표시)
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
                    case 0: buttonText.text = $"+1{shareUnit}"; break;  // +1주
                    case 1: buttonText.text = $"+5{shareUnit}"; break;  // +5주
                    case 2: buttonText.text = $"+10{shareUnit}"; break; // +10주
                    case 3: buttonText.text = maxText; break;           // 최대
                }
            }
        }
    }

    /// <summary>
    /// 종목 정보 업데이트 메서드
    /// 기존 매니저들에서 데이터를 가져와서 UI에 표시
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

        // 섹터 태그 (색상 + 텍스트)
        UpdateSectorTag();

        // 현재 보유 정보
        UpdateCurrentHoldings(locManager);
    }

    /// <summary>
    /// 주식 가격 정보 업데이트 메서드
    /// </summary>
    /// <param name="locManager">로컬라이징 매니저</param>
    void UpdateStockPrice(CSVLocalizationManager locManager)
    {
        if (stockPriceText == null || currentStock == null) return;

        // 변동률에 따른 화살표
        string arrow = "";
        if (currentStock.changeRate > 0) arrow = "▲ ";
        else if (currentStock.changeRate < 0) arrow = "▼ ";
        else arrow = "— ";

        // 로컬라이징된 통화 포맷 사용
        string currencyFormat = locManager?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
        string priceText = string.Format(currencyFormat, currentStock.currentPrice);

        stockPriceText.text = $"{priceText} ({arrow}{currentStock.changeRate:F1}%)";
    }

    /// <summary>
    /// 섹터 태그 업데이트 메서드 (색상 + 텍스트)
    /// </summary>
    void UpdateSectorTag()
    {
        if (currentStock == null) return;

        // 섹터별 색상 설정
        Color sectorColor = GetSectorColor(currentStock.sector);

        if (sectorTagImage != null)
            sectorTagImage.color = sectorColor;

        if (sectorTagText != null)
        {
            // 섹터명 로컬라이징
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
    /// <param name="sector">섹터 타입</param>
    /// <returns>섹터에 해당하는 색상</returns>
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
    /// <param name="locManager">로컬라이징 매니저</param>
    void UpdateCurrentHoldings(CSVLocalizationManager locManager)
    {
        if (currentHoldingsText == null) return;

        int holdings = GetCurrentHoldings();

        if (holdings > 0)
        {
            string avgPriceLabel = locManager?.GetLocalizedText("portfolio_averageprice") ?? "평단가";
            string sharesUnit = locManager?.GetLocalizedText("ui_shares_unit") ?? "주";
            string currencyFormat = locManager?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
            string currentHoldingsLabel = locManager?.GetLocalizedText("current_holdings") ?? "현재 보유";

            float avgPrice = GetAveragePurchasePrice();
            string avgPriceText = string.Format(currencyFormat, (int)avgPrice);
            currentHoldingsText.text = $"{currentHoldingsLabel}: {holdings}{sharesUnit} ({avgPriceLabel}: {avgPriceText})";
        }
        else
        {
            string noHoldingsText = locManager?.GetLocalizedText("no_holdings") ?? "보유 없음";
            currentHoldingsText.text = $"{noHoldingsText}";
        }
    }

    /// <summary>
    /// 수량 더하기 메서드 (최대치 제한 추가)
    /// </summary>
    /// <param name="addAmount">추가할 수량</param>
    void AddQuantity(int addAmount)
    {
        int newQuantity = currentQuantity + addAmount;

        // ✅ 최대치 제한
        if (newQuantity > maxAffordableQuantity)
        {
            newQuantity = maxAffordableQuantity;

            if (enableDebugLog)
                Debug.Log($"⚠️ 최대 구매량 제한: {addAmount}주 추가 요청 → 최대치 {maxAffordableQuantity}주로 제한");
        }

        SetQuantity(newQuantity);

        if (enableDebugLog && newQuantity < currentQuantity + addAmount)
            Debug.Log($"📊 수량 추가 (제한적용): +{addAmount}주 요청 → 실제 {newQuantity}주");
        else if (enableDebugLog)
            Debug.Log($"📊 수량 추가: +{addAmount}주 → 총 {newQuantity}주");
    }

    /// <summary>
    /// 수량 변경 메서드 (최대치 제한 추가)
    /// </summary>
    /// <param name="delta">변경량 (+1, -1)</param>
    void ChangeQuantity(int delta)
    {
        int newQuantity = currentQuantity + delta;

        // ✅ 최대치 제한 (+ 버튼용)
        if (newQuantity > maxAffordableQuantity)
        {
            newQuantity = maxAffordableQuantity;

            if (enableDebugLog)
                Debug.Log($"⚠️ + 버튼 최대치 제한: {maxAffordableQuantity}주");
        }

        SetQuantity(newQuantity);
    }

    /// <summary>
    /// 수량 설정 메서드 (최대치 제한 추가)
    /// </summary>
    /// <param name="quantity">설정할 수량</param>
    void SetQuantity(int quantity)
    {
        // ✅ 최소값 1, 최대값 maxAffordableQuantity로 제한
        currentQuantity = Mathf.Clamp(quantity, 1, maxAffordableQuantity);

        UpdateCalculation();
        UpdateButtonStates(); // ✅ 버튼 상태 업데이트 추가

        if (enableDebugLog)
            Debug.Log($"📊 수량 설정: {currentQuantity}주 (요청: {quantity}주, 최대: {maxAffordableQuantity}주)");
    }

    /// <summary>
    /// 최대 매수 가능 수량 설정 메서드 (수정: maxAffordableQuantity 저장)
    /// GameManager의 수수료 시스템을 고려하여 정확히 계산
    /// </summary>
    void SetMaxQuantity()
    {
        if (currentStock == null) return;

        int currentCash = GetCurrentCash();
        int stockPrice = currentStock.currentPrice;

        // ✅ 최대 구매 가능량을 계산하고 저장
        maxAffordableQuantity = CalculateMaxAffordableQuantity();

        // 현재 수량을 최대량으로 설정
        SetQuantity(maxAffordableQuantity);

        if (enableDebugLog)
        {
            int finalCost = (stockPrice * maxAffordableQuantity) + CalculateTradingFee(stockPrice * maxAffordableQuantity);
            Debug.Log($"💰 최대 매수량 정확 계산: {maxAffordableQuantity}주");
            Debug.Log($"   - 현금: {currentCash:N0}원");
            Debug.Log($"   - 주가: {stockPrice:N0}원");
            Debug.Log($"   - 총비용: {finalCost:N0}원");
        }
    }

    /// <summary>
    /// ✅ 새로 추가: 최대 구매 가능 수량 계산 메서드
    /// </summary>
    /// <returns>최대 구매 가능 수량</returns>
    int CalculateMaxAffordableQuantity()
    {
        if (currentStock == null) return 1;

        int currentCash = GetCurrentCash();
        int stockPrice = currentStock.currentPrice;
        int maxQuantity = 0;

        // 1개씩 늘려가면서 실제로 살 수 있는 최대량 찾기
        for (int testQuantity = 1; testQuantity <= 1000; testQuantity++)
        {
            int stockCost = stockPrice * testQuantity;
            int fee = CalculateTradingFee(stockCost);
            int totalCost = stockCost + fee;

            if (totalCost <= currentCash)
            {
                maxQuantity = testQuantity;
            }
            else
            {
                break;
            }
        }

        return Mathf.Max(1, maxQuantity); // 최소 1주는 보장
    }

    /// <summary>
    /// ✅ 새로 추가: 버튼 상태 업데이트 메서드
    /// 최대치에 도달했을 때 + 버튼들을 비활성화
    /// </summary>
    void UpdateButtonStates()
    {
        bool isAtMax = (currentQuantity >= maxAffordableQuantity);
        bool isAtMin = (currentQuantity <= 1);

        // + 버튼 상태 (최대치에서 비활성화)
        if (increaseButton != null)
            increaseButton.interactable = !isAtMax;

        // - 버튼 상태 (최소치에서 비활성화)  
        if (decreaseButton != null)
            decreaseButton.interactable = !isAtMin;

        // 빠른 선택 버튼들 상태 (+1, +5, +10은 최대치에서 비활성화, 최대는 항상 활성화)
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
                    else // 최대 버튼 (i == 3)
                    {
                        quickSelectButtons[i].interactable = true; // 최대 버튼은 항상 활성화
                    }
                }
            }
        }

        if (enableDebugLog && isAtMax)
            Debug.Log($"🔒 최대치 도달: {currentQuantity}주 - 증가 버튼들 비활성화");
    }
    /// 실시간으로 수량에 따른 비용 계산
    /// </summary>
    void UpdateCalculation()
    {
        if (currentStock == null) return;

        var locManager = CSVLocalizationManager.Instance;

        // 기본 계산
        int stockPrice = currentStock.currentPrice;
        int stockCost = stockPrice * currentQuantity;
        int fee = CalculateTradingFee(stockCost);
        int totalCost = stockCost + fee;

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

        if (totalCostText != null)
            totalCostText.text = string.Format(currencyFormat, totalCost);

        // 자금 충분 여부 체크
        CheckFundsAvailability(totalCost);

        // ✅ 버튼 상태 업데이트 추가
        UpdateButtonStates();
    }

    /// <summary>
    /// 자금 충분 여부 체크 및 경고 표시 메서드
    /// </summary>
    /// <param name="totalCost">총 필요 금액</param>
    void CheckFundsAvailability(int totalCost)
    {
        int currentCash = GetCurrentCash();
        bool hasEnoughFunds = totalCost <= currentCash;

        // 경고 메시지 표시/숨김
        if (warningPanel != null)
        {
            warningPanel.SetActive(!hasEnoughFunds);

            if (warningText != null && !hasEnoughFunds)
            {
                var locManager = CSVLocalizationManager.Instance;
                string insufficientMsg = locManager?.GetLocalizedText("msg_insufficient_funds") ?? "자금이 부족합니다!";
                string currencyFormat = locManager?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
                string cashAmount = string.Format(currencyFormat, currentCash);

                warningText.text = $"⚠️ {insufficientMsg} (보유: {cashAmount})";
            }
        }

        // 확정 버튼 활성화/비활성화
        if (confirmButton != null)
            confirmButton.interactable = hasEnoughFunds;
    }

    /// <summary>
    /// 매수 확정 메서드
    /// GameManager의 수수료 시스템을 활용하여 실제 매수 처리
    /// </summary>
    void ConfirmPurchase()
    {
        if (currentStock == null)
        {
            Debug.LogError("❌ currentStock이 null입니다!");
            return;
        }

        // ✅ 한번에 전체 수량 매수 (기존 BuyStockWithFee 사용)
        bool success = GameManager.Instance.BuyStockWithFee(currentStock.stockKey, currentQuantity);

        if (success)
        {
            if (enableDebugLog)
                Debug.Log($"✅ 매수 완료: {currentStock.displayName} {currentQuantity}주");
            HidePopup();
        }
        else
        {
            Debug.LogError("❌ 매수 실패!");
        }
    }
    #region 기존 시스템 연동 메서드들

    /// <summary>
    /// 현재 보유 현금 가져오기 (UIManager에서)
    /// </summary>
    /// <returns>현재 보유 현금</returns>
    int GetCurrentCash()
    {
        return UIManager.Instance?.GetCurrentCash() ?? 0;
    }

    /// <summary>
    /// 현재 보유 수량 가져오기 (StockManager에서)
    /// </summary>
    /// <returns>현재 보유 수량</returns>
    int GetCurrentHoldings()
    {
        if (currentStock == null || StockManager.Instance == null) return 0;
        return StockManager.Instance.GetHoldingAmount(currentStock.stockKey);
    }

    /// <summary>
    /// 평균 매수가 가져오기 (PortfolioManager에서)
    /// </summary>
    /// <returns>평균 매수가</returns>
    float GetAveragePurchasePrice()
    {
        if (currentStock == null || PortfolioManager.Instance == null) return 0f;
        return PortfolioManager.Instance.GetAveragePurchasePrice(currentStock.stockKey);
    }

    /// <summary>
    /// 매매 수수료율 가져오기 (GameManager에서)
    /// </summary>
    /// <returns>수수료율 (수치 자체가 백분율임)</returns>
    float GetTradingFeeRate()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.TradingFeeRate; // 백분율로 변환
        return 1f; // 폴백: 1%
    }

    /// <summary>
    /// 매매 수수료 계산 (GameManager 시스템 활용)
    /// </summary>
    /// <param name="amount">거래 금액</param>
    /// <returns>수수료</returns>
    int CalculateTradingFee(int amount)
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.CalculateTradingFee(amount);
        return Mathf.RoundToInt(amount * 0.01f); // 폴백: 1%
    }

    #endregion

    #region 공개 프로퍼티

    /// <summary>
    /// 팝업이 현재 열려있는지 확인
    /// </summary>
    public bool IsPopupOpen => popupPanel != null && popupPanel.activeInHierarchy;

    /// <summary>
    /// 현재 선택된 종목 정보
    /// </summary>
    public StockData CurrentStock => currentStock;

    /// <summary>
    /// 현재 선택된 수량
    /// </summary>
    public int CurrentQuantity => currentQuantity;

    #endregion

    #region 디버그 메서드들

    /// <summary>
    /// 현재 상태 정보 출력 (디버그용)
    /// </summary>
    [ContextMenu("현재 상태 출력")]
    void PrintCurrentState()
    {
        if (currentStock == null)
        {
            Debug.Log("📊 현재 선택된 종목 없음");
            return;
        }

        Debug.Log($"📊 === 매수 팝업 현재 상태 ===");
        Debug.Log($"종목: {currentStock.displayName}");
        Debug.Log($"현재가: {currentStock.currentPrice:N0}원");
        Debug.Log($"선택 수량: {currentQuantity}주");
        Debug.Log($"보유 현금: {GetCurrentCash():N0}원");
        Debug.Log($"현재 보유: {GetCurrentHoldings()}주");
        Debug.Log($"평균 매수가: {GetAveragePurchasePrice():N0}원");
        Debug.Log($"수수료율: {GetTradingFeeRate()}%");

        int totalCost = currentStock.currentPrice * currentQuantity;
        int fee = CalculateTradingFee(totalCost);
        Debug.Log($"총 비용: {totalCost + fee:N0}원 (수수료 {fee:N0}원 포함)");
    }

    /// <summary>
    /// 테스트용 팝업 열기
    /// </summary>
    [ContextMenu("테스트 팝업 열기")]
    void TestOpenPopup()
    {
        if (StockManager.Instance != null)
        {
            var stocks = StockManager.Instance.GetAllStocks();
            if (stocks.Count > 0)
            {
                ShowPurchasePopup(stocks[0]);
            }
        }
    }

    #endregion
}