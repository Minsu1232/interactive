using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 게임 히스토리 수집 및 결과 분석 매니저
/// </summary>
public class GameHistoryManager : MonoBehaviour
{
    [System.Serializable]
    public class TurnSnapshot
    {
        public int turnNumber;
        public float totalAssets;
        public float cashAmount;
        public float stockValue;
        public List<StockHolding> holdings;
        public List<TransactionRecord> transactions;
        public List<EventRecord> events;
        public DateTime turnTime;

        public TurnSnapshot()
        {
            holdings = new List<StockHolding>();
            transactions = new List<TransactionRecord>();
            events = new List<EventRecord>();
            turnTime = DateTime.Now;
        }
    }

    [System.Serializable]
    public class StockHolding
    {
        public string stockKey;
        public string displayName;
        public StockSector sector;
        public int quantity;
        public float currentPrice;
        public float avgPurchasePrice;
        public float profit; // 현재 수익/손실
        public float profitPercent;
    }

    [System.Serializable]
    public class TransactionRecord
    {
        public TransactionType type;
        public int turnNumber; // 추가!
        public string stockKey;
        public string stockName;
        public StockSector sector;
        public int quantity;
        public float pricePerShare;
        public float totalAmount;
        public float fee;
        public DateTime timestamp;
    }

    [System.Serializable]
    public class EventRecord
    {
        public string eventName;
        public string eventDescription;
        public StockSector? affectedSector; // null이면 전체 시장
        public float impactPercent;
        public int turnNumber;
    }

    [System.Serializable]
    public class GameResult
    {
        public float finalAssets;
        public float initialMoney;
        public float totalProfit;
        public float profitPercent;
        public int totalTrades;
        public int profitableTrades;
        public float winRate;
        public float totalFees;
        public string investmentGrade;
        public List<string> achievements;
        public StockHolding bestInvestment;
        public StockHolding worstInvestment;
        public string investmentStyle;
        public List<TurnSnapshot> turnHistory;
        public List<TransactionRecord> allTransactions;
        public List<EventRecord> allEvents;
    }

    public enum TransactionType
    {
        Buy,
        Sell
    }

    [Header("설정")]
    [SerializeField] private float initialMoney = 1000000f;
    [SerializeField] private bool enableDetailedLogging = true;

    // 히스토리 데이터
    private List<TurnSnapshot> turnSnapshots = new List<TurnSnapshot>();
    private List<TransactionRecord> allTransactions = new List<TransactionRecord>();
    private List<EventRecord> allEvents = new List<EventRecord>();
    private TurnSnapshot currentTurnSnapshot;

    // 싱글톤
    private static GameHistoryManager instance;
    public static GameHistoryManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GameHistoryManager>();
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
        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            // 매수/매도 이벤트 구독 (GameManager에 이벤트 추가 필요)
            // GameManager.Instance.OnStockPurchased += OnStockPurchased;
            // GameManager.Instance.OnStockSold += OnStockSold;
        }

        // StockManager 이벤트 구독 (필요시 StockManager에 이벤트 추가)
        // StockManager.Instance.OnTurnChanged += OnTurnChanged;
    }

    #region 턴 관리

    /// <summary>
    /// 새 턴 시작 시 호출
    /// </summary>
    public void OnTurnStart(int turnNumber)
    {
        // 이전 턴 데이터 저장
        if (currentTurnSnapshot != null)
        {
            turnSnapshots.Add(currentTurnSnapshot);
        }

        // 새 턴 스냅샷 생성
        currentTurnSnapshot = new TurnSnapshot
        {
            turnNumber = turnNumber,
            totalAssets = GetCurrentTotalAssets(),
            cashAmount = GetCurrentCash(),
            stockValue = GetCurrentStockValue()
        };

        // 현재 보유 종목 스냅샷
        CaptureCurrentHoldings();

        if (enableDetailedLogging)
            Debug.Log($"📊 턴 {turnNumber} 히스토리 시작 - 총자산: {currentTurnSnapshot.totalAssets:N0}원");
    }

    /// <summary>
    /// 턴 종료 시 호출
    /// </summary>
    public void OnTurnEnd(int turnNumber)
    {
        if (currentTurnSnapshot == null) return;

        // 턴 종료시 최신 데이터로 업데이트
        currentTurnSnapshot.totalAssets = GetCurrentTotalAssets();
        currentTurnSnapshot.cashAmount = GetCurrentCash();
        currentTurnSnapshot.stockValue = GetCurrentStockValue();

        // 보유 종목 재캡처 (가격 변동 반영)
        CaptureCurrentHoldings();

        if (enableDetailedLogging)
            Debug.Log($"📊 턴 {turnNumber} 히스토리 완료 - 총자산: {currentTurnSnapshot.totalAssets:N0}원");
    }

    #endregion

    #region 거래 기록

    /// <summary>
    /// 매수 거래 기록 - GameManager에서 호출용 (수수료 포함)
    /// </summary>
    public void OnStockPurchased(string stockKey, int quantity, float pricePerShare, float fee = 0f)
    {
        var stockData = StockManager.Instance?.GetStockData(stockKey);
        if (stockData == null) return;

        // 🆕 현재 턴 번호 가져오기
        int currentTurn = GameManager.Instance?.CurrentTurn ?? 1;

        var transaction = new TransactionRecord
        {
            type = TransactionType.Buy,
            turnNumber = currentTurn, // 🆕 턴 정보 추가!
            stockKey = stockKey,
            stockName = stockData.displayName,
            sector = stockData.sector,
            quantity = quantity,
            pricePerShare = pricePerShare,
            totalAmount = pricePerShare * quantity,
            fee = fee,
            timestamp = DateTime.Now
        };

        allTransactions.Add(transaction);
        currentTurnSnapshot?.transactions.Add(transaction);

        if (enableDetailedLogging)
            Debug.Log($"💰 매수 기록: {stockData.displayName} {quantity}주 @ {pricePerShare:N0}원 (수수료: {fee:N0}원) [턴 {currentTurn}]");
    }
    /// <summary>
    /// 매도 거래 기록 - GameManager에서 호출용 (수수료 포함)
    /// </summary>
    public void OnStockSold(string stockKey, int quantity, float pricePerShare, float fee = 0f)
    {
        var stockData = StockManager.Instance?.GetStockData(stockKey);
        if (stockData == null) return;

        // 🆕 현재 턴 번호 가져오기
        int currentTurn = GameManager.Instance?.CurrentTurn ?? 1;

        var transaction = new TransactionRecord
        {
            type = TransactionType.Sell,
            turnNumber = currentTurn, // 🆕 턴 정보 추가!
            stockKey = stockKey,
            stockName = stockData.displayName,
            sector = stockData.sector,
            quantity = quantity,
            pricePerShare = pricePerShare,
            totalAmount = pricePerShare * quantity,
            fee = fee,
            timestamp = DateTime.Now
        };

        allTransactions.Add(transaction);
        currentTurnSnapshot?.transactions.Add(transaction);

        if (enableDetailedLogging)
            Debug.Log($"📈 매도 기록: {stockData.displayName} {quantity}주 @ {pricePerShare:N0}원 (수수료: {fee:N0}원) [턴 {currentTurn}]");
    }

    #endregion

    #region 이벤트 기록

    /// <summary>
    /// 게임 이벤트 기록
    /// </summary>
    public void OnEventOccurred(string eventName, string description, StockSector? affectedSector, float impactPercent, int turnNumber)
    {
        var eventRecord = new EventRecord
        {
            eventName = eventName,
            eventDescription = description,
            affectedSector = affectedSector,
            impactPercent = impactPercent,
            turnNumber = turnNumber
        };

        allEvents.Add(eventRecord);
        currentTurnSnapshot?.events.Add(eventRecord);

        if (enableDetailedLogging)
        {
            string sectorText = affectedSector.HasValue ? affectedSector.Value.ToString() : "전체 시장";
            Debug.Log($"📰 이벤트 기록: {eventName} - {sectorText} {impactPercent:+0.0;-0.0}%");
        }
    }

    #endregion

    #region 데이터 수집

    /// <summary>
    /// 현재 보유 종목 스냅샷 캡처
    /// </summary>
    private void CaptureCurrentHoldings()
    {
        if (currentTurnSnapshot == null || StockManager.Instance == null) return;

        currentTurnSnapshot.holdings.Clear();
        var holdings = StockManager.Instance.GetAllHoldings();

        foreach (var holding in holdings)
        {
            var stockData = StockManager.Instance.GetStockData(holding.Key);
            if (stockData == null) continue;

            float avgPrice = PortfolioManager.Instance?.GetAveragePurchasePrice(holding.Key) ?? stockData.currentPrice;
            float currentValue = stockData.currentPrice * holding.Value;
            float investedValue = avgPrice * holding.Value;
            float profit = currentValue - investedValue;
            float profitPercent = investedValue > 0 ? (profit / investedValue) * 100f : 0f;

            var stockHolding = new StockHolding
            {
                stockKey = holding.Key,
                displayName = stockData.displayName,
                sector = stockData.sector,
                quantity = holding.Value,
                currentPrice = stockData.currentPrice,
                avgPurchasePrice = avgPrice,
                profit = profit,
                profitPercent = profitPercent
            };

            currentTurnSnapshot.holdings.Add(stockHolding);
        }
    }

    /// <summary>
    /// 현재 총자산 계산
    /// </summary>
    private float GetCurrentTotalAssets()
    {
        if (UIManager.Instance != null)
            return UIManager.Instance.GetTotalAsset();

        // 폴백: 직접 계산
        return GetCurrentCash() + GetCurrentStockValue();
    }

    /// <summary>
    /// 현재 현금 조회
    /// </summary>
    private float GetCurrentCash()
    {
        return UIManager.Instance?.GetCurrentCash() ?? 0f;
    }

    /// <summary>
    /// 현재 주식 가치 계산
    /// </summary>
    private float GetCurrentStockValue()
    {
        if (StockManager.Instance == null) return 0f;

        float totalValue = 0f;
        var holdings = StockManager.Instance.GetAllHoldings();

        foreach (var holding in holdings)
        {
            var stockData = StockManager.Instance.GetStockData(holding.Key);
            if (stockData != null)
            {
                totalValue += stockData.currentPrice * holding.Value;
            }
        }

        return totalValue;
    }

    #endregion

    #region 결과 분석

    /// <summary>
    /// 최종 게임 결과 생성
    /// </summary>
    public GameResult GenerateGameResult()
    {
        // 마지막 턴 데이터 저장
        if (currentTurnSnapshot != null)
        {
            OnTurnEnd(currentTurnSnapshot.turnNumber);
            turnSnapshots.Add(currentTurnSnapshot);
        }

        GameResult result = new GameResult
        {
            finalAssets = GetCurrentTotalAssets(),
            initialMoney = initialMoney,
            turnHistory = new List<TurnSnapshot>(turnSnapshots),
            allTransactions = new List<TransactionRecord>(allTransactions),
            allEvents = new List<EventRecord>(allEvents)
        };

        // 기본 통계 계산
        result.totalProfit = result.finalAssets - result.initialMoney;
        result.profitPercent = (result.totalProfit / result.initialMoney) * 100f;
        result.totalTrades = allTransactions.Count;
        result.profitableTrades = CountProfitableTrades();
        result.winRate = result.totalTrades > 0 ? (result.profitableTrades / (float)result.totalTrades) * 100f : 0f;
        result.totalFees = allTransactions.Sum(t => t.fee);

        // 최고/최악 투자 분석
        AnalyzeBestWorstInvestments(result);

        // 투자 등급 계산
        result.investmentGrade = CalculateInvestmentGrade(result.profitPercent);

        // 성취 배지 계산
        result.achievements = CalculateAchievements(result);

        // 투자 스타일 분석
        result.investmentStyle = AnalyzeInvestmentStyle(result);

    
        return result;
    }

    /// <summary>
    /// 수익 거래 개수 계산
    /// </summary>
    private int CountProfitableTrades()
    {
        int profitableCount = 0;
        var sellTransactions = allTransactions.Where(t => t.type == TransactionType.Sell).ToList();

        foreach (var sell in sellTransactions)
        {
            // 해당 종목의 평균 매수가와 비교
            float avgBuyPrice = CalculateAverageBuyPrice(sell.stockKey, sell.timestamp);
            if (sell.pricePerShare > avgBuyPrice)
                profitableCount++;
        }

        return profitableCount;
    }

    /// <summary>
    /// 특정 종목의 평균 매수가 계산 (특정 시점까지)
    /// </summary>
    private float CalculateAverageBuyPrice(string stockKey, DateTime beforeTime)
    {
        var buyTransactions = allTransactions
            .Where(t => t.type == TransactionType.Buy && t.stockKey == stockKey && t.timestamp <= beforeTime)
            .ToList();

        if (buyTransactions.Count == 0) return 0f;

        float totalCost = buyTransactions.Sum(t => t.totalAmount);
        int totalQuantity = buyTransactions.Sum(t => t.quantity);

        return totalQuantity > 0 ? totalCost / totalQuantity : 0f;
    }

    /// <summary>
    /// 최고/최악 투자 분석
    /// </summary>
    private void AnalyzeBestWorstInvestments(GameResult result)
    {
        var finalHoldings = currentTurnSnapshot?.holdings ?? new List<StockHolding>();
        var soldStocks = AnalyzeSoldStocks();

        // 모든 투자 결과 합치기
        var allInvestmentResults = new List<StockHolding>();
        allInvestmentResults.AddRange(finalHoldings);
        allInvestmentResults.AddRange(soldStocks);

        if (allInvestmentResults.Count > 0)
        {
            result.bestInvestment = allInvestmentResults.OrderByDescending(h => h.profitPercent).First();
            result.worstInvestment = allInvestmentResults.OrderBy(h => h.profitPercent).First();
        }
    }

    /// <summary>
    /// 매도된 종목들의 수익 분석
    /// </summary>
    private List<StockHolding> AnalyzeSoldStocks()
    {
        var soldStocks = new List<StockHolding>();
        var stockKeys = allTransactions.Select(t => t.stockKey).Distinct();

        foreach (var stockKey in stockKeys)
        {
            var stockTransactions = allTransactions.Where(t => t.stockKey == stockKey).OrderBy(t => t.timestamp).ToList();

            int currentHolding = StockManager.Instance?.GetHoldingAmount(stockKey) ?? 0;
            if (currentHolding == 0) // 완전히 매도된 종목만
            {
                float totalBought = stockTransactions.Where(t => t.type == TransactionType.Buy).Sum(t => t.totalAmount);
                float totalSold = stockTransactions.Where(t => t.type == TransactionType.Sell).Sum(t => t.totalAmount);
                float totalFees = stockTransactions.Sum(t => t.fee);

                float profit = totalSold - totalBought - totalFees;
                float profitPercent = totalBought > 0 ? (profit / totalBought) * 100f : 0f;

                var stockData = StockManager.Instance?.GetStockData(stockKey);
                if (stockData != null)
                {
                    soldStocks.Add(new StockHolding
                    {
                        stockKey = stockKey,
                        displayName = stockData.displayName,
                        sector = stockData.sector,
                        quantity = 0,
                        currentPrice = stockData.currentPrice,
                        avgPurchasePrice = totalBought / stockTransactions.Where(t => t.type == TransactionType.Buy).Sum(t => t.quantity),
                        profit = profit,
                        profitPercent = profitPercent
                    });
                }
            }
        }

        return soldStocks;
    }

    /// <summary>
    /// 투자 등급 계산
    /// </summary>
    private string CalculateInvestmentGrade(float profitPercent)
    {
        if (profitPercent >= 80f) return "투자 천재";
        if (profitPercent >= 50f) return "투자 고수";
        if (profitPercent >= 20f) return "투자 달인";
        if (profitPercent >= 0f) return "투자 입문";
        return "투자 연습생";
    }

    /// <summary>
    /// 성취 배지 계산
    /// </summary>
    private List<string> CalculateAchievements(GameResult result)
    {
        var achievements = new List<string>();

        // 수익률 기반 성취
        if (result.profitPercent >= 100f) achievements.Add("👑 투자 킹");
        if (result.profitPercent >= 50f) achievements.Add("🎯 정확한 타이밍");

        // 승률 기반 성취
        if (result.winRate >= 80f) achievements.Add("⚡ 빠른 판단");
        if (result.winRate >= 60f) achievements.Add("🎲 운이 좋은 투자자");

        // 분산투자 성취
        var uniqueSectors = allTransactions.Select(t => t.sector).Distinct().Count();
        if (uniqueSectors >= 4) achievements.Add("🌈 분산투자 마스터");
        if (uniqueSectors >= 3) achievements.Add("📊 포트폴리오 관리자");

        // 거래량 기반 성취
        if (result.totalTrades >= 50) achievements.Add("🔥 액티브 트레이더");
        if (result.totalTrades <= 20) achievements.Add("🧘 참을성 있는 투자자");

        // 이벤트 활용 성취
        var eventTurns = allEvents.Select(e => e.turnNumber).Distinct().ToList();
        var eventRelatedTrades = allTransactions.Where(t =>
            eventTurns.Any(turn => Math.Abs((DateTime.Now.AddDays(-10 + turn) - t.timestamp).TotalDays) < 1)
        ).Count();

        if (eventRelatedTrades >= 10) achievements.Add("📰 뉴스 마스터");

        return achievements;
    }

    /// <summary>
    /// 투자 스타일 분석
    /// </summary>
    private string AnalyzeInvestmentStyle(GameResult result)
    {
        float avgHoldingPeriod = CalculateAverageHoldingPeriod();
        var favoriteSecror = GetFavoriteSector();
        bool isEventDriven = IsEventDrivenInvestor();

        if (isEventDriven) return "이벤트 드리븐 투자자";
        if (avgHoldingPeriod >= 5f) return "장기 투자자";
        if (result.totalTrades >= 40) return "데이 트레이더";
        if (favoriteSecror == StockSector.TECH) return "기술주 전문가";
        if (favoriteSecror == StockSector.CRYPTO) return "위험 선호 투자자";

        return "균형잡힌 투자자";
    }

    private float CalculateAverageHoldingPeriod()
    {
        // 매수-매도 쌍을 분석하여 평균 보유기간 계산
        // 간단히 총 턴수 / 총 거래수로 근사치 계산
        return turnSnapshots.Count > 0 && allTransactions.Count > 0 ?
            (float)turnSnapshots.Count / allTransactions.Count : 1f;
    }

    private StockSector GetFavoriteSector()
    {
        return allTransactions
            .GroupBy(t => t.sector)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? StockSector.TECH;
    }

    private bool IsEventDrivenInvestor()
    {
        // 이벤트 발생 턴 주변에서 거래가 많이 일어났는지 확인
        var eventTurns = allEvents.Select(e => e.turnNumber).ToHashSet();
        int eventRelatedTrades = 0;

        foreach (var transaction in allTransactions)
        {
            // 거래가 이벤트 턴 근처에서 발생했는지 확인 (임시 로직)
            // 실제로는 timestamp와 이벤트 시간을 비교해야 함
            if (eventTurns.Count > 0) eventRelatedTrades++;
        }

        return eventRelatedTrades > allTransactions.Count * 0.3f; // 30% 이상이 이벤트 관련
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 게임 리셋
    /// </summary>
    public void ResetHistory()
    {
        turnSnapshots.Clear();
        allTransactions.Clear();
        allEvents.Clear();
        currentTurnSnapshot = null;

        if (enableDetailedLogging)
            Debug.Log("🔄 게임 히스토리 리셋 완료");
    }

    /// <summary>
    /// 현재까지의 간단한 통계 조회
    /// </summary>
    public void LogCurrentStats()
    {
        if (!enableDetailedLogging) return;

        Debug.Log($"📊 현재 게임 통계:");
        Debug.Log($"  턴: {currentTurnSnapshot?.turnNumber ?? 0}");
        Debug.Log($"  총자산: {GetCurrentTotalAssets():N0}원");
        Debug.Log($"  총 거래: {allTransactions.Count}회");
        Debug.Log($"  이벤트: {allEvents.Count}개");
        Debug.Log($"  수익률: {((GetCurrentTotalAssets() - initialMoney) / initialMoney * 100f):F1}%");
    }

    #endregion

    #region 디버그 메서드

    [ContextMenu("테스트 매수 기록")]
    void TestBuyRecord()
    {
        OnStockPurchased("SmartTech", 5, 45000f, 1125f);
    }

    [ContextMenu("테스트 이벤트 기록")]
    void TestEventRecord()
    {
        OnEventOccurred("AI 기술 혁신", "AI 기술 발전으로 기술주 상승", StockSector.TECH, 15f, 3);
    }

    [ContextMenu("현재 통계 출력")]
    void TestLogStats()
    {
        LogCurrentStats();
    }

    #endregion
}