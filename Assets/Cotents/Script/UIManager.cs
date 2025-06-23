using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 게임 UI 전체를 관리하는 통합 매니저 (성능 최적화 버전)
/// GameManager와 연동하여 실시간 정보 표시
/// 기존 기능 100% 유지 + 더티 플래그 최적화 추가
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Header UI 참조")]
    public TextMeshProUGUI headerTitleText;         // "투자 수익률 순위"
    public TextMeshProUGUI headerSubtitleText;      // "실시간 등락률 기준 정렬"
    public TextMeshProUGUI headerYearText;          // "2024"
    public TextMeshProUGUI roundText;               // "라운드 3/10"
    public TextMeshProUGUI timeRemainingText;       // "남은 시간: 25초"
    public TextMeshProUGUI cashBalanceText;         // "보유 자금: 1,000,000원"
    public TextMeshProUGUI totalAssetText;          // "총 자산: 1,200,000원"

    [Header("포트폴리오 및 종목들")]
    public TextMeshProUGUI stocksText;              // 포트폴리오
    public TextMeshProUGUI portfolioValueText;      // 보유중
    public TextMeshProUGUI portfolioChangeText;     // 보유중 변화율

    [Header("게임 상태 UI (선택사항)")]
    public TextMeshProUGUI gameStatusText;          // 게임 상태 표시
    public Button startGameButton;                  // 게임 시작 버튼
    public Button resetGameButton;                  // 게임 리셋 버튼
    public GameObject gameOverPanel;                // 게임 종료 패널
    public TextMeshProUGUI finalResultText;        // 최종 결과 텍스트

    [Header("이벤트 알림 UI (선택사항)")]
    public GameObject eventPopup;                   // 이벤트 팝업 패널
    public TextMeshProUGUI eventTitleText;         // 이벤트 제목
    public TextMeshProUGUI eventDescriptionText;   // 이벤트 설명
    public Button eventConfirmButton;              // 이벤트 확인 버튼

    [Header("게임 데이터")]
    public int initialCash = 1000000;               // 초기 자금 100만원
    public float turnDuration = 30f;                // 턴당 30초

    [Header("🚀 성능 최적화 설정 (추가)")]
    [Range(0.05f, 0.5f)]
    public float dynamicUIUpdateInterval = 0.1f;    // 동적 UI 업데이트 주기 (초)
    public bool enablePerformanceOptimization = true; // 성능 최적화 활성화
    public bool enablePerformanceLogging = false;   // 성능 로깅

    [Header("색상 설정")]
    public Color profitColor = Color.red;           // 수익 색상
    public Color lossColor = Color.blue;            // 손실 색상
    public Color neutralColor = Color.gray;         // 중립 색상

    [Header("디버그")]
    public bool enableDebugLog = true;
    public bool showGameStatus = true;              // 게임 상태 텍스트 표시 여부

    // 🔥 기존 게임 상태 변수들 (그대로 유지)
    private int currentCash;
    private int totalAsset;
    private float remainingTime;
    private bool isTimerRunning = false;
    private Coroutine timerCoroutine;
    private bool isGameManagerMode = false;         // GameManager 연동 모드

    // 🚀 최적화: 더티 플래그 시스템 (기존 기능에 영향 없음)
    [System.Flags]
    private enum UIUpdateFlags
    {
        None = 0,
        StaticText = 1 << 0,    // 정적 텍스트 (언어 변경시)
        DynamicText = 1 << 1,   // 동적 텍스트 (게임 데이터)
        GameStatus = 1 << 2,    // 게임 상태
        All = ~0                // 모든 UI
    }

    private UIUpdateFlags pendingUpdates = UIUpdateFlags.None;
    private Coroutine optimizedUpdateCoroutine;

    // 🚀 최적화: 캐시된 값들 (변경 감지용)
    private int lastCash = -1;
    private int lastTotalAsset = -1;
    private float lastRemainingTime = -1f;
    private int lastTurn = -1;
    private GameState lastGameState = GameState.WaitingToStart;

    // 싱글톤 패턴
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<UIManager>();
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
        // 로컬라이징 매니저 초기화 대기
        StartCoroutine(WaitForLocalizationAndInitialize());
    }

    /// <summary>
    /// 로컬라이징 초기화 완료 후 UI 시작
    /// </summary>
    IEnumerator WaitForLocalizationAndInitialize()
    {
        // CSVLocalizationManager 초기화 완료까지 대기
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        if (enableDebugLog)
            Debug.Log("⏳ UIManager: 로컬라이징 초기화 완료, UI 시작");

        InitializeUI();
        InitializeGameData();
        SetupUIButtons();

        // 로컬라이징 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        // GameManager 이벤트 구독
        StartCoroutine(SubscribeToGameManagerEvents());

        // 🚀 최적화 시스템 시작 (로컬라이징 완료 후)
        if (enablePerformanceOptimization)
        {
            StartCoroutine(WaitForOptimizationStart());
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }

        // GameManager 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= OnGameManagerTurnChanged;
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnTurnTimerUpdate -= OnTurnTimerUpdate;
            GameManager.Instance.OnEventTriggered -= OnEventTriggered;
            GameManager.Instance.OnGameCompleted -= OnGameCompleted;
        }

        // 🚀 최적화 코루틴 정리
        if (optimizedUpdateCoroutine != null)
            StopCoroutine(optimizedUpdateCoroutine);
    }

    #region 🚀 성능 최적화 시스템 (기존 기능에 영향 없음)

    /// <summary>
    /// 🔧 로컬라이징 완료 후 최적화 시스템 시작
    /// </summary>
    IEnumerator WaitForOptimizationStart()
    {
        // CSVLocalizationManager 완전 초기화 대기
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        // 추가로 한 프레임 더 대기 (안전장치)
        yield return null;

        StartOptimizedUpdateSystem();

        if (enableDebugLog)
            Debug.Log("🚀 로컬라이징 완료 후 최적화 시스템 시작");
    }

    /// <summary>
    /// 최적화된 업데이트 시스템 시작
    /// </summary>
    void StartOptimizedUpdateSystem()
    {
        // 모든 UI를 초기에 업데이트
        MarkForUpdate(UIUpdateFlags.All);

        // 최적화된 업데이트 루프 시작
        if (optimizedUpdateCoroutine != null)
            StopCoroutine(optimizedUpdateCoroutine);
        optimizedUpdateCoroutine = StartCoroutine(OptimizedUpdateLoop());

        if (enableDebugLog)
            Debug.Log("🚀 UI 성능 최적화 시스템 시작");
    }

    /// <summary>
    /// 최적화된 업데이트 루프
    /// </summary>
    IEnumerator OptimizedUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(dynamicUIUpdateInterval);

            if (pendingUpdates != UIUpdateFlags.None)
            {
                float startTime = enablePerformanceLogging ? Time.realtimeSinceStartup : 0f;

                ProcessPendingUpdates();

                if (enablePerformanceLogging)
                {
                    float updateTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    Debug.Log($"🔧 최적화 UI 업데이트: {updateTime:F2}ms (플래그: {pendingUpdates})");
                }

                pendingUpdates = UIUpdateFlags.None;
            }
        }
    }

    /// <summary>
    /// 대기 중인 업데이트 처리
    /// </summary>
    void ProcessPendingUpdates()
    {
        // 🔧 로컬라이징 매니저 안전 체크
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            if (enablePerformanceLogging)
                Debug.LogWarning("⚠️ 로컬라이징 준비 안됨, 업데이트 연기");
            return;
        }

        if (pendingUpdates.HasFlag(UIUpdateFlags.StaticText))
        {
            UpdateStaticTexts();
        }

        if (pendingUpdates.HasFlag(UIUpdateFlags.DynamicText))
        {
            UpdateDynamicTextsOptimized();
        }

        if (pendingUpdates.HasFlag(UIUpdateFlags.GameStatus))
        {
            UpdateGameStatusUI();
        }
    }

    /// <summary>
    /// 업데이트 필요 마킹
    /// </summary>
    void MarkForUpdate(UIUpdateFlags flags)
    {
        pendingUpdates |= flags;
    }

    /// <summary>
    /// 🚀 최적화된 동적 텍스트 업데이트 (변경 감지 기반)
    /// </summary>
    void UpdateDynamicTextsOptimized()
    {
        // 🔧 로컬라이징 매니저 안전 체크
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            if (enablePerformanceLogging)
                Debug.LogWarning("⚠️ 로컬라이징이 아직 준비되지 않음");
            return;
        }

        var localizationManager = CSVLocalizationManager.Instance;
        bool hasChanges = false;

        // 턴 정보 체크
        int currentTurn = GetCurrentTurnNumber();
        if (currentTurn != lastTurn)
        {
            if (roundText != null)
            {
                string roundFormat = localizationManager.GetLocalizedText("ui_round_format");
                if (GameManager.Instance != null)
                {
                    roundText.text = string.Format(roundFormat, GameManager.Instance.CurrentTurn, GameManager.Instance.maxTurns);
                }
                else if (StockManager.Instance != null)
                {
                    roundText.text = string.Format(roundFormat, StockManager.Instance.currentTurn, StockManager.Instance.maxTurns);
                }
            }
            lastTurn = currentTurn;
            hasChanges = true;
        }

        // 시간 체크 (0.1초 단위로만 업데이트)
        float roundedTime = Mathf.Round(remainingTime * 10f) / 10f;
        if (Mathf.Abs(roundedTime - lastRemainingTime) >= 0.05f)
        {
            if (timeRemainingText != null)
            {
                string timeFormat = localizationManager.GetLocalizedText("ui_time_format");
                timeRemainingText.text = string.Format(timeFormat, Mathf.Ceil(remainingTime));
            }
            lastRemainingTime = roundedTime;
            hasChanges = true;
        }

        // 현금 체크
        if (currentCash != lastCash)
        {
            if (cashBalanceText != null)
            {
                string moneyFormat = localizationManager.GetLocalizedText("ui_money_format");
                cashBalanceText.text = string.Format(moneyFormat, currentCash);
            }
            lastCash = currentCash;
            hasChanges = true;
        }

        // 총자산 체크
        if (totalAsset != lastTotalAsset)
        {
            if (totalAssetText != null)
            {
                string totalAssetFormat = localizationManager.GetLocalizedText("ui_money_format");
                totalAssetText.text = string.Format(totalAssetFormat, totalAsset);

                // 수익/손실에 따른 색상 변경
                if (totalAsset > initialCash)
                    totalAssetText.color = profitColor;
                else if (totalAsset < initialCash)
                    totalAssetText.color = lossColor;
                else
                    totalAssetText.color = neutralColor;
            }
            lastTotalAsset = totalAsset;
            hasChanges = true;
        }

        if (enablePerformanceLogging && hasChanges)
        {
            Debug.Log($"💡 최적화 업데이트 완료: 현금={currentCash:N0}, 자산={totalAsset:N0}, 시간={remainingTime:F1}");
        }
    }

    /// <summary>
    /// 현재 턴 번호 가져오기
    /// </summary>
    int GetCurrentTurnNumber()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.CurrentTurn;
        else if (StockManager.Instance != null)
            return StockManager.Instance.currentTurn;
        return 1;
    }

    #endregion

    #region 🔥 기존 UI 메서드들 (100% 동일하게 유지)

    /// <summary>
    /// UI 초기화
    /// </summary>
    void InitializeUI()
    {
        UpdateStaticTexts();
        UpdateDynamicTexts();
        UpdateGameStatusUI();

        // 초기 상태 설정
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (eventPopup != null)
            eventPopup.SetActive(false);

        if (enableDebugLog)
            Debug.Log("✅ UIManager 초기화 완료");
    }

    /// <summary>
    /// 게임 데이터 초기화
    /// </summary>
    void InitializeGameData()
    {
        currentCash = initialCash;
        totalAsset = initialCash;
        remainingTime = turnDuration;
    }

    /// <summary>
    /// UI 버튼 설정
    /// </summary>
    void SetupUIButtons()
    {
        // 게임 시작 버튼
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }

        // 게임 리셋 버튼
        if (resetGameButton != null)
        {
            resetGameButton.onClick.RemoveAllListeners();
            resetGameButton.onClick.AddListener(OnResetGameButtonClicked);
        }

        // 이벤트 확인 버튼
        if (eventConfirmButton != null)
        {
            eventConfirmButton.onClick.RemoveAllListeners();
            eventConfirmButton.onClick.AddListener(OnEventConfirmButtonClicked);
        }
    }

    /// <summary>
    /// 정적 텍스트 업데이트 (로컬라이징)
    /// </summary>
    void UpdateStaticTexts()
    {
        if (CSVLocalizationManager.Instance == null) return;

        var localizationManager = CSVLocalizationManager.Instance;

        // Header 텍스트
        if (headerTitleText != null)
            headerTitleText.text = localizationManager.GetLocalizedText("header_title");

        if (headerSubtitleText != null)
            headerSubtitleText.text = localizationManager.GetLocalizedText("header_subtitle");

        if (headerYearText != null)
            headerYearText.text = localizationManager.GetLocalizedText("ui_year");

        // 포트폴리오 텍스트
        if (stocksText != null)
            stocksText.text = localizationManager.GetLocalizedText("ui_stocksText");
        if (portfolioValueText != null)
            portfolioValueText.text = localizationManager.GetLocalizedText("ui_portfolio_value");
        if (portfolioChangeText != null)
            portfolioChangeText.text = localizationManager.GetLocalizedText("ui_portfolio_change");

        // 버튼 텍스트
        if (startGameButton != null)
        {
            var buttonText = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = localizationManager.GetLocalizedText("ui_start_game");
        }

        if (resetGameButton != null)
        {
            var buttonText = resetGameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = localizationManager.GetLocalizedText("ui_reset_game");
        }
    }

    /// <summary>
    /// 동적 텍스트 업데이트 (게임 데이터) - 🔥 기존 로직 100% 유지
    /// </summary>
    void UpdateDynamicTexts()
    {
        // 🚀 최적화가 활성화된 경우 플래그만 설정
        if (enablePerformanceOptimization)
        {
            MarkForUpdate(UIUpdateFlags.DynamicText);
            return;
        }

        // 🔥 기존 로직 (최적화 비활성화시)
        if (CSVLocalizationManager.Instance == null) return;

        var localizationManager = CSVLocalizationManager.Instance;

        // 라운드 표시 - GameManager 우선
        if (roundText != null)
        {
            string roundFormat = localizationManager.GetLocalizedText("ui_round_format");

            if (GameManager.Instance != null)
            {
                roundText.text = string.Format(roundFormat, GameManager.Instance.CurrentTurn, GameManager.Instance.maxTurns);
            }
            else if (StockManager.Instance != null)
            {
                // 폴백: 기존 방식
                roundText.text = string.Format(roundFormat, StockManager.Instance.currentTurn, StockManager.Instance.maxTurns);
            }
        }

        // 남은 시간
        if (timeRemainingText != null)
        {
            string timeFormat = localizationManager.GetLocalizedText("ui_time_format");
            timeRemainingText.text = string.Format(timeFormat, Mathf.Ceil(remainingTime));
        }

        // 보유 자금
        if (cashBalanceText != null)
        {
            string moneyFormat = localizationManager.GetLocalizedText("ui_money_format");
            cashBalanceText.text = string.Format(moneyFormat, currentCash);
        }

        // 총 자산 (색상 적용)
        if (totalAssetText != null)
        {
            string totalAssetFormat = localizationManager.GetLocalizedText("ui_money_format");
            totalAssetText.text = string.Format(totalAssetFormat, totalAsset);

            // 수익/손실에 따른 색상 변경
            if (totalAsset > initialCash)
                totalAssetText.color = profitColor;
            else if (totalAsset < initialCash)
                totalAssetText.color = lossColor;
            else
                totalAssetText.color = neutralColor;
        }
    }

    /// <summary>
    /// 게임 상태 UI 업데이트
    /// </summary>
    void UpdateGameStatusUI()
    {
        if (!showGameStatus || gameStatusText == null) return;

        string statusText = "대기 중...";

        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.CurrentState)
            {
                case GameState.WaitingToStart:
                    statusText = "게임 시작 대기 중";
                    break;
                case GameState.Ready:
                    statusText = "게임 준비 완료";
                    break;
                case GameState.Playing:
                    statusText = $"게임 진행 중 (턴 {GameManager.Instance.CurrentTurn})";
                    break;
                case GameState.Finished:
                    statusText = "게임 종료";
                    break;
            }
        }

        gameStatusText.text = statusText;
    }

    /// <summary>
    /// 언어 변경시 호출
    /// </summary>
    void OnLanguageChanged(Language newLanguage)
    {
        // 🚀 최적화: 플래그 설정
        if (enablePerformanceOptimization)
        {
            MarkForUpdate(UIUpdateFlags.StaticText | UIUpdateFlags.DynamicText);
        }
        else
        {
            // 🔥 기존 로직
            UpdateStaticTexts();
            UpdateDynamicTexts();
        }

        if (enableDebugLog)
            Debug.Log($"🌍 UIManager 언어 변경: {newLanguage}");
    }

    #endregion

    #region GameManager 연동 (🔥 기존 로직 100% 유지)

    /// <summary>
    /// GameManager 이벤트 구독
    /// </summary>
    IEnumerator SubscribeToGameManagerEvents()
    {
        // GameManager 초기화 대기
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        // GameManager 이벤트 구독
        GameManager.Instance.OnTurnChanged += OnGameManagerTurnChanged;
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        GameManager.Instance.OnTurnTimerUpdate += OnTurnTimerUpdate;
        GameManager.Instance.OnEventTriggered += OnEventTriggered;
        GameManager.Instance.OnGameCompleted += OnGameCompleted;

        isGameManagerMode = true;

        if (enableDebugLog)
            Debug.Log("🔗 UIManager: GameManager 이벤트 구독 완료");
    }

    /// <summary>
    /// GameManager 턴 변경 이벤트 처리
    /// </summary>
    void OnGameManagerTurnChanged(int newTurn)
    {
        // 🚀 최적화: 플래그 설정
        if (enablePerformanceOptimization)
        {
            MarkForUpdate(UIUpdateFlags.DynamicText | UIUpdateFlags.GameStatus);
        }
        else
        {
            // 🔥 기존 로직
            UpdateDynamicTexts();
            UpdateGameStatusUI();
        }

        if (enableDebugLog)
            Debug.Log($"🎮 UIManager: 턴 {newTurn}로 변경됨");
    }

    /// <summary>
    /// GameManager 게임 상태 변경 이벤트 처리
    /// </summary>
    void OnGameStateChanged(GameState newState)
    {
        // 🚀 최적화: 플래그 설정
        if (enablePerformanceOptimization)
        {
            MarkForUpdate(UIUpdateFlags.GameStatus);
        }
        else
        {
            // 🔥 기존 로직
            UpdateGameStatusUI();
        }

        switch (newState)
        {
            case GameState.Ready:
                // 게임 준비 완료시 버튼 활성화
                if (startGameButton != null)
                    startGameButton.interactable = true;
                break;

            case GameState.Playing:
                // 게임 시작시 버튼 비활성화
                if (startGameButton != null)
                    startGameButton.interactable = false;
                break;

            case GameState.Finished:
                // 게임 종료시 타이머 정리 및 버튼 다시 활성화
                EndTurn();
                if (startGameButton != null)
                    startGameButton.interactable = true;
                break;
        }

        if (enableDebugLog)
            Debug.Log($"🎮 UIManager: 게임 상태 변경 - {newState}");
    }

    /// <summary>
    /// GameManager 타이머 업데이트 처리
    /// </summary>
    void OnTurnTimerUpdate(float remainingTime)
    {
        this.remainingTime = remainingTime;

        // 🚀 최적화: 플래그 설정
        if (enablePerformanceOptimization)
        {
            MarkForUpdate(UIUpdateFlags.DynamicText);
        }
        else
        {
            // 🔥 기존 로직
            UpdateDynamicTexts();
        }
    }

    /// <summary>
    /// GameManager 이벤트 발생 처리
    /// </summary>
    void OnEventTriggered(TurnEvent turnEvent)
    {
        ShowEventPopup(turnEvent);
    }

    /// <summary>
    /// GameManager 게임 완료 처리
    /// </summary>
    void OnGameCompleted(GameResult result)
    {
        ShowGameResult(result);
    }

    #endregion

    #region 타이머 시스템 (🔥 기존 로직 100% 유지)

    /// <summary>
    /// 턴 시작 (GameManager 연동)
    /// </summary>
    public void StartTurn()
    {
        // GameManager가 타이머를 관리하는 경우
        if (isGameManagerMode && GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            isTimerRunning = true;

            if (enableDebugLog)
                Debug.Log($"⏰ UIManager: GameManager 타이머와 연동");

            return;
        }

        // 폴백: 기존 방식 (GameManager가 없을 때)
        remainingTime = turnDuration;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(TurnTimer());
        isTimerRunning = true;

        UpdateDynamicTexts();

        if (enableDebugLog)
            Debug.Log($"⏰ UIManager 자체 타이머 시작: {turnDuration}초");
    }

    /// <summary>
    /// 턴 종료
    /// </summary>
    public void EndTurn()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        isTimerRunning = false;

        if (enableDebugLog)
            Debug.Log("⏰ UIManager 턴 타이머 종료");
    }

    /// <summary>
    /// 턴 타이머 코루틴 (폴백용) - 🔥 기존 로직 100% 유지
    /// </summary>
    IEnumerator TurnTimer()
    {
        while (remainingTime > 0 && isTimerRunning)
        {
            yield return new WaitForSeconds(0.1f);
            remainingTime -= 0.1f;

            // UI 업데이트 (1초마다) - 🔥 기존 로직 유지
            if (Mathf.RoundToInt(remainingTime * 10) % 10 == 0)
            {
                UpdateDynamicTexts();
            }
        }

        // 시간 종료
        if (remainingTime <= 0)
        {
            remainingTime = 0;
            UpdateDynamicTexts();
            OnTurnTimeOut();
        }
    }

    /// <summary>
    /// 시간 초과시 호출
    /// </summary>
    void OnTurnTimeOut()
    {
        if (enableDebugLog)
            Debug.Log("⏰ UIManager: 턴 시간 초과!");

        // GameManager가 있으면 GameManager가 처리, 없으면 StockManager에 알림
        if (isGameManagerMode && GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            // GameManager가 자체적으로 턴 관리하므로 여기서는 아무것도 하지 않음
            if (enableDebugLog)
                Debug.Log("⏰ GameManager가 턴을 관리 중이므로 UIManager는 대기");
        }
        else if (StockManager.Instance != null)
        {
            // 폴백: 기존 방식
            StockManager.Instance.NextTurn();
        }
    }

    #endregion

    #region 이벤트 팝업 시스템 (🔥 기존 로직 100% 유지)

    /// <summary>
    /// 이벤트 팝업 표시
    /// </summary>
    void ShowEventPopup(TurnEvent turnEvent)
    {
        if (eventPopup == null) return;

        eventPopup.SetActive(true);

        if (eventTitleText != null)
            eventTitleText.text = turnEvent.title;

        if (eventDescriptionText != null)
            eventDescriptionText.text = turnEvent.description;

        if (enableDebugLog)
            Debug.Log($"📰 이벤트 팝업 표시: {turnEvent.title}");
    }

    /// <summary>
    /// 이벤트 팝업 닫기
    /// </summary>
    void OnEventConfirmButtonClicked()
    {
        if (eventPopup != null)
            eventPopup.SetActive(false);

        if (enableDebugLog)
            Debug.Log("📰 이벤트 팝업 닫기");
    }

    #endregion

    #region 게임 결과 시스템 (🔥 기존 로직 100% 유지)

    /// <summary>
    /// 게임 결과 표시
    /// </summary>
    void ShowGameResult(GameResult result)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (finalResultText != null)
        {
            string resultText = $"게임 완료!\n\n";
            resultText += $"초기 자금: {result.initialCash:N0}원\n";
            resultText += $"최종 자산: {result.finalAsset:N0}원\n";
            resultText += $"총 수익: {result.totalProfit:N0}원 ({result.profitRate:F1}%)\n";
            resultText += $"납부 세금: {result.taxPaid:N0}원\n\n";
            resultText += $"라이프스타일: {GetLifestyleGradeName(result.lifestyleGrade)}";

            finalResultText.text = resultText;

            // 결과에 따른 색상 적용
            if (result.totalProfit > 0)
                finalResultText.color = profitColor;
            else if (result.totalProfit < 0)
                finalResultText.color = lossColor;
            else
                finalResultText.color = neutralColor;
        }

        if (enableDebugLog)
            Debug.Log($"🏆 게임 결과 표시: {result.lifestyleGrade}");
    }

    /// <summary>
    /// 라이프스타일 등급명 가져오기
    /// </summary>
    string GetLifestyleGradeName(LifestyleGrade grade)
    {
        switch (grade)
        {
            case LifestyleGrade.Upper: return "상류층";
            case LifestyleGrade.MiddleUpper: return "중상류층";
            case LifestyleGrade.Middle: return "평범층";
            case LifestyleGrade.Lower: return "하류층";
            default: return "알 수 없음";
        }
    }

    #endregion

    #region 버튼 이벤트 (🔥 기존 로직 100% 유지)

    /// <summary>
    /// 게임 시작 버튼 클릭
    /// </summary>
    void OnStartGameButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();

            if (enableDebugLog)
                Debug.Log("🎮 게임 시작 버튼 클릭");
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager가 없어서 게임을 시작할 수 없습니다.");
        }
    }

    /// <summary>
    /// 게임 리셋 버튼 클릭
    /// </summary>
    void OnResetGameButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();

            if (enableDebugLog)
                Debug.Log("🔄 게임 리셋 버튼 클릭");
        }

        // UI도 리셋
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (eventPopup != null)
            eventPopup.SetActive(false);

        InitializeGameData();
        UpdateDynamicTexts();
        UpdateGameStatusUI();
    }

    #endregion

    #region 공개 메서드 (🔥 기존 API 100% 유지)

    /// <summary>
    /// 현금 업데이트
    /// </summary>
    public void UpdateCash(int newCash)
    {
        currentCash = newCash;
        UpdateDynamicTexts();
    }

    /// <summary>
    /// 총 자산 업데이트
    /// </summary>
    public void UpdateTotalAsset(int newTotalAsset)
    {
        totalAsset = newTotalAsset;
        UpdateDynamicTexts();
    }

    /// <summary>
    /// 게임 데이터 업데이트 (한 번에)
    /// </summary>
    public void UpdateGameData(int cash, int asset)
    {
        currentCash = cash;
        totalAsset = asset;
        UpdateDynamicTexts();
    }

    /// <summary>
    /// 현재 게임 상태 가져오기
    /// </summary>
    public int GetCurrentCash() => currentCash;
    public int GetTotalAsset() => totalAsset;
    public float GetRemainingTime() => remainingTime;
    public bool IsTimerRunning() => isTimerRunning;

    #endregion

    #region 디버그 메서드 (🔥 기존 + 최적화 관련 추가)

    /// <summary>
    /// UI 강제 업데이트
    /// </summary>
    [ContextMenu("UI 강제 업데이트")]
    public void ForceUpdateUI()
    {
        UpdateStaticTexts();
        UpdateDynamicTexts();
        UpdateGameStatusUI();

        if (enableDebugLog)
            Debug.Log("🔄 UI 강제 업데이트 완료");
    }

    /// <summary>
    /// 🚀 최적화 시스템 토글
    /// </summary>
    [ContextMenu("성능 최적화 토글")]
    public void ToggleOptimization()
    {
        enablePerformanceOptimization = !enablePerformanceOptimization;

        if (enablePerformanceOptimization)
        {
            StartOptimizedUpdateSystem();
            Debug.Log("🚀 성능 최적화 활성화");
        }
        else
        {
            if (optimizedUpdateCoroutine != null)
            {
                StopCoroutine(optimizedUpdateCoroutine);
                optimizedUpdateCoroutine = null;
            }
            Debug.Log("🔥 기존 방식으로 복귀");
        }
    }

 

    #endregion
}