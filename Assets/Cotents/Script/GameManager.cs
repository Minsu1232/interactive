using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 플로우를 관리하는 메인 매니저
/// 턴 진행, 이벤트 발생, 게임 상태 관리, 분산투자 보너스 적용
/// 수수료 0.25% 즉시 처리 시스템
/// 🆕 GameHistoryManager 연동 추가
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("게임 설정")]
    public int maxTurns = 10;                   // 총 턴 수
    public float turnDuration = 30f;            // 턴당 시간 (초)
    public int initialCash = 1000000;           // 초기 자금
    private bool eventAppliedThisTurn = false;  // ✅ 현재 턴에 이벤트가 적용되었는지 추적하는 플래그

    // ✅ 게임 종료 시 저장할 데이터
    private int finalCash = 0;
    private int finalStockValue = 0;
    private int finalTotalAsset = 0;
    private bool gameDataSaved = false;
    [Header("수수료 설정")]
    [Range(0f, 10f)]
    public float tradingFeeRate = 0.25f;        // 매매 수수료 0.25% (매수/매도 동일)

    [Header("라이프스타일 등급 기준 (원)")]
    public int upperGradeThreshold = 1500000;   // 150만원 이상
    public int middleUpperThreshold = 1300000;  // 130만원 이상
    public int middleThreshold = 1000000;       // 100만원 이상
    public int underThreshold = 0;              // 0원 이상

    [Header("디버그")]
    public bool enableDebugLog = true;
    public bool skipTimer = false;              // 테스트용: 타이머 건너뛰기

    // 게임 상태
    private int currentTurn = 1;
    private GameState currentState = GameState.WaitingToStart;
    private bool isGameActive = false;
    private Coroutine gameFlowCoroutine;
    private float currentTurnRemainingTime = 0f;

    // 분산투자 보너스 추적
    private int maxSectorsDiversified = 0;
    private Dictionary<int, float> sectorBonusHistory = new Dictionary<int, float>();

    // 이벤트 시스템
    public Dictionary<int, TurnEvent> scheduledEvents;

    // 싱글톤 패턴
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GameManager>();
            return instance;
        }
    }

    // 게임 상태 이벤트
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<int> OnTurnChanged;
    public System.Action<TurnEvent> OnEventTriggered;
    public System.Action<GameResult> OnGameCompleted;
    public System.Action<float> OnTurnTimerUpdate;
    public System.Action<int> OnTurnDiversificationUpdated;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEvents();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(WaitForManagersAndStart());
    }

    /// <summary>
    /// 모든 매니저 초기화 대기 후 게임 시작 준비
    /// </summary>
    IEnumerator WaitForManagersAndStart()
    {
        while (CSVLocalizationManager.Instance == null ||
            !CSVLocalizationManager.Instance.IsInitialized ||
            StockManager.Instance == null ||
            UIManager.Instance == null)
        {
            yield return null;
        }

        // ✅ 언어 변경 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += (language) => OnLanguageChanged();
        }

        if (enableDebugLog)
            Debug.Log("🎮 GameManager: 모든 매니저 초기화 완료, 게임 준비됨");

        PrepareGame();
    }

    /// <summary>
    /// 이벤트 스케줄 초기화
    /// </summary>
    void InitializeEvents()
    {
        scheduledEvents = new Dictionary<int, TurnEvent>
        {
            [3] = new TurnEvent
            {
                eventKey = "ai_innovation",                    // ✅ 이벤트 식별 키
                titleKey = "event_title_ai_innovation",        // ✅ 제목 로컬라이징 키
                descriptionKey = "event_desc_ai_innovation",   // ✅ 설명 로컬라이징 키
                newsKey = "news_event_ai_innovation",          // ✅ 뉴스 로컬라이징 키
                previewKey = "news_preview_tech",              // ✅ 예고 로컬라이징 키
                effects = new List<StockEffect>
            {   new StockEffect {
                    isGlobal = true,
                    useIndividualVariation = true,
                    changeRate = 0f,        // 사용 안함
                    variationMin = -2f,     // 다른 섹터 -2~+2%
                    variationMax = 2f
                },
                new StockEffect {
                    sector = StockSector.SEM,
                    changeRate = 25f,
                    useIndividualVariation = true,
                    variationMin = -3f,     // 22~28% 범위
                    variationMax = 3f
                },
                new StockEffect {
                    sector = StockSector.TECH,
                    changeRate = 15f,
                    useIndividualVariation = true,
                    variationMin = -3f,     // 12~18% 범위
                    variationMax = 3f
                }
             
            }
            },

            [5] = new TurnEvent
            {
                eventKey = "energy_policy",
                titleKey = "event_title_energy_policy",
                descriptionKey = "event_desc_energy_policy",
                newsKey = "news_event_energy_policy",
                previewKey = "news_preview_energy",
                effects = new List<StockEffect>
            {
                     new StockEffect {
                    isGlobal = true,
                    useIndividualVariation = true,
                    changeRate = 0f,        // 사용 안함
                    variationMin = -4f,     // 다른 섹터 -2~+2%
                    variationMax = 4f
                },
                new StockEffect {
                    sector = StockSector.EV,
                    changeRate = 20f,       // 기존 Random.Range(10f, 30f) 평균값
                    useIndividualVariation = true,
                    variationMin = -10f,    // 10~30% 범위 유지
                    variationMax = 10f
                }
              
            }
            },

            [7] = new TurnEvent
            {
                eventKey = "interest_rate",
                titleKey = "event_title_interest_rate",
                descriptionKey = "event_desc_interest_rate",
                newsKey = "news_event_rate_hike",
                previewKey = "news_preview_monetary",
                effects = new List<StockEffect>
            {
                       new StockEffect {
                    isGlobal = true,
                    useIndividualVariation = true,
                    changeRate = 0f,        // 사용 안함
                    variationMin = -5f,     // 기존 Random.Range(-5f, 10f) 유지
                    variationMax = 10f
                },
                new StockEffect {
                    sector = StockSector.TECH,
                    changeRate = -10f,      // 기존 Random.Range(-15f, -5f) 평균값
                    useIndividualVariation = true,
                    variationMin = -5f,     // -15~-5% 범위 유지
                    variationMax = 5f
                },
                new StockEffect {
                    sector = StockSector.CORP,
                    changeRate = 7.5f,      // 기존 Random.Range(5f, 10f) 평균값
                    useIndividualVariation = true,
                    variationMin = -2.5f,   // 5~10% 범위 유지
                    variationMax = 2.5f
                }
             
            }
            },

            [9] = new TurnEvent
            {
                eventKey = "crypto_regulation",
                titleKey = "event_title_crypto_regulation",
                descriptionKey = "event_desc_crypto_regulation",
                newsKey = "news_event_crypto_regulation",
                previewKey = "news_preview_crypto",
                effects = new List<StockEffect>
            {
                       new StockEffect {
                    isGlobal = true,
                    useIndividualVariation = true,
                    changeRate = 0f,        // 사용 안함
                    variationMin = -5f,     // 기존 Random.Range(-5f, 10f) 유지
                    variationMax = 10f
                },
                new StockEffect {
                    sector = StockSector.CRYPTO,
                    changeRate = -15f,      // 기존 Random.Range(-20f, -10f) 평균값
                    useIndividualVariation = true,
                    variationMin = -5f,     // -20~-10% 범위 유지
                    variationMax = 5f
                }
          
            }
            }
        };

        if (enableDebugLog)
            Debug.Log($"📅 이벤트 스케줄 초기화 완료 (완전한 키 기반): {scheduledEvents.Count}개 이벤트");
    }

    /// <summary>
    /// 게임 시작 준비
    /// </summary>
    void PrepareGame()
    {
        currentTurn = 1;
        maxSectorsDiversified = 0;
        sectorBonusHistory.Clear();
        ChangeGameState(GameState.Ready);

        if (enableDebugLog)
            Debug.Log("🎯 게임 준비 완료! StartGame() 호출 대기 중...");
        
    }

    /// <summary>
    /// 게임 시작 (외부에서 호출)
    /// </summary>
    [ContextMenu("게임 시작")]
    public void StartGame()
    {
        if (currentState != GameState.Ready)
        {
            Debug.LogWarning("⚠️ 게임을 시작할 수 없는 상태입니다.");
            return;
        }

        if (gameFlowCoroutine != null)
            StopCoroutine(gameFlowCoroutine);

        gameFlowCoroutine = StartCoroutine(GameFlowCoroutine());

        if (enableDebugLog)
            Debug.Log("🚀 게임 시작!");
    }

    /// <summary>
    /// 메인 게임 플로우 코루틴 - 히스토리 기록 추가
    /// </summary>
    IEnumerator GameFlowCoroutine()
    {
        isGameActive = true;
        ChangeGameState(GameState.Playing);

        // 10턴 진행
        for (currentTurn = 1; currentTurn <= maxTurns; currentTurn++)
        {
            if (enableDebugLog)
                Debug.Log($"🎮 === 턴 {currentTurn} 시작 ===");

            // ✅ 턴 시작할 때마다 이벤트 플래그 초기화
            eventAppliedThisTurn = false;

            // GameHistoryManager에 턴 시작 알림
            if (GameHistoryManager.Instance != null)
            {
                GameHistoryManager.Instance.OnTurnStart(currentTurn);
            }

            currentTurnRemainingTime = turnDuration;
            OnTurnChanged?.Invoke(currentTurn);
            OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);

            yield return null;

            // 이벤트 체크 및 적용
            yield return StartCoroutine(CheckAndApplyEvents());

            // 턴 진행 (30초 타이머)
            yield return StartCoroutine(PlayTurn());

            // 턴 종료 처리
            yield return StartCoroutine(EndTurn());

            // GameHistoryManager에 턴 종료 알림
            if (GameHistoryManager.Instance != null)
            {
                GameHistoryManager.Instance.OnTurnEnd(currentTurn);
            }

            if (enableDebugLog)
                Debug.Log($"✅ 턴 {currentTurn} 완료");
        }

        // 게임 종료
        yield return StartCoroutine(EndGame());
    }

    /// <summary>
    /// 이벤트 체크 및 적용 - 히스토리 기록 추가
    /// </summary>
    IEnumerator CheckAndApplyEvents()
    {
        if (scheduledEvents.ContainsKey(currentTurn))
        {
            var turnEvent = scheduledEvents[currentTurn];
            // ✅ 이벤트가 적용되었음을 플래그로 기록
            eventAppliedThisTurn = true;
            // ✅ 로컬라이징된 이벤트 정보 가져오기
            var localizedEvent = GetLocalizedTurnEvent(turnEvent);

            if (enableDebugLog)
                Debug.Log($"📰 이벤트 발생: {localizedEvent.title}");

            // ✅ GameHistoryManager에 로컬라이징된 제목으로 기록
            if (GameHistoryManager.Instance != null)
            {
                float avgImpact = 0f;
                if (turnEvent.effects != null && turnEvent.effects.Count > 0)
                {
                    foreach (var effect in turnEvent.effects)
                    {
                        avgImpact += effect.changeRate;
                    }
                    avgImpact /= turnEvent.effects.Count;
                }

                GameHistoryManager.Instance.OnEventOccurred(
                    localizedEvent.title,      // ✅ 로컬라이징된 제목 사용
                    localizedEvent.description, // ✅ 로컬라이징된 설명 사용
                    null, // 전체 시장 영향으로 처리
                    avgImpact,
                    currentTurn
                );
            }

            // ✅ 뉴스 티커에 로컬라이징된 이벤트 전달
            if (NewsTickerManager.Instance != null)
            {
                NewsTickerManager.Instance.ShowLocalizedEventNews(localizedEvent);
            }

            // ✅ 이벤트 트리거시에도 로컬라이징된 버전 사용
            OnEventTriggered?.Invoke(turnEvent); // 원본 이벤트는 effects 때문에 유지

            ApplyEventEffects(turnEvent);

            yield return new WaitForSeconds(3f);
        }
    }
    /// <summary>
    /// StockManager에 추가할 새로운 메서드 - 개별 종목 가격 변동
    /// </summary>
    public void ApplyIndividualStockChange(string stockKey, float changeRate)
    {
        if (StockManager.Instance == null) return;

        var stock = StockManager.Instance.GetStockData(stockKey);
        if (stock != null)
        {
            stock.UpdatePrice(changeRate); // StockData의 기존 메서드 사용

            if (enableDebugLog)
                Debug.Log($"📊 개별 변동: {stock.displayName} {changeRate:+0.1;-0.1}%");
        }
    }

    /// <summary>
    /// 섹터 내 종목별로 다른 변동률 적용
    /// </summary>
    void ApplySectorChangeWithVariation(StockSector sector, float baseChangeRate, float variationMin, float variationMax)
    {
        if (StockManager.Instance == null) return;

        // StockManager의 기존 메서드 사용
        var sectorStocks = StockManager.Instance.GetStocksBySector(sector);

        foreach (var stock in sectorStocks)
        {
            // 기본 변동률 + 개별 랜덤 변동
            float individualVariation = Random.Range(variationMin, variationMax);
            float finalChangeRate = baseChangeRate + individualVariation;

            // 개별 종목에 변동 적용
            ApplyIndividualStockChange(stock.stockKey, finalChangeRate);

            if (enableDebugLog)
            {
                Debug.Log($"📊 {stock.displayName}: 기본{baseChangeRate:+0;-0}% + 개별{individualVariation:+0.1;-0.1}% = {finalChangeRate:+0.1;-0.1}%");
            }
        }
    }

    /// <summary>
    /// 영향받지 않은 섹터들에 랜덤 효과 적용
    /// </summary>
    void ApplyRandomEffectToUnaffectedSectors(HashSet<StockSector> affectedSectors, float effectMin, float effectMax)
    {
        if (StockManager.Instance == null) return;

        // StockManager의 기존 메서드 사용
        var allStocks = StockManager.Instance.GetAllStocks();

        foreach (var stock in allStocks)
        {
            if (affectedSectors.Contains(stock.sector)) continue; // 이미 영향받은 섹터는 제외

            float randomEffect = Random.Range(effectMin, effectMax);
            ApplyIndividualStockChange(stock.stockKey, randomEffect);

            if (enableDebugLog)
            {
                Debug.Log($"🎲 {stock.displayName} (기타효과): {randomEffect:+0.1;-0.1}%");
            }
        }
    }

    /// <summary>
    /// 전체 종목에 개별 랜덤 효과 적용
    /// </summary>
    void ApplyRandomEffectToAllStocks(float effectMin, float effectMax)
    {
        if (StockManager.Instance == null) return;

        var allStocks = StockManager.Instance.GetAllStocks();

        foreach (var stock in allStocks)
        {
            float randomEffect = Random.Range(effectMin, effectMax);
            ApplyIndividualStockChange(stock.stockKey, randomEffect);

            if (enableDebugLog)
            {
                Debug.Log($"🌍 {stock.displayName} (전체효과): {randomEffect:+0.1;-0.1}%");
            }
        }
    }
    /// <summary>
    /// 개선된 이벤트 효과 적용 메서드 (GameManager.cs의 기존 메서드 대체)
    /// </summary>
    void ApplyEventEffects(TurnEvent turnEvent)
    {
        var affectedSectors = new HashSet<StockSector>();

        foreach (var effect in turnEvent.effects)
        {
            if (effect.isGlobal)
            {
                // 전체 시장에 종목별로 다른 랜덤 효과
                float globalMin = effect.useIndividualVariation ? effect.variationMin : effect.changeRate;
                float globalMax = effect.useIndividualVariation ? effect.variationMax : effect.changeRate;

                ApplyRandomEffectToAllStocks(globalMin, globalMax);
            }
            else
            {
                affectedSectors.Add(effect.sector);

                if (effect.useIndividualVariation)
                {
                    // ✅ 종목별 다른 변동
                    ApplySectorChangeWithVariation(
                        effect.sector,
                        effect.changeRate,
                        effect.variationMin,
                        effect.variationMax
                    );
                }
                else
                {
                    // 기존 방식: 섹터 내 동일 변동
                    StockManager.Instance.ApplySectorChange(effect.sector, effect.changeRate);
                }
            }
        }

        // 순위 재계산 및 UI 업데이트
        if (StockManager.Instance != null)
        {
            // StockManager의 기존 메서드들 사용
            StockManager.Instance.UpdateAllUI();
        }
    }

    /// <summary>
    /// 턴 진행 (30초 타이머)
    /// </summary>
    IEnumerator PlayTurn()
    {
        if (currentTurnRemainingTime != turnDuration)
        {
            if (enableDebugLog)
                Debug.LogWarning($"⚠️ 타이머 불일치 감지! 강제 초기화: {currentTurnRemainingTime} → {turnDuration}");

            currentTurnRemainingTime = turnDuration;
            OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartTurn();
        }

        if (skipTimer)
        {
            currentTurnRemainingTime = 1f;
            OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);
            yield return new WaitForSeconds(1f);
            currentTurnRemainingTime = 0f;
            OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);
        }
        else
        {
            while (currentTurnRemainingTime > 0)
            {
                yield return new WaitForSecondsRealtime(0.1f); // timeScale 무관하게 진행

                // ✅ timeScale이 0이 아닐 때만 시간 차감
                if (!Mathf.Approximately(Time.timeScale, 0f))
                {
                    currentTurnRemainingTime -= 0.1f;
                }

                if (Mathf.RoundToInt(currentTurnRemainingTime * 10) % 10 == 0)
                {
                    OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);
                }

                if (!isGameActive) break;
            }

            if (currentTurnRemainingTime <= 0)
            {
                currentTurnRemainingTime = 0f;
                OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.EndTurn();
        }
    }

    /// <summary>
    /// 턴 종료 처리
    /// </summary>
    IEnumerator EndTurn()
    {

        // ✅ 다음 턴에 이벤트가 있는지 미리 체크
        bool hasEventNextTurn = scheduledEvents.ContainsKey(currentTurn + 1);

        if (!hasEventNextTurn && StockManager.Instance != null)
        {
            StockManager.Instance.ApplyRandomPriceChanges();
            StockManager.Instance.UpdateAllUI();
            Debug.Log($"🔚 턴 {currentTurn} 종료: 다음 턴 이벤트 없음, 랜덤변동 적용");
        }
        else
        {
            Debug.Log($"🔚 턴 {currentTurn} 종료: 다음 턴 이벤트 있음, 랜덤변동 건너뜀");
        }

        TrackDiversificationProgress();

        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.UpdatePortfolioUI();
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 게임 종료 처리
    /// </summary>
    IEnumerator EndGame()
    {
        isGameActive = false;

        // ✅ 게임 종료 시 보유 주식 전량 강제 매도
        ForceFiniteSellAllStocks();

        // 게임 종료 시 현재 상태 저장
        SaveFinalGameData();
        ChangeGameState(GameState.Finished);

        if (enableDebugLog)
            Debug.Log($"🏁 게임 종료! 최종 자산: {finalTotalAsset:N0}원 저장됨");

        GameResult result = CalculateFinalResult();
        OnGameCompleted?.Invoke(result);

        yield return new WaitForSeconds(2f);
        // ✅ 셔터 효과로 씬 전환
        if (GameSceneShutterIntro.Instance != null)
        {
            GameSceneShutterIntro.Instance.StartEndGameShutter("EndScene");
            // SceneManager.LoadScene는 제거 (셔터에서 처리)
        }
        else
        {
            // 셔터가 없으면 기존 방식
            yield return new WaitForSeconds(2f);
            SceneManager.LoadScene("EndScene");
        }
    }

    void ForceFiniteSellAllStocks()
    {
        if (StockManager.Instance == null || PortfolioManager.Instance == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ StockManager 또는 PortfolioManager가 없어서 강제 매도 불가");
            return;
        }

        var holdings = StockManager.Instance.GetAllHoldings();

        if (holdings.Count == 0)
        {
            if (enableDebugLog)
                Debug.Log("📊 보유 주식이 없어서 강제 매도할 것이 없음");
            return;
        }

        if (enableDebugLog)
            Debug.Log($"🔄 게임 종료로 인한 보유 주식 {holdings.Count}종목 전량 강제 매도 시작");

        foreach (var holding in holdings.ToList()) // ToList()로 복사해서 순회 중 수정 방지
        {
            string stockId = holding.Key;
            int quantity = holding.Value;

            var stockData = StockManager.Instance.GetStockData(stockId);
            if (stockData == null) continue;

            // 현재가로 전량 매도
            float currentPrice = stockData.currentPrice;

            if (enableDebugLog)
            {
                Debug.Log($"💸 강제 매도: {stockData.displayName} {quantity}주 @ {currentPrice:N0}원");
            }

            // ✅ GameManager의 매도 기능 사용 (기존 시스템과 동일)
            bool success = GameManager.Instance.SellStockWithFee(stockId, quantity);

            if (success && enableDebugLog)
            {
                Debug.Log($"✅ {stockData.displayName} 강제 매도 완료");
            }
        }

        if (enableDebugLog)
        {
            var remainingHoldings = StockManager.Instance.GetAllHoldings();
            Debug.Log($"🏁 강제 매도 완료! 남은 보유 종목: {remainingHoldings.Count}개");
        }
    }
    /// <summary>
    /// ✅ 최종 게임 데이터 저장
    /// </summary>
    void SaveFinalGameData()
    {
        // 현금
        finalCash = UIManager.Instance?.GetCurrentCash() ?? initialCash;

        // 주식 가치 직접 계산
        finalStockValue = 0;
        if (StockManager.Instance != null)
        {
            var holdings = StockManager.Instance.GetAllHoldings();
            foreach (var holding in holdings)
            {
                var stock = StockManager.Instance.GetStockData(holding.Key);
                if (stock != null)
                {
                    finalStockValue += stock.currentPrice * holding.Value;
                }
            }
        }

        // 총자산
        finalTotalAsset = finalCash + finalStockValue;
        gameDataSaved = true;

        if (enableDebugLog)
        {
            Debug.Log($"💾 최종 데이터 저장:");
            Debug.Log($"  현금: {finalCash:N0}원");
            Debug.Log($"  주식가치: {finalStockValue:N0}원");
            Debug.Log($"  총자산: {finalTotalAsset:N0}원");
        }
    }
    #region 분산투자 보너스 시스템

    void TrackDiversificationProgress()
    {
        if (StockManager.Instance == null) return;

        var holdings = StockManager.Instance.GetAllHoldings();
        var uniqueSectors = new HashSet<StockSector>();

        foreach (var holding in holdings)
        {
            var stock = StockManager.Instance.GetStockData(holding.Key);
            if (stock != null)
            {
                uniqueSectors.Add(stock.sector);
            }
        }

        int currentSectorCount = uniqueSectors.Count;

        if (currentSectorCount > maxSectorsDiversified)
        {
            maxSectorsDiversified = currentSectorCount;

            if (enableDebugLog)
                Debug.Log($"🌟 새로운 분산투자 기록: {maxSectorsDiversified}개 섹터!");
        }

        float bonusRate = GetDiversificationBonusRate(currentSectorCount);
        sectorBonusHistory[currentTurn] = bonusRate;
    }

    public float GetDiversificationBonusRate(int sectorCount)
    {
        switch (sectorCount)
        {
            case 0:
            case 1:
                return -10f;
            case 2:
                return 5f;
            case 3:
                return 10f;
            case 4:
                return 15f;
            case 5:
            default:
                return 20f;
        }
    }

    int ApplyBestDiversificationBonus(int baseAsset)
    {
        float bestBonusRate = GetDiversificationBonusRate(maxSectorsDiversified);

        if (bestBonusRate == 0f) return baseAsset;

        int bonusAmount = Mathf.RoundToInt(baseAsset * (bestBonusRate / 100f));
        int finalAsset = baseAsset + bonusAmount;

        if (enableDebugLog)
        {
            Debug.Log($"🏆 최고 분산투자 보너스 적용:");
            Debug.Log($"  최대 분산도: {maxSectorsDiversified}개 섹터");
            Debug.Log($"  보너스율: {bestBonusRate:+0;-0}%");
            Debug.Log($"  보너스: {bonusAmount:N0}원");
            Debug.Log($"  최종 자산: {finalAsset:N0}원");
        }

        return finalAsset;
    }

    #endregion

    #region 매매 수수료 시스템 - 히스토리 기록 추가

    public int CalculateTradingFee(int tradeAmount)
    {
        return Mathf.RoundToInt(tradeAmount * (tradingFeeRate / 100f));
    }

    /// <summary>
    /// ✅ 수정된 수수료 포함 매수 처리 - GameHistoryManager 연동
    /// </summary>
    public bool BuyStockWithFee(string stockKey, int quantity)
    {
        var stock = StockManager.Instance?.GetStockData(stockKey);
        if (stock == null) return false;

        int stockCost = stock.currentPrice * quantity;
        int fee = CalculateTradingFee(stockCost);
        int totalCost = stockCost + fee;

        int currentCash = UIManager.Instance?.GetCurrentCash() ?? 0;

        if (currentCash < totalCost)
        {
            if (enableDebugLog)
                Debug.LogWarning($"⚠️ 자금 부족! 필요: {totalCost:N0}원 (수수료 {fee:N0}원 포함), 보유: {currentCash:N0}원");
            return false;
        }

        int beforeCash = currentCash;
        UIManager.Instance?.UpdateCash(currentCash - totalCost);

        bool success = StockManager.Instance.BuyStockWithoutCashDeduction(stockKey, quantity, stock.currentPrice);

        if (!success)
        {
            UIManager.Instance?.UpdateCash(beforeCash);
            if (enableDebugLog)
                Debug.LogError($"❌ 매수 실패: {stock.displayName} - 현금 복구됨");
            return false;
        }

        // 포트폴리오 매니저에 매수 기록
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnStockPurchased(stockKey, quantity, stock.currentPrice);
        }

        // ✅ GameHistoryManager에 매수 기록
        if (GameHistoryManager.Instance != null)
        {
            GameHistoryManager.Instance.OnStockPurchased(stockKey, quantity, stock.currentPrice, fee);
        }

        if (enableDebugLog)
        {
            int afterCash = UIManager.Instance?.GetCurrentCash() ?? 0;
            Debug.Log($"💰 수수료 포함 매수 완료: {stock.displayName} {quantity}주");
            Debug.Log($"📊 매수 전후 비교:");
            Debug.Log($"  현금: {beforeCash:N0}원 → {afterCash:N0}원 (차감: {totalCost:N0}원)");
            Debug.Log($"  주식비용: {stockCost:N0}원");
            Debug.Log($"  수수료: {fee:N0}원 ({tradingFeeRate}%)");
            Debug.Log($"  총 비용: {totalCost:N0}원");
        }

        return true;
    }

    /// <summary>
    /// ✅ 수정된 수수료 포함 매도 처리 - GameHistoryManager 연동
    /// </summary>
    public bool SellStockWithFee(string stockKey, int quantity)
    {
        var stock = StockManager.Instance?.GetStockData(stockKey);
        if (stock == null) return false;

        int sellAmount = stock.currentPrice * quantity;
        int fee = CalculateTradingFee(sellAmount);
        int netReceived = sellAmount - fee;

        int beforeCash = UIManager.Instance?.GetCurrentCash() ?? 0;

        bool success = StockManager.Instance.SellStockWithoutCashAddition(stockKey, quantity);
        if (!success)
        {
            if (enableDebugLog)
                Debug.LogError($"❌ 매도 실패: {stock.displayName}");
            return false;
        }

        UIManager.Instance?.UpdateCash(beforeCash + netReceived);

        // 포트폴리오 매니저에 매도 기록
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnStockSold(stockKey, quantity);
        }

        // ✅ GameHistoryManager에 매도 기록
        if (GameHistoryManager.Instance != null)
        {
            GameHistoryManager.Instance.OnStockSold(stockKey, quantity, stock.currentPrice, fee);
        }

        if (enableDebugLog)
        {
            int afterCash = UIManager.Instance?.GetCurrentCash() ?? 0;
            Debug.Log($"💰 수수료 포함 매도 완료: {stock.displayName} {quantity}주");
            Debug.Log($"📊 매도 전후 비교:");
            Debug.Log($"  현금: {beforeCash:N0}원 → {afterCash:N0}원 (증가: {netReceived:N0}원)");
            Debug.Log($"  매도금액: {sellAmount:N0}원");
            Debug.Log($"  수수료: {fee:N0}원 ({tradingFeeRate}%)");
            Debug.Log($"  실수령액: {netReceived:N0}원");
        }

        return true;
    }

    #endregion

    #region 최종 결과 계산

    /// <summary>
    /// 🔧 새로 추가: GameHistoryManager에서 현재 주식 가치 계산 (필요시)
    /// </summary>
    private float GetCurrentStockValueFromHistory()
    {
        if (GameHistoryManager.Instance == null) return 0f;

        var historyResult = GameHistoryManager.Instance.GenerateGameResult();
        if (historyResult?.turnHistory == null || historyResult.turnHistory.Count == 0) return 0f;

        // 마지막 턴의 주식 가치 반환
        var lastTurn = historyResult.turnHistory.LastOrDefault();
        return lastTurn?.stockValue ?? 0f;
    }
    public GameResult CalculateFinalResult()
    {
        int actualTotalAsset;

        if (gameDataSaved)
        {
            // 저장된 데이터 사용
            actualTotalAsset = finalTotalAsset;

            if (enableDebugLog)
                Debug.Log($"✅ 저장된 데이터 사용: {actualTotalAsset:N0}원");
        }
        else
        {
            // 실시간 계산 (게임 중)
            int cash = UIManager.Instance?.GetCurrentCash() ?? initialCash;
            int stockValue = 0;

            if (StockManager.Instance != null)
            {
                var holdings = StockManager.Instance.GetAllHoldings();
                foreach (var holding in holdings)
                {
                    var stock = StockManager.Instance.GetStockData(holding.Key);
                    if (stock != null)
                    {
                        stockValue += stock.currentPrice * holding.Value;
                    }
                }
            }

            actualTotalAsset = cash + stockValue;

            if (enableDebugLog)
                Debug.Log($"🔄 실시간 계산: {actualTotalAsset:N0}원");
        }

        // 분산투자 보너스 적용
        int finalAssetWithBonus = ApplyBestDiversificationBonus(actualTotalAsset);

        int profit = finalAssetWithBonus - initialCash;
        float profitRate = ((float)profit / initialCash) * 100f;

        var result = new GameResult
        {
            initialCash = initialCash,
            finalAsset = finalAssetWithBonus,
            totalProfit = profit,
            profitRate = profitRate,
            lifestyleGrade = DetermineLifestyleGrade(finalAssetWithBonus),
            totalTurns = maxTurns,
            taxPaid = 0,
            diversificationBonus = GetDiversificationBonusRate(maxSectorsDiversified),
            maxSectorsDiversified = maxSectorsDiversified
        };

        if (enableDebugLog)
        {
            Debug.Log($"📊 최종 결과:");
            Debug.Log($"  실제 총자산: {actualTotalAsset:N0}원");
            Debug.Log($"  분산투자 보너스: {result.diversificationBonus:+0;-0}%");
            Debug.Log($"  최종 자산: {result.finalAsset:N0}원");
            Debug.Log($"  수익률: {result.profitRate:F1}%");
        }

        return result;
    }

    LifestyleGrade DetermineLifestyleGrade(int finalAsset)
    {
        if (finalAsset >= upperGradeThreshold)
            return LifestyleGrade.Upper;
        else if (finalAsset >= middleUpperThreshold)
            return LifestyleGrade.MiddleUpper;
        else if (finalAsset >= middleThreshold)
            return LifestyleGrade.Middle;
        else
            return LifestyleGrade.Lower;
    }

    #endregion

    #region 게임 상태 관리

    void ChangeGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnGameStateChanged?.Invoke(newState);

        if (enableDebugLog)
            Debug.Log($"🎮 게임 상태 변경: {newState}");
    }

    [ContextMenu("게임 리셋")]
    public void ResetGame()
    {
        if (gameFlowCoroutine != null)
        {
            StopCoroutine(gameFlowCoroutine);
            gameFlowCoroutine = null;
        }

        isGameActive = false;
        currentTurn = 1;
        maxSectorsDiversified = 0;
        sectorBonusHistory.Clear();

        // ✅ 저장된 데이터 리셋
        finalCash = 0;
        finalStockValue = 0;
        finalTotalAsset = 0;
        gameDataSaved = false;

        StockManager.Instance?.ResetGame();
        PortfolioManager.Instance?.ResetPortfolio();
        if (GameHistoryManager.Instance != null)
            GameHistoryManager.Instance.ResetHistory();

        PrepareGame();

        if (enableDebugLog)
            Debug.Log("🔄 게임 리셋 완료");
    }


    #endregion

    #region 게임 제어 기능

    public void ForceEndCurrentTurn()
    {
        if (!isGameActive || IsGamePaused)
        {
            if (enableDebugLog)
            {
                if (!isGameActive)
                    Debug.LogWarning("⚠️ 게임이 진행 중이 아니어서 턴을 종료할 수 없습니다.");
                else if (IsGamePaused)
                    Debug.LogWarning("⚠️ 게임이 일시정지 중이어서 턴을 종료할 수 없습니다.");
            }
            return;
        }

        if (NewsTickerManager.Instance != null)
        {
            NewsTickerManager.Instance.ForceEndCurrentNews();
        }

        currentTurnRemainingTime = 0f;
        OnTurnTimerUpdate?.Invoke(currentTurnRemainingTime);

        if (enableDebugLog)
            Debug.Log("⏭️ GameManager: 현재 턴 강제 종료됨 - 다음 턴으로 진행");
    }

    public bool CanSkipCurrentTurn
    {
        get
        {
            return isGameActive &&
                   currentState == GameState.Playing &&
                   !IsGamePaused;
        }
    }

    public void PauseGame()
    {
        if (!isGameActive)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 게임이 진행 중이 아니어서 일시정지할 수 없습니다.");
            return;
        }

        Time.timeScale = 0f;

        if (enableDebugLog)
            Debug.Log("⏸️ GameManager: 게임 일시정지");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;

        if (enableDebugLog)
            Debug.Log("▶️ GameManager: 게임 재개");
    }

    public bool IsGamePaused => Mathf.Approximately(Time.timeScale, 0f);

    #endregion

    #region 외부 접근 프로퍼티

    public int CurrentTurn => currentTurn;
    public GameState CurrentState => currentState;
    public bool IsGameActive => isGameActive;
    public float TradingFeeRate => tradingFeeRate;
    public float CurrentTurnRemainingTime => currentTurnRemainingTime;
    public int MaxSectorsDiversified => maxSectorsDiversified;
    public int MaxTurns => maxTurns;
    public float TurnDuration => turnDuration;

    // ✅ 추가: 뉴스 티커용 메서드들
    public Dictionary<int, TurnEvent> GetScheduledEvents()
    {
        return new Dictionary<int, TurnEvent>(scheduledEvents);
    }

    public TurnEvent GetEventForTurn(int turn)
    {
        return scheduledEvents.ContainsKey(turn) ? scheduledEvents[turn] : null;
    }

    #endregion
    /// <summary>
    /// ✅ 언어 변경시 뉴스 티커 업데이트
    /// </summary>
    public void OnLanguageChanged()
    {
        // 현재 표시 중인 뉴스가 있다면 업데이트
        if (NewsTickerManager.Instance != null)
        {
            NewsTickerManager.Instance.RefreshCurrentLanguage();
        }

        if (enableDebugLog)
            Debug.Log("🌍 GameManager: 언어 변경으로 이벤트 텍스트 업데이트");
    }
    /// <summary>
    /// ✅ 로컬라이징된 이벤트 제목 가져오기
    /// </summary>
    public string GetLocalizedEventTitle(TurnEvent turnEvent)
    {
        if (CSVLocalizationManager.Instance == null || string.IsNullOrEmpty(turnEvent.titleKey))
            return turnEvent.eventKey ?? "Unknown Event";

        return CSVLocalizationManager.Instance.GetLocalizedText(turnEvent.titleKey);
    }

    /// <summary>
    /// ✅ 로컬라이징된 이벤트 설명 가져오기
    /// </summary>
    public string GetLocalizedEventDescription(TurnEvent turnEvent)
    {
        if (CSVLocalizationManager.Instance == null || string.IsNullOrEmpty(turnEvent.descriptionKey))
            return "No description available";

        return CSVLocalizationManager.Instance.GetLocalizedText(turnEvent.descriptionKey);
    }

    /// <summary>
    /// ✅ 로컬라이징된 뉴스 내용 가져오기
    /// </summary>
    public string GetLocalizedEventNews(TurnEvent turnEvent)
    {
        if (CSVLocalizationManager.Instance == null || string.IsNullOrEmpty(turnEvent.newsKey))
            return GetLocalizedEventTitle(turnEvent); // 폴백으로 제목 사용

        return CSVLocalizationManager.Instance.GetLocalizedText(turnEvent.newsKey);
    }

    /// <summary>
    /// ✅ 로컬라이징된 예고 뉴스 가져오기
    /// </summary>
    public string GetLocalizedPreviewNews(TurnEvent turnEvent)
    {
        if (CSVLocalizationManager.Instance == null || string.IsNullOrEmpty(turnEvent.previewKey))
            return CSVLocalizationManager.Instance?.GetLocalizedText("news_preview_default") ?? "Important announcement coming tomorrow";

        return CSVLocalizationManager.Instance.GetLocalizedText(turnEvent.previewKey);
    }

    /// <summary>
    /// ✅ 완전한 로컬라이징된 이벤트 정보 가져오기
    /// </summary>
    public LocalizedTurnEvent GetLocalizedTurnEvent(TurnEvent turnEvent)
    {
        return new LocalizedTurnEvent
        {
            eventKey = turnEvent.eventKey,
            title = GetLocalizedEventTitle(turnEvent),
            description = GetLocalizedEventDescription(turnEvent),
            newsContent = GetLocalizedEventNews(turnEvent),
            previewContent = GetLocalizedPreviewNews(turnEvent),
            titleKey = turnEvent.titleKey,
            descriptionKey = turnEvent.descriptionKey,
            newsKey = turnEvent.newsKey,
            previewKey = turnEvent.previewKey,
            effects = turnEvent.effects
        };
    }
    // ✅ OnDestroy()에서 이벤트 구독 해제 추가
    void OnDestroy()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= (language) => OnLanguageChanged();
        }
    }
    /// <summary>
    /// 개선된 이벤트 효과 적용 메서드 (GameManager.cs의 기존 메서드 대체)
    /// </summary>
   

}

/// <summary>
/// 게임 상태 열거형
/// </summary>
public enum GameState
{
    WaitingToStart,     // 시작 대기
    Ready,              // 준비 완료
    Playing,            // 게임 중
    Finished            // 게임 종료
}

/// <summary>
/// 턴 이벤트 데이터 구조
/// </summary>
[System.Serializable]
public class TurnEvent
{
    [Header("이벤트 식별")]
    public string eventKey;                     // ✅ 이벤트 고유 식별자 (예: "ai_innovation")

    [Header("로컬라이징 키들")]
    public string titleKey;                     // ✅ 제목 로컬라이징 키 (예: "event_title_ai_innovation")
    public string descriptionKey;               // ✅ 설명 로컬라이징 키 (예: "event_desc_ai_innovation")
    public string newsKey;                      // ✅ 뉴스 로컬라이징 키 (예: "news_event_ai_innovation")
    public string previewKey;                   // ✅ 예고 로컬라이징 키 (예: "news_preview_tech")

    [Header("이벤트 효과")]
    public List<StockEffect> effects;           // 주식에 미치는 영향

    [Header("레거시 필드 (하위 호환성)")]
    [System.Obsolete("Use titleKey instead")]
    public string title;                        // 기존 하드코딩된 제목 (점진적 제거)
    [System.Obsolete("Use descriptionKey instead")]
    public string description;                  // 기존 하드코딩된 설명 (점진적 제거)

    /// <summary>
    /// ✅ 레거시 코드 호환성을 위한 프로퍼티
    /// 기존 코드가 .title을 사용하는 경우 로컬라이징된 텍스트 반환
    /// </summary>
    public string Title
    {
        get
        {
            if (!string.IsNullOrEmpty(titleKey) && GameManager.Instance != null)
            {
                return GameManager.Instance.GetLocalizedEventTitle(this);
            }
            return title ?? titleKey ?? eventKey ?? "Unknown Event";
        }
    }

    /// <summary>
    /// ✅ 레거시 코드 호환성을 위한 프로퍼티
    /// 기존 코드가 .description을 사용하는 경우 로컬라이징된 텍스트 반환
    /// </summary>
    public string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(descriptionKey) && GameManager.Instance != null)
            {
                return GameManager.Instance.GetLocalizedEventDescription(this);
            }
            return description ?? descriptionKey ?? "No description available";
        }
    }

    /// <summary>
    /// ✅ 이벤트 타입 판별 (뉴스티커용)
    /// </summary>
    public EventCategory GetEventCategory()
    {
        if (string.IsNullOrEmpty(eventKey)) return EventCategory.General;

        return eventKey switch
        {
            "ai_innovation" => EventCategory.Technology,
            "energy_policy" => EventCategory.Energy,
            "interest_rate" => EventCategory.Interest,
            "crypto_regulation" => EventCategory.Crypto,
            _ => EventCategory.General
        };
    }
}
/// <summary>
/// ✅ 완전히 개선된 로컬라이징된 이벤트 정보를 담는 구조체
/// 실제 표시용 텍스트가 이미 로컬라이징되어 있음
/// </summary>
[System.Serializable]
public class LocalizedTurnEvent
{
    [Header("이벤트 식별")]
    public string eventKey;                     // 이벤트 고유 식별자

    [Header("로컬라이징된 텍스트들")]
    public string title;                        // 로컬라이징된 제목
    public string description;                  // 로컬라이징된 설명
    public string newsContent;                  // 로컬라이징된 뉴스 내용
    public string previewContent;               // 로컬라이징된 예고 내용

    [Header("원본 키들 (참조용)")]
    public string titleKey;                     // 원본 제목 키
    public string descriptionKey;               // 원본 설명 키
    public string newsKey;                      // 원본 뉴스 키
    public string previewKey;                   // 원본 예고 키

    [Header("이벤트 효과")]
    public List<StockEffect> effects;           // 주식에 미치는 영향

    /// <summary>
    /// TurnEvent에서 LocalizedTurnEvent로 변환하는 정적 메서드
    /// </summary>
    public static LocalizedTurnEvent FromTurnEvent(TurnEvent turnEvent)
    {
        if (GameManager.Instance == null)
        {
            return new LocalizedTurnEvent
            {
                eventKey = turnEvent.eventKey,
                title = turnEvent.titleKey ?? "Unknown Event",
                description = turnEvent.descriptionKey ?? "No description",
                newsContent = turnEvent.newsKey ?? "No news available",
                previewContent = turnEvent.previewKey ?? "No preview available",
                titleKey = turnEvent.titleKey,
                descriptionKey = turnEvent.descriptionKey,
                newsKey = turnEvent.newsKey,
                previewKey = turnEvent.previewKey,
                effects = turnEvent.effects
            };
        }

        return GameManager.Instance.GetLocalizedTurnEvent(turnEvent);
    }

    /// <summary>
    /// ✅ 이벤트 카테고리 가져오기
    /// </summary>
    public EventCategory GetEventCategory()
    {
        if (string.IsNullOrEmpty(eventKey)) return EventCategory.General;

        return eventKey switch
        {
            "ai_innovation" => EventCategory.Technology,
            "energy_policy" => EventCategory.Energy,
            "interest_rate" => EventCategory.Interest,
            "crypto_regulation" => EventCategory.Crypto,
            _ => EventCategory.General
        };
    }
}

/// <summary>
/// ✅ 이벤트 카테고리 열거형 (뉴스티커 및 UI에서 사용)
/// </summary>
public enum EventCategory
{
    General,        // 일반
    Technology,     // 기술/AI
    Energy,         // 에너지
    Interest,       // 금리
    Crypto,         // 가상자산
    Corporate       // 기업
}
/// <summary>
/// 주식 효과 데이터 구조
/// </summary>
[System.Serializable]
public class StockEffect
{
    [Header("기본 효과")]
    public StockSector sector;              // 영향받는 섹터
    public float changeRate;                // 기본 변동률 (%)
    public bool isGlobal = false;           // 전체 시장 영향 여부

    [Header("종목별 변동 설정")]
    public bool useIndividualVariation = false;    // 종목별 다른 변동 사용
    public float variationMin = -3f;               // 추가 변동 최소값 (%)
    public float variationMax = 3f;                // 추가 변동 최대값 (%)
}

/// <summary>
/// 게임 결과 데이터 구조
/// </summary>
[System.Serializable]
public class GameResult
{
    public int initialCash;                 // 초기 자금
    public int finalAsset;                  // 최종 자산 (보너스 포함)
    public int totalProfit;                 // 총 수익
    public float profitRate;                // 수익률 (%)
    public LifestyleGrade lifestyleGrade;   // 라이프스타일 등급
    public int totalTurns;                  // 총 턴 수
    public int taxPaid;                     // 납부한 세금
    public float diversificationBonus;     // 적용된 분산투자 보너스 (%)
    public int maxSectorsDiversified;      // 게임 중 최대 분산도
    public int totalTrades;                // 총 매매 횟수 (추가된 필드)
}

/// <summary>
/// 라이프스타일 등급
/// </summary>
public enum LifestyleGrade
{
    Lower,          // 하류층 (70만원 미만)
    Middle,         // 평범층 (70-130만원)
    MiddleUpper,    // 중상류층 (130-200만원)
    Upper           // 상류층 (200만원 이상)
}