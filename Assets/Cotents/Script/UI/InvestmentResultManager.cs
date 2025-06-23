using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameHistoryManager;

/// <summary>
/// 투자 결과 화면 관리자 - 실제 데이터 연동 버전
/// 4개 탭: 최종결과, 매매내역, 섹터성과, 주요이벤트
/// ✅ 더미 데이터 제거하고 실제 GameHistoryManager 데이터 사용
/// </summary>
public class InvestmentResultManager : MonoBehaviour
{
    [Header("메인 타이틀 UI")]
    public TextMeshProUGUI mainTitleText;  // "🎮 투자 결과" 메인 타이틀
    [Header("탭 버튼들")]
    public Button finalResultTabButton;
    public Button tradeHistoryTabButton;
    public Button sectorPerformanceTabButton;
    public Button majorEventsTabButton;

    [Header("탭 컨텐츠 패널들")]
    public GameObject finalResultPanel;
    public GameObject tradeHistoryPanel;
    public GameObject sectorPerformancePanel;
    public GameObject majorEventsPanel;
    [Header("패널 제목들")]
    public TextMeshProUGUI finalResultPanelTitle;      // "최종결과" 제목
    public TextMeshProUGUI tradeHistoryPanelTitle;     // "매매내역" 제목  
    public TextMeshProUGUI sectorPerformancePanelTitle; // "섹터별 성과" 제목
    public TextMeshProUGUI majorEventsPanelTitle;      // "주요이벤트" 제목
    [Header("최종결과 패널 UI")]
    public TextMeshProUGUI congratulationsText;
    public TextMeshProUGUI finalAmountText;
    public TextMeshProUGUI profitRateText;
    public TextMeshProUGUI lifestyleGradeText;
    public TextMeshProUGUI diversificationStarsText;
    public TextMeshProUGUI diversificationBonusText;
    public TextMeshProUGUI totalTradesText;
    public TextMeshProUGUI totalProfitText;
    public TextMeshProUGUI totalTradesLabelText;  // "총 거래" 라벨
    public TextMeshProUGUI totalProfitLabelText;  // "총 수익" 라벨

    [Header("매매내역 패널 UI")]
    public Transform tradeHistoryContentParent;
    public GameObject tradeHistoryItemPrefab;

    [Header("섹터성과 패널 UI")]
    public Transform sectorContentParent;
    public GameObject sectorItemPrefab;

    [Header("주요이벤트 패널 UI")]
    public Transform eventsContentParent;
    public GameObject eventItemPrefab;

    [Header("하단 버튼들")]
    public Button restartButton;
    public Button printButton;
    public Button mainMenuButton;
    [Header("신문or잡지패널")]
    public GameObject magazinePanel; // 신문/잡지 패널 (선택 사항, 필요시 활성화)
    [Header("색상 설정")]
    public Color profitColor = Color.green;
    public Color lossColor = Color.red;
    public Color selectedTabColor = Color.blue;
    public Color unselectedTabColor = Color.white;

    [Header("디버그")]
    public bool enableDebugLog = true;

    // 현재 상태
    private TabType currentTab = TabType.FinalResult;
    private GameResult gameResult;

    // 싱글톤
    private static InvestmentResultManager instance;
    public static InvestmentResultManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<InvestmentResultManager>();
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
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeCoroutine());
    }

    void OnDestroy()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 초기화 코루틴
    /// </summary>
    IEnumerator InitializeCoroutine()
    {
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        SetupButtonEvents();
        UpdateLocalization();
        CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        ShowTab(TabType.FinalResult);

        if (enableDebugLog)
            Debug.Log("✅ InvestmentResultManager 초기화 완료");
    }

    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    void SetupButtonEvents()
    {
        finalResultTabButton?.onClick.AddListener(() => ShowTab(TabType.FinalResult));
        tradeHistoryTabButton?.onClick.AddListener(() => ShowTab(TabType.TradeHistory));
        sectorPerformanceTabButton?.onClick.AddListener(() => ShowTab(TabType.SectorPerformance));
        majorEventsTabButton?.onClick.AddListener(() => ShowTab(TabType.MajorEvents));

        restartButton?.onClick.AddListener(OnRestartGame);
        printButton?.onClick.AddListener(OnPrintResult);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    /// <summary>
    /// 언어 변경 이벤트 처리
    /// </summary>
    void OnLanguageChanged(Language newLanguage)
    {
        UpdateLocalization();
        RefreshCurrentTab();
    }

    /// <summary>
    /// 로컬라이징 업데이트
    /// </summary>
    void UpdateLocalization()
    {
        var loc = CSVLocalizationManager.Instance;
        if (loc == null) return;
        mainTitleText.text = loc.GetLocalizedText("result_main_title") ?? "🎮 투자 결과";
        UpdateButtonText(finalResultTabButton, "result_tab_final");
        UpdateButtonText(tradeHistoryTabButton, "result_tab_trade_history");
        UpdateButtonText(sectorPerformanceTabButton, "result_tab_sector");
        UpdateButtonText(majorEventsTabButton, "result_tab_events");

        UpdateButtonText(restartButton, "result_button_restart");
        UpdateButtonText(printButton, "result_button_print");
        UpdateButtonText(mainMenuButton, "result_button_main_menu");
    }

    /// <summary>
    /// 버튼 텍스트 업데이트 헬퍼
    /// </summary>
    void UpdateButtonText(Button button, string localizationKey)
    {
        if (button == null) return;

        var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = CSVLocalizationManager.Instance?.GetLocalizedText(localizationKey) ?? localizationKey;
        }
    }

    #region 탭 관리

    /// <summary>
    /// 탭 전환
    /// </summary>
    public void ShowTab(TabType tabType)
    {
        currentTab = tabType;
        HideAllPanels();
        UpdateTabButtons();

        switch (tabType)
        {
            case TabType.FinalResult:
                finalResultPanel?.SetActive(true);
                UpdatePanelTitle(finalResultPanelTitle, "result_panel_final");
                break;
            case TabType.TradeHistory:
                tradeHistoryPanel?.SetActive(true);
                UpdatePanelTitle(tradeHistoryPanelTitle, "result_panel_trade_history");
                PopulateTradeHistoryData();
                break;
            case TabType.SectorPerformance:
                sectorPerformancePanel?.SetActive(true);
                UpdatePanelTitle(sectorPerformancePanelTitle, "result_panel_sector_performance");
                PopulateSectorData();
                break;
            case TabType.MajorEvents:
                majorEventsPanel?.SetActive(true);
                UpdatePanelTitle(majorEventsPanelTitle, "result_panel_major_events");
                PopulateEventsData();
                break;
        }

        if (enableDebugLog)
            Debug.Log($"📊 탭 전환: {tabType}");
    }
    void UpdatePanelTitle(TextMeshProUGUI titleText, string localizationKey)
    {
        if (titleText != null && CSVLocalizationManager.Instance != null)
        {
            titleText.text = CSVLocalizationManager.Instance.GetLocalizedText(localizationKey);
        }
    }
    void HideAllPanels()
    {
        finalResultPanel?.SetActive(false);
        tradeHistoryPanel?.SetActive(false);
        sectorPerformancePanel?.SetActive(false);
        majorEventsPanel?.SetActive(false);
    }

    void UpdateTabButtons()
    {
        UpdateTabButtonState(finalResultTabButton, currentTab == TabType.FinalResult);
        UpdateTabButtonState(tradeHistoryTabButton, currentTab == TabType.TradeHistory);
        UpdateTabButtonState(sectorPerformanceTabButton, currentTab == TabType.SectorPerformance);
        UpdateTabButtonState(majorEventsTabButton, currentTab == TabType.MajorEvents);
    }

    void UpdateTabButtonState(Button button, bool isSelected)
    {
        if (button == null) return;

        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedTabColor : unselectedTabColor;
        }
    }

    void RefreshCurrentTab()
    {
        ShowTab(currentTab);
    }

    #endregion

    #region 데이터 설정

    /// <summary>
    /// 게임 결과 데이터 설정 (외부에서 호출)
    /// </summary>
    public void SetGameResult(GameResult result)
    {
        gameResult = result;
        UpdateFinalResultPanel();

        if (enableDebugLog)
            Debug.Log($"📊 게임 결과 설정 완료: {result.finalAsset:N0}원, {result.profitRate:F1}%");
    }

    /// <summary>
    /// 최종결과 패널 업데이트
    /// </summary>
    void UpdateFinalResultPanel()
    {
        if (gameResult == null) return;

        var loc = CSVLocalizationManager.Instance;

        // 축하 메시지 로컬라이징 적용
        if (congratulationsText != null)
        {
            congratulationsText.text = loc?.GetLocalizedText("result_congratulations") ?? "투자 완료!";
        }

        if (finalAmountText != null)
        {
            finalAmountText.text = FormatCurrency(gameResult.finalAsset);
        }

        if (profitRateText != null)
        {
            profitRateText.text = $"{gameResult.profitRate:+0.0;-0.0}%";
            profitRateText.color = gameResult.profitRate >= 0 ? profitColor : lossColor;
        }

        if (lifestyleGradeText != null)
        {
            string gradeKey = GetLifestyleGradeKey(gameResult.lifestyleGrade);
            lifestyleGradeText.text = loc?.GetLocalizedText(gradeKey) ?? gameResult.lifestyleGrade.ToString();
        }

        UpdateDiversificationDisplay();

        if (totalTradesText != null)
        {
            int tradeCount = GetActualTradeCount();
            totalTradesText.text = loc?.GetLocalizedText("result_count_format")?.Replace("{0}", tradeCount.ToString()) ?? $"{tradeCount}회";
        }

        if (totalProfitText != null)
        {
            totalProfitText.text = FormatCurrency(gameResult.totalProfit);
            totalProfitText.color = gameResult.totalProfit >= 0 ? profitColor : lossColor;
        }

        // 라벨용 추가 변수들 (필요한 경우)
        if (totalTradesLabelText != null)
        {
            totalTradesLabelText.text = loc?.GetLocalizedText("result_total_trades") ?? "총 거래";
        }

        if (totalProfitLabelText != null)
        {
            totalProfitLabelText.text = loc?.GetLocalizedText("result_total_profit") ?? "총 수익";
        }
    }

    void UpdateDiversificationDisplay()
    {
        if (gameResult == null) return;

        var loc = CSVLocalizationManager.Instance;

        if (diversificationStarsText != null)
        {
            diversificationStarsText.text = CreateStarDisplay(gameResult.maxSectorsDiversified);
        }

        if (diversificationBonusText != null)
        {
            // 보너스 텍스트 로컬라이징 적용
            string bonusLabel = loc?.GetLocalizedText("result_bonus") ?? "보너스";
            diversificationBonusText.text = $"{gameResult.diversificationBonus:+0.0;-0.0}% {bonusLabel}";
            diversificationBonusText.color = gameResult.diversificationBonus >= 0 ? profitColor : lossColor;
        }
    }

    string CreateStarDisplay(int achievedSectors)
    {
        string stars = "";
        for (int i = 1; i <= 5; i++)
        {
            stars += (i <= achievedSectors) ? "★" : "☆";
        }
        return stars;
    }

    string GetLifestyleGradeKey(LifestyleGrade grade)
    {
        return grade switch
        {
            LifestyleGrade.Upper => "rank_upper_class",        // 초상류층 -> Elite Class
            LifestyleGrade.MiddleUpper => "rank_upper_middle",  // 상류층 -> Upper Class  
            LifestyleGrade.Middle => "rank_middle_class",       // 중상층 -> Middle Class
            LifestyleGrade.Lower => "rank_lower_class",         // 평범층 -> Common Class
            _ => "rank_middle_class"
        };
    }

    string FormatCurrency(int amount)
    {
        var loc = CSVLocalizationManager.Instance;
        string format = loc?.GetLocalizedText("ui_money_format") ?? "₩{0:N0}";
        return string.Format(format, amount);
    }

    #endregion

    #region 실제 데이터 표시 - ✅ 수정된 부분

    /// <summary>
    /// ✅ 매매내역 데이터 표시 - 실제 데이터 우선, 더미 데이터 제거
    /// </summary>
    void PopulateTradeHistoryData()
    {
        if (tradeHistoryContentParent == null || tradeHistoryItemPrefab == null) return;
        ClearContentParent(tradeHistoryContentParent);
        var tradeHistory = GetTradeHistoryFromGame();

        if (tradeHistory != null && tradeHistory.Count > 0)
        {
            // ✅ 실제 거래 시간순으로 정렬 (매수/매도 순서 강제 안 함)
            tradeHistory.Sort((a, b) =>
            {
                // 1순위: 턴 번호 (작은 턴 → 큰 턴)
                int turnComparison = a.turnNumber.CompareTo(b.turnNumber);
                if (turnComparison != 0) return turnComparison;

                // 2순위: ❌ 매수/매도 타입 강제 정렬 제거
                // ✅ timestamp 또는 거래 순서로 정렬
                if (a.timestamp != b.timestamp)
                {
                    return a.timestamp.CompareTo(b.timestamp);
                }

                // 3순위: 종목명 알파벳 순 (같은 시간일 때만)
                return string.Compare(a.stockName, b.stockName, System.StringComparison.OrdinalIgnoreCase);
            });
      

            foreach (var trade in tradeHistory)
            {
                CreateTradeHistoryItem(trade);
            }
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 매매내역이 없습니다. GameHistoryManager 연결 상태를 확인하세요.");
            CreateNoDataMessage(tradeHistoryContentParent, "매매 기록이 없습니다.");
        }
    }

    /// <summary>
    /// ✅ 섹터 성과 데이터 표시 - 실제 데이터 우선, 더미 데이터 제거
    /// </summary>
    void PopulateSectorData()
    {
        if (sectorContentParent == null || sectorItemPrefab == null) return;

        ClearContentParent(sectorContentParent);
        var sectorData = CalculateSectorPerformance();

        if (sectorData != null && sectorData.Count > 0)
        {
            if (enableDebugLog)
                Debug.Log($"📈 섹터 성과 {sectorData.Count}개 표시");

            foreach (var sector in sectorData)
            {
                CreateSectorItem(sector);
            }
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 섹터 데이터가 없습니다.");

            CreateNoDataMessage(sectorContentParent, "투자한 섹터가 없습니다.");
        }
    }

    /// <summary>
    /// ✅ 이벤트 데이터 표시 - 실제 데이터 우선, 더미 데이터 제거
    /// </summary>
    void PopulateEventsData()
    {
        if (eventsContentParent == null || eventItemPrefab == null) return;

        ClearContentParent(eventsContentParent);
        var eventHistory = GetEventHistoryFromGame();

        if (eventHistory != null && eventHistory.Count > 0)
        {
            if (enableDebugLog)
                Debug.Log($"📰 이벤트 {eventHistory.Count}개 표시");

            foreach (var eventRecord in eventHistory)
            {
                CreateEventItem(eventRecord);
            }
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 이벤트 기록이 없습니다.");

            CreateNoDataMessage(eventsContentParent, "발생한 이벤트가 없습니다.");
        }
    }

    /// <summary>
    /// ✅ 데이터 없을 때 메시지 표시하는 헬퍼 메서드
    /// </summary>
    void CreateNoDataMessage(Transform parent, string message)
    {
        GameObject messageObj = new GameObject("NoDataMessage");
        messageObj.transform.SetParent(parent);

        var textComponent = messageObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = message;
        textComponent.fontSize = 18;
        textComponent.color = Color.gray;
        textComponent.alignment = TextAlignmentOptions.Center;

        var rectTransform = messageObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 50);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    #endregion

    #region 실제 데이터 수집 - ✅ 수정된 부분

    /// <summary>
    /// ✅ 실제 매매내역 가져오기 - GameHistoryManager 우선
    /// </summary>
    List<TradeRecord> GetTradeHistoryFromGame()
    {
        // 1순위: GameHistoryManager에서 실제 거래 기록 가져오기
        if (GameHistoryManager.Instance != null)
        {
            var gameResult = GameHistoryManager.Instance.GenerateGameResult();
            if (gameResult?.allTransactions != null && gameResult.allTransactions.Count > 0)
            {
                if (enableDebugLog)
                    Debug.Log($"✅ GameHistoryManager에서 {gameResult.allTransactions.Count}개 거래기록 발견");

                return ConvertToTradeRecords(gameResult.allTransactions);
            }
        }

        if (enableDebugLog)
            Debug.LogWarning("⚠️ GameHistoryManager에서 거래기록을 찾을 수 없습니다.");

        return new List<TradeRecord>(); // 빈 리스트 반환 (더미 데이터 사용 안함)
    }

    /// <summary>
    /// ✅ 실제 섹터 성과 계산 - 개선된 버전
    /// </summary>
    List<SectorPerformance> CalculateSectorPerformance()
    {
        var sectorData = new List<SectorPerformance>();
        var sectorStats = new Dictionary<StockSector, SectorStats>();

        // 1. GameHistoryManager에서 거래 기록 수집
        if (GameHistoryManager.Instance != null)
        {
            var gameResult = GameHistoryManager.Instance.GenerateGameResult();
            if (gameResult?.allTransactions != null)
            {
                foreach (var transaction in gameResult.allTransactions)
                {
                    var sector = transaction.sector;
                    if (!sectorStats.ContainsKey(sector))
                        sectorStats[sector] = new SectorStats();

                    float amount = transaction.quantity * transaction.pricePerShare;

                    if (transaction.type == GameHistoryManager.TransactionType.Buy)
                    {
                        sectorStats[sector].totalBuyAmount += amount;
                    }
                    else
                    {
                        sectorStats[sector].totalSellAmount += amount;
                    }
                }
            }
        }

        // 2. ✅ StockManager에서 현재 보유 종목의 실제 가치 계산
        if (StockManager.Instance != null)
        {
            var currentHoldings = StockManager.Instance.GetAllHoldings();

            foreach (var holding in currentHoldings)
            {
                var stockData = StockManager.Instance.GetStockData(holding.Key);
                if (stockData != null)
                {
                    var sector = stockData.sector;
                    if (!sectorStats.ContainsKey(sector))
                        sectorStats[sector] = new SectorStats();

                    // ✅ 실제 현재 가격으로 계산
                    float currentValue = stockData.currentPrice * holding.Value;
                    sectorStats[sector].currentHoldingValue += currentValue;

                    if (enableDebugLog)
                    {
                        Debug.Log($"📊 현재 보유: {stockData.displayName} {holding.Value}주 × {stockData.currentPrice:N0}원 = {currentValue:N0}원");
                    }
                }
            }
        }

        // 3. 수익률 계산
        foreach (var kvp in sectorStats)
        {
            var sector = kvp.Key;
            var stats = kvp.Value;

            if (stats.totalBuyAmount <= 0) continue;

            float totalRecovered = stats.totalSellAmount + stats.currentHoldingValue;
            float profit = totalRecovered - stats.totalBuyAmount;
            float returnRate = (profit / stats.totalBuyAmount) * 100f;

            sectorData.Add(new SectorPerformance
            {
                sector = sector,
                returnRate = returnRate,
                investedAmount = stats.totalBuyAmount,
                currentValue = totalRecovered
            });

            if (enableDebugLog)
            {
                Debug.Log($"💼 {sector}: 투자 {stats.totalBuyAmount:N0} → 회수 {totalRecovered:N0} = {returnRate:+0.0;-0.0}%");
            }
        }

        return sectorData.OrderByDescending(s => s.returnRate).ToList();
    }

    /// <summary>
    /// ✅ 실제 이벤트 기록 가져오기 - GameHistoryManager 우선
    /// </summary>
    List<EventRecord> GetEventHistoryFromGame()
    {
        // 1순위: GameHistoryManager에서 이벤트 기록 가져오기
        if (GameHistoryManager.Instance != null)
        {
            var gameResult = GameHistoryManager.Instance.GenerateGameResult();
            if (gameResult?.allEvents != null && gameResult.allEvents.Count > 0)
            {
                if (enableDebugLog)
                    Debug.Log($"✅ GameHistoryManager에서 {gameResult.allEvents.Count}개 이벤트 기록 발견");

                return ConvertToEventRecords(gameResult.allEvents);
            }
        }

        // 2순위: GameManager에서 스케줄된 이벤트 중 발생한 것들 가져오기
        if (GameManager.Instance != null)
        {
            var eventRecords = new List<EventRecord>();
            var scheduledEvents = GameManager.Instance.GetScheduledEvents();
            int currentTurn = GameManager.Instance.CurrentTurn;

            foreach (var kvp in scheduledEvents)
            {
                if (kvp.Key <= currentTurn) // 이미 지나간 턴의 이벤트만
                {
                    var turnEvent = kvp.Value;
                    float avgImpact = 0f;

                    if (turnEvent.effects != null && turnEvent.effects.Count > 0)
                    {
                        foreach (var effect in turnEvent.effects)
                        {
                            avgImpact += effect.changeRate;
                        }
                        avgImpact /= turnEvent.effects.Count;
                    }

                    eventRecords.Add(new EventRecord
                    {
                        turnNumber = kvp.Key,
                        eventName = turnEvent.title,
                        impactPercent = avgImpact
                    });
                }
            }

            if (enableDebugLog && eventRecords.Count > 0)
                Debug.Log($"✅ GameManager에서 {eventRecords.Count}개 이벤트 기록 생성");

            return eventRecords;
        }

        if (enableDebugLog)
            Debug.LogWarning("⚠️ 이벤트 기록을 찾을 수 없습니다.");

        return new List<EventRecord>(); // 빈 리스트 반환 (더미 데이터 사용 안함)
    }

    /// <summary>
    /// ✅ 실제 거래 횟수 계산
    /// </summary>
    int GetActualTradeCount()
    {
        var tradeHistory = GetTradeHistoryFromGame();
        return tradeHistory?.Count ?? 0;
    }

    #endregion

    #region 아이템 생성

    void CreateTradeHistoryItem(TradeRecord trade)
    {
        GameObject item = Instantiate(tradeHistoryItemPrefab, tradeHistoryContentParent);
        var itemScript = item.GetComponent<TradeHistoryItemUI>();
        itemScript?.SetData(trade);
    }

    void CreateSectorItem(SectorPerformance sector)
    {
        GameObject item = Instantiate(sectorItemPrefab, sectorContentParent);
        var itemScript = item.GetComponent<SectorPerformanceItemUI>();
        itemScript?.SetData(sector);
    }

    void CreateEventItem(EventRecord eventRecord)
    {
        GameObject item = Instantiate(eventItemPrefab, eventsContentParent);
        var itemScript = item.GetComponent<EventItemUI>();
        itemScript?.SetData(eventRecord);
    }

    #endregion

    #region 유틸리티

    void ClearContentParent(Transform parent)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    List<TradeRecord> ConvertToTradeRecords(List<GameHistoryManager.TransactionRecord> transactions)
    {
        var trades = new List<TradeRecord>();
        if (transactions == null) return trades;

        foreach (var transaction in transactions)
        {
            trades.Add(new TradeRecord
            {
                turnNumber = transaction.turnNumber, // 🆕 실제 턴 번호 사용!
                tradeType = transaction.type == GameHistoryManager.TransactionType.Buy ? TradeType.Buy : TradeType.Sell,
                stockName = transaction.stockName,
                stockId = transaction.stockKey,
                quantity = transaction.quantity,
                price = (int)transaction.pricePerShare,
                timestamp = transaction.timestamp
            });
        }

        return trades;
    }

    List<EventRecord> ConvertToEventRecords(List<GameHistoryManager.EventRecord> events)
    {
        var eventRecords = new List<EventRecord>();
        if (events == null) return eventRecords;

        foreach (var gameEvent in events)
        {
            eventRecords.Add(new EventRecord
            {
                turnNumber = gameEvent.turnNumber,
                eventName = gameEvent.eventName,
                impactPercent = gameEvent.impactPercent
            });
        }

        return eventRecords;
    }


    #endregion

    #region 버튼 이벤트

    void OnRestartGame()
    {
        if (enableDebugLog)
            Debug.Log("🔄 게임 다시하기");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    void OnPrintResult()
    {
        if(magazinePanel != null)
        {
            magazinePanel.gameObject.SetActive(true);
        }
        

    }



    void OnMainMenu()
    {
        if (enableDebugLog)
            Debug.Log("🏠 메인 메뉴로 이동");

        UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
    }

    #endregion

    #region 공개 메서드

    [ContextMenu("테스트 결과 설정")]
    public void SetTestResult()
    {
        var testResult = new GameResult
        {
            initialCash = 1000000,
            finalAsset = 1380000,
            totalProfit = 380000,
            profitRate = 38.0f,
            lifestyleGrade = LifestyleGrade.MiddleUpper,
            totalTurns = 10,
            taxPaid = 0,
            diversificationBonus = 15.0f,
            maxSectorsDiversified = 4,
            totalTrades = 12
        };

        SetGameResult(testResult);

        if (enableDebugLog)
            Debug.Log("🧪 테스트 결과 설정 완료");
    }

    public void ShowSpecificTab(string tabName)
    {
        TabType targetTab = tabName.ToLower() switch
        {
            "final" or "result" => TabType.FinalResult,
            "trade" or "history" => TabType.TradeHistory,
            "sector" or "performance" => TabType.SectorPerformance,
            "events" or "event" => TabType.MajorEvents,
            _ => TabType.FinalResult
        };

        ShowTab(targetTab);
    }

    #endregion

    #region 디버그

    [ContextMenu("최종결과 탭")]
    void DebugShowFinalResult() => ShowTab(TabType.FinalResult);

    [ContextMenu("매매내역 탭")]
    void DebugShowTradeHistory() => ShowTab(TabType.TradeHistory);

    [ContextMenu("섹터성과 탭")]
    void DebugShowSectorPerformance() => ShowTab(TabType.SectorPerformance);

    [ContextMenu("주요이벤트 탭")]
    void DebugShowMajorEvents() => ShowTab(TabType.MajorEvents);

    [ContextMenu("실제 데이터 상태 확인")]
    void DebugCheckDataStatus()
    {
        Debug.Log("=== 실제 데이터 상태 확인 ===");

        // GameHistoryManager 상태
        if (GameHistoryManager.Instance != null)
        {
            var gameResult = GameHistoryManager.Instance.GenerateGameResult();
            Debug.Log($"✅ GameHistoryManager 연결됨");
            Debug.Log($"  - 거래기록: {gameResult?.allTransactions?.Count ?? 0}개");
            Debug.Log($"  - 이벤트기록: {gameResult?.allEvents?.Count ?? 0}개");
        }
        else
        {
            Debug.LogWarning("❌ GameHistoryManager 없음");
        }

        // StockManager 상태
        if (StockManager.Instance != null)
        {
            var holdings = StockManager.Instance.GetAllHoldings();
            Debug.Log($"✅ StockManager 연결됨");
            Debug.Log($"  - 현재 보유종목: {holdings?.Count ?? 0}개");
        }
        else
        {
            Debug.LogWarning("❌ StockManager 없음");
        }

        // PortfolioManager 상태
        if (PortfolioManager.Instance != null)
        {
            Debug.Log($"✅ PortfolioManager 연결됨");
        }
        else
        {
            Debug.LogWarning("❌ PortfolioManager 없음");
        }

        // GameManager 상태
        if (GameManager.Instance != null)
        {
            var scheduledEvents = GameManager.Instance.GetScheduledEvents();
            Debug.Log($"✅ GameManager 연결됨");
            Debug.Log($"  - 현재 턴: {GameManager.Instance.CurrentTurn}");
            Debug.Log($"  - 스케줄된 이벤트: {scheduledEvents?.Count ?? 0}개");
        }
        else
        {
            Debug.LogWarning("❌ GameManager 없음");
        }
    }

    [ContextMenu("강제로 샘플 데이터 생성")]
    void DebugCreateSampleData()
    {
        Debug.Log("🧪 강제로 샘플 데이터 생성 중...");

        // GameHistoryManager에 강제로 샘플 데이터 추가
        if (GameHistoryManager.Instance != null)
        {
            // 샘플 매수 기록
            GameHistoryManager.Instance.OnStockPurchased("SmartTech", 5, 45000f, 1125f);
            GameHistoryManager.Instance.OnStockPurchased("NeoChips", 3, 28500f, 855f);

            // 샘플 매도 기록
            GameHistoryManager.Instance.OnStockSold("SmartTech", 2, 52000f, 1040f);

            // 샘플 이벤트 기록
            GameHistoryManager.Instance.OnEventOccurred("AI 기술 혁신", "테스트 이벤트입니다", StockSector.TECH, 15f, 3);

            Debug.Log("✅ 샘플 데이터 생성 완료");

            // 현재 탭 새로고침
            RefreshCurrentTab();
        }
        else
        {
            Debug.LogError("❌ GameHistoryManager가 없어서 샘플 데이터를 생성할 수 없습니다.");
        }
    }

    #endregion
}