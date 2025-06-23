using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class StockManager : MonoBehaviour
{
    [Header("UI 참조 - 분리된 영역")]
    public Transform allStocksParent;       // AllStock 패널의 부모 (전체 종목 그리드)
    public Transform haveStocksParent;      // HaveStock_ScrollView/Viewport/Content (보유 종목)
    public GameObject stockItemPrefab;      // StockListItem_Prefab (카드형 - 전체 종목용)
    public GameObject haveStockItemPrefab;  // HaveStockItem_Prefab (보유 종목 전용)

    [Header("게임 상태")]
    public int currentTurn = 1;
    public int maxTurns = 10;

    [Header("디버그")]
    public bool enableDebugLog = true;

    // 전체 종목 데이터
    private List<StockData> allStocks = new List<StockData>();

    // UI 아이템들 (두 영역 분리)
    private List<StockItemUI> allStockUIItems = new List<StockItemUI>();           // 전체 종목 UI
    private List<HaveStockItemUI> haveStockUIItems = new List<HaveStockItemUI>();  // 보유 종목 UI

    // 보유 종목 정보 (종목키 → 보유량)
    private Dictionary<string, int> holdings = new Dictionary<string, int>();

    // 싱글톤 패턴
    private static StockManager instance;
    public static StockManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<StockManager>();
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

    // 로컬라이징 초기화 완료 후 게임 시작
    IEnumerator WaitForLocalizationAndInitialize()
    {
        // CSVLocalizationManager 초기화 완료까지 대기
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        if (enableDebugLog)
            Debug.Log("⏳ StockManager: 로컬라이징 초기화 완료, 게임 시작");

        InitializeStocks();
        CreateAllStockUI();
        UpdateAllUI();
    }

    // 15개 종목 초기화 (기획서 기준)
    void InitializeStocks()
    {
        allStocks.Clear();
        holdings.Clear();

        // 기술주 섹터 (5개)
        allStocks.Add(new StockData("SmartTech", "SmartTech", StockSector.TECH, 45000));
        allStocks.Add(new StockData("CloudKing", "CloudKing", StockSector.TECH, 36000));
        allStocks.Add(new StockData("SearchMaster", "SearchMaster", StockSector.TECH, 23600));
        allStocks.Add(new StockData("SocialVerse", "SocialVerse", StockSector.TECH, 26300));
        allStocks.Add(new StockData("StreamPlus", "StreamPlus", StockSector.TECH, 31200));

        // 반도체/AI 섹터 (3개)
        allStocks.Add(new StockData("NeoChips", "NeoChips", StockSector.SEM, 28500));
        allStocks.Add(new StockData("ChipFactory", "ChipFactory", StockSector.SEM, 19850));
        allStocks.Add(new StockData("RyzenTech", "RyzenTech", StockSector.SEM, 15200));

        // 전기차/에너지 섹터 (3개)
        allStocks.Add(new StockData("ThunderMotors", "ThunderMotors", StockSector.EV, 18200));
        allStocks.Add(new StockData("GreenCar", "GreenCar", StockSector.EV, 14750));
        allStocks.Add(new StockData("CleanEnergy", "CleanEnergy", StockSector.EV, 12600));

        // 가상자산 섹터 (2개)
        allStocks.Add(new StockData("DigitalGold", "DigitalGold", StockSector.CRYPTO, 52800));
        allStocks.Add(new StockData("SmartCoin", "SmartCoin", StockSector.CRYPTO, 8950));

        // 전통 대기업 섹터 (2개)
        allStocks.Add(new StockData("KoreaElec", "KoreaElec", StockSector.CORP, 67500));
        allStocks.Add(new StockData("MemoryKing", "MemoryKing", StockSector.CORP, 11400));

        // 언어 적용
        UpdateStockNames();

        // 초기 순위 계산
        CalculateRankings();

        if (enableDebugLog)
            Debug.Log($"📊 15개 종목 초기화 완료! 현재 턴: {currentTurn}");
    }

    // 전체 종목 UI 생성 (AllStock 패널)
    void CreateAllStockUI()
    {
        // 기존 UI 정리
        ClearExistingUI(allStocksParent);
        allStockUIItems.Clear();

        foreach (var stock in allStocks)
        {
            GameObject newItem = Instantiate(stockItemPrefab, allStocksParent);
            StockItemUI itemUI = newItem.GetComponent<StockItemUI>();

            if (itemUI != null)
            {
                // 보유량 정보와 함께 설정
                int holdingAmount = GetHoldingAmount(stock.stockKey);
                itemUI.SetStockData(stock, holdingAmount);

                // 매수 버튼 이벤트 연결
                itemUI.OnBuyButtonClicked += OnBuyStock;

                allStockUIItems.Add(itemUI);
            }
            else
            {
                Debug.LogError($"❌ {stockItemPrefab.name}에 StockItemUI 컴포넌트가 없습니다!");
            }
        }

        if (enableDebugLog)
            Debug.Log($"🎨 전체 종목 UI {allStockUIItems.Count}개 생성 완료!");
    }

    // 보유 종목 UI 생성/업데이트 (HaveStock 패널)
    void UpdateHaveStockUI()
    {
        // 기존 보유 종목 UI 정리
        ClearExistingUI(haveStocksParent);
        haveStockUIItems.Clear();

        // 보유 중인 종목만 필터링
        var ownedStocks = allStocks.Where(stock => GetHoldingAmount(stock.stockKey) > 0).ToList();

        foreach (var stock in ownedStocks)
        {
            GameObject newItem = Instantiate(haveStockItemPrefab, haveStocksParent);
            HaveStockItemUI itemUI = newItem.GetComponent<HaveStockItemUI>();

            if (itemUI != null)
            {
                int holdingAmount = GetHoldingAmount(stock.stockKey);

                // 평균 매수가 가져오기 (PortfolioManager에서)
                float avgPrice = 0f;
                if (PortfolioManager.Instance != null)
                {
                    avgPrice = PortfolioManager.Instance.GetAveragePurchasePrice(stock.stockKey);
                }

                if (enableDebugLog)
                {
                    Debug.Log($"📊 보유 종목 UI 생성: {stock.displayName}");
                    Debug.Log($"  - 보유량: {holdingAmount}주");
                    Debug.Log($"  - 평균매수가: {avgPrice:N0}원");
                    Debug.Log($"  - 현재가: {stock.currentPrice:N0}원");
                }

                itemUI.SetStockData(stock, holdingAmount, avgPrice);

                // 매도 버튼 이벤트 연결
                itemUI.OnSellButtonClicked += OnSellStock;

                haveStockUIItems.Add(itemUI);
            }
            else
            {
                Debug.LogError($"❌ {haveStockItemPrefab.name}에 HaveStockItemUI 컴포넌트가 없습니다!");
            }
        }

        if (enableDebugLog)
            Debug.Log($"📈 보유 종목 UI {haveStockUIItems.Count}개 업데이트 완료!");
    }

    // 기존 UI 아이템들 삭제
    void ClearExistingUI(Transform parent)
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

    // 매수 처리
    void OnBuyStock(StockData stockData)
    {
        if (enableDebugLog)
            Debug.Log($"💰 매수 요청: {stockData.displayName}");

        // GameManager가 있으면 수수료 포함 매수 사용
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            GameManager.Instance.BuyStockWithFee(stockData.stockKey, 1);
        }
        else
        {
            // 폴백: 기존 직접 매수
            BuyStock(stockData.stockKey, 1);
        }
    }

    // 매도 처리
    void OnSellStock(StockData stockData, int maxQuantity)
    {
        if (enableDebugLog)
            Debug.Log($"📉 매도 요청: {stockData.displayName} (최대 {maxQuantity}주)");

        // GameManager가 있으면 수수료 포함 매도 사용
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            GameManager.Instance.SellStockWithFee(stockData.stockKey, maxQuantity);
        }
        else
        {
            // 폴백: 기존 직접 매도
            SellStock(stockData.stockKey, maxQuantity);
        }
    }
    // StockManager.cs의 기존 코드에 추가할 메서드들

    /// <summary>
    /// 🔧 새로 추가: 현금 차감 없이 매수 처리 (GameManager용)
    /// 보유량만 증가시키고 현금은 GameManager에서 미리 처리
    /// </summary>
    public bool BuyStockWithoutCashDeduction(string stockKey, int quantity, int pricePerShare)
    {
        var stock = GetStockData(stockKey);
        if (stock == null)
        {
            Debug.LogError($"❌ 종목을 찾을 수 없습니다: {stockKey}");
            return false;
        }

        // 🔧 현금 차감 없이 보유량만 증가
        if (!holdings.ContainsKey(stockKey))
            holdings[stockKey] = 0;

        holdings[stockKey] += quantity;

        // 포트폴리오 매니저에 매수 기록 (GameManager에서 처리하므로 여기서는 제외)
        // PortfolioManager는 GameManager에서 호출

        if (enableDebugLog)
            Debug.Log($"✅ 보유량 증가: {stock.displayName} {quantity}주 (현금 차감 없음)");

        // UI 업데이트
        UpdateAllUI();

        return true;
    }

    /// <summary>
    /// 🔧 새로 추가: 현금 증가 없이 매도 처리 (GameManager용)
    /// 보유량만 감소시키고 현금은 GameManager에서 나중에 처리
    /// </summary>
    public bool SellStockWithoutCashAddition(string stockKey, int quantity)
    {
        if (!holdings.ContainsKey(stockKey) || holdings[stockKey] < quantity)
        {
            Debug.LogWarning($"⚠️ 보유량 부족! {stockKey}: {GetHoldingAmount(stockKey)}주");
            return false;
        }

        var stock = GetStockData(stockKey);
        if (stock == null) return false;

        // 🔧 현금 증가 없이 보유량만 감소
        holdings[stockKey] -= quantity;

        // 보유량이 0이 되면 딕셔너리에서 제거
        if (holdings[stockKey] <= 0)
            holdings.Remove(stockKey);

        // 포트폴리오 매니저에 매도 기록 (GameManager에서 처리하므로 여기서는 제외)
        // PortfolioManager는 GameManager에서 호출

        if (enableDebugLog)
            Debug.Log($"✅ 보유량 감소: {stock.displayName} {quantity}주 (현금 증가 없음)");

        // UI 업데이트
        UpdateAllUI();

        return true;
    }
    /// <summary>
    /// 매수 실행 (수정: 동기화 개선)
    /// </summary>
    public bool BuyStock(string stockKey, int quantity)
    {
        var stock = GetStockData(stockKey);
        if (stock == null)
        {
            Debug.LogError($"❌ 종목을 찾을 수 없습니다: {stockKey}");
            return false;
        }

        int totalCost = stock.currentPrice * quantity;
        int currentCash = UIManager.Instance?.GetCurrentCash() ?? 1000000;

        // 자금 부족 체크
        if (currentCash < totalCost)
        {
            Debug.LogWarning($"⚠️ 자금 부족! 필요: {totalCost:N0}원, 보유: {currentCash:N0}원");
            return false;
        }

        // 🔧 수정: 매수 전 상태 로깅 (디버그용)
        int beforeCash = currentCash;
        int beforeHoldings = GetHoldingAmount(stockKey);

        // 매수 처리
        if (!holdings.ContainsKey(stockKey))
            holdings[stockKey] = 0;

        holdings[stockKey] += quantity;

        // 현금 차감
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCash(currentCash - totalCost);
        }

        // 포트폴리오 매니저에 매수 기록
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnStockPurchased(stockKey, quantity, stock.currentPrice);
        }

        // 🔧 수정: 총자산 강제 재계산
        UpdateTotalAssetWithDelay();

        if (enableDebugLog)
        {
            Debug.Log($"✅ 매수 완료: {stock.displayName} {quantity}주 ({totalCost:N0}원)");
            Debug.Log($"📊 매수 전후 비교:");
            Debug.Log($"  현금: {beforeCash:N0}원 → {UIManager.Instance?.GetCurrentCash():N0}원");
            Debug.Log($"  보유량: {beforeHoldings}주 → {GetHoldingAmount(stockKey)}주");
        }

        // UI 업데이트
        UpdateAllUI();

        return true;
    }

    /// <summary>
    /// 매도 실행 (수정: 동기화 문제 해결)
    /// </summary>
    public bool SellStock(string stockKey, int quantity)
    {
        if (!holdings.ContainsKey(stockKey) || holdings[stockKey] < quantity)
        {
            Debug.LogWarning($"⚠️ 보유량 부족! {stockKey}: {GetHoldingAmount(stockKey)}주");
            return false;
        }

        var stock = GetStockData(stockKey);
        if (stock == null) return false;

        // 🔧 수정: 매도 전 상태 로깅 (디버그용)
        int beforeCash = UIManager.Instance?.GetCurrentCash() ?? 0;
        int beforeTotalAsset = UIManager.Instance?.GetTotalAsset() ?? 0;
        int beforeHoldings = holdings[stockKey];

        int totalReceived = stock.currentPrice * quantity;

        // 🔧 수정: 보유량 감소를 나중에 처리 (포트폴리오 매니저 먼저 호출)
        // PortfolioManager에 매도 기록 (보유량 변경 전에)
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnStockSold(stockKey, quantity);
        }

        // 이제 보유량 감소
        holdings[stockKey] -= quantity;

        // 보유량이 0이 되면 딕셔너리에서 제거
        if (holdings[stockKey] <= 0)
            holdings.Remove(stockKey);

        // 현금 추가
        if (UIManager.Instance != null)
        {
            int currentCash = UIManager.Instance.GetCurrentCash();
            UIManager.Instance.UpdateCash(currentCash + totalReceived);
        }

        // 🔧 수정: 총자산 강제 재계산 및 동기화
        UpdateTotalAssetWithDelay();

        if (enableDebugLog)
        {
            Debug.Log($"✅ 매도 완료: {stock.displayName} {quantity}주 ({totalReceived:N0}원)");
            Debug.Log($"📊 매도 전후 비교:");
            Debug.Log($"  현금: {beforeCash:N0}원 → {UIManager.Instance?.GetCurrentCash():N0}원");
            Debug.Log($"  보유량: {beforeHoldings}주 → {GetHoldingAmount(stockKey)}주");
            Debug.Log($"  총자산 (이전): {beforeTotalAsset:N0}원");
        }

        // UI 업데이트
        UpdateAllUI();

        return true;
    }

    /// <summary>
    /// 🔧 새로 추가: 지연된 총자산 계산 (동기화 보장)
    /// </summary>
    private void UpdateTotalAssetWithDelay()
    {
        // 코루틴으로 한 프레임 뒤에 총자산 재계산
        StartCoroutine(DelayedTotalAssetUpdate());
    }

    /// <summary>
    /// 🔧 새로 추가: 총자산 재계산 코루틴
    /// </summary>
    private IEnumerator DelayedTotalAssetUpdate()
    {
        yield return null; // 한 프레임 대기

        // 정확한 총자산 재계산
        int currentCash = UIManager.Instance?.GetCurrentCash() ?? 0;
        int stockValue = 0;

        // 현재 보유 종목들의 정확한 가치 계산
        foreach (var holding in holdings)
        {
            var stock = GetStockData(holding.Key);
            if (stock != null)
            {
                stockValue += stock.currentPrice * holding.Value;

                if (enableDebugLog)
                    Debug.Log($"  📈 {stock.displayName}: {holding.Value}주 × {stock.currentPrice:N0}원 = {stock.currentPrice * holding.Value:N0}원");
            }
        }

        int accurateTotalAsset = currentCash + stockValue;

        // UIManager에 정확한 총자산 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTotalAsset(accurateTotalAsset);
        }

        // 🔧 검증: 전량 매도시 현금과 총자산이 같은지 확인
        if (holdings.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"🔍 전량 매도 완료 검증:");
                Debug.Log($"  현금: {currentCash:N0}원");
                Debug.Log($"  총자산: {accurateTotalAsset:N0}원");
                Debug.Log($"  차이: {accurateTotalAsset - currentCash:N0}원 (0이어야 정상)");
            }

            if (accurateTotalAsset != currentCash)
            {
                Debug.LogWarning($"⚠️ 동기화 문제 발견! 차이: {accurateTotalAsset - currentCash:N0}원");

                // 강제 동기화
                UIManager.Instance.UpdateTotalAsset(currentCash);
                Debug.Log($"🔧 강제 동기화 적용: 총자산을 현금과 맞춤");
            }
        }

        if (enableDebugLog)
        {
            Debug.Log($"🔄 정확한 총자산 재계산 완료:");
            Debug.Log($"  현금: {currentCash:N0}원");
            Debug.Log($"  주식가치: {stockValue:N0}원");
            Debug.Log($"  총자산: {accurateTotalAsset:N0}원");
        }
    }

    // 모든 UI 업데이트
    public void UpdateAllUI()
    {
        // 전체 종목 UI 업데이트 (보유량 정보 포함)
        UpdateAllStockUI();

        // 보유 종목 UI 업데이트
        UpdateHaveStockUI();

        // 총 자산 계산 및 업데이트
        UpdateTotalAsset();

        if (enableDebugLog)
            Debug.Log($"🔄 모든 UI 업데이트 완료! (턴 {currentTurn})");
    }

    // 전체 종목 UI 업데이트 (보유량 반영)
    void UpdateAllStockUI()
    {
        for (int i = 0; i < allStockUIItems.Count && i < allStocks.Count; i++)
        {
            var stock = allStocks[i];
            int holdingAmount = GetHoldingAmount(stock.stockKey);

            allStockUIItems[i].SetStockData(stock, holdingAmount);
        }
    }

    /// <summary>
    /// 총자산 계산 및 업데이트 (수정: 더 정확한 계산)
    /// </summary>
    void UpdateTotalAsset()
    {
        int currentCash = UIManager.Instance?.GetCurrentCash() ?? 0;
        int stockValue = 0;

        if (enableDebugLog)
            Debug.Log($"📊 총자산 계산 시작:");

        // 보유 종목들의 현재 가치 계산 (더 상세한 로깅)
        foreach (var holding in holdings)
        {
            var stock = GetStockData(holding.Key);
            if (stock != null)
            {
                int itemValue = stock.currentPrice * holding.Value;
                stockValue += itemValue;

                if (enableDebugLog)
                    Debug.Log($"  📈 {stock.displayName}: {holding.Value}주 × {stock.currentPrice:N0}원 = {itemValue:N0}원");
            }
        }

        int totalAsset = currentCash + stockValue;

        if (enableDebugLog)
        {
            Debug.Log($"📊 총자산 계산 결과:");
            Debug.Log($"  현금: {currentCash:N0}원");
            Debug.Log($"  주식가치: {stockValue:N0}원");
            Debug.Log($"  총자산: {totalAsset:N0}원");
        }

        // UIManager에 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTotalAsset(totalAsset);
        }
    }

    // 보유량 조회
    public int GetHoldingAmount(string stockKey)
    {
        return holdings.ContainsKey(stockKey) ? holdings[stockKey] : 0;
    }

    // 보유 종목 목록 조회
    public Dictionary<string, int> GetAllHoldings()
    {
        return new Dictionary<string, int>(holdings);
    }

    // 순위 계산 (현재가 기준 내림차순)
    void CalculateRankings()
    {
        // 현재가 내림차순 정렬
        var sortedStocks = allStocks.OrderByDescending(s => s.currentPrice).ToList();

        for (int i = 0; i < sortedStocks.Count; i++)
        {
            sortedStocks[i].UpdateRank(i + 1);
        }

        if (enableDebugLog)
        {
            Debug.Log($"📈 순위 재계산 완료:");
            for (int i = 0; i < 3 && i < sortedStocks.Count; i++) // 상위 3개만 로그
            {
                var stock = sortedStocks[i];
                Debug.Log($"  {i + 1}위: {stock.displayName} - {stock.currentPrice:N0}원");
            }
        }
    }

    // 다음 턴으로 진행
    [ContextMenu("다음 턴")]
    public void NextTurn()
    {
        if (currentTurn >= maxTurns)
        {
            EndGame();
            return;
        }

        currentTurn++;

        if (enableDebugLog)
            Debug.Log($"🎮 턴 {currentTurn} 시작!");

        // 가격 변동 적용
        ApplyPriceChanges();

        // 순위 재계산
        CalculateRankings();

        // UI 업데이트
        UpdateAllUI();

        if (enableDebugLog)
            Debug.Log($"✅ 턴 {currentTurn} 완료!");
    }

    // 게임 종료
    void EndGame()
    {
        Debug.Log($"🏆 게임 종료! 최종 턴: {maxTurns}");

        // 최종 순위 출력
        var finalRanking = allStocks.OrderBy(s => s.currentRank).ToList();
        Debug.Log("🥇 최종 순위:");
        foreach (var stock in finalRanking)
        {
            Debug.Log($"  {stock.currentRank}위: {stock.displayName} - {stock.currentPrice:N0}원 ({stock.changeRate:+0.0;-0.0}%)");
        }

        // TODO: 결과 화면으로 전환
    }

    // 가격 변동 적용 (현재는 임시 랜덤)
    void ApplyPriceChanges()
    {
        foreach (var stock in allStocks)
        {
            // TODO: 나중에 하드코딩 패턴이나 이벤트 시스템으로 교체
            float randomChange = GetRandomChangeForStock(stock);
            stock.UpdatePrice(randomChange);

            if (enableDebugLog)
                Debug.Log($"  📊 {stock.displayName}: {randomChange:+0.0;-0.0}% → {stock.currentPrice:N0}원");
        }
    }

    // 종목별 랜덤 변동률 계산 (임시)
    float GetRandomChangeForStock(StockData stock)
    {
        // 섹터별 다른 변동성 적용
        float baseVolatility = 5f; // 기본 ±5%

        switch (stock.sector)
        {
            case StockSector.TECH:
                baseVolatility = 4f; // 기술주 ±4%
                break;
            case StockSector.SEM:
                baseVolatility = 6f; // 반도체 ±6%
                break;
            case StockSector.EV:
                baseVolatility = 7f; // 전기차 ±7%
                break;
            case StockSector.CRYPTO:
                baseVolatility = 10f; // 암호화폐 ±10%
                break;
            case StockSector.CORP:
                baseVolatility = 3f; // 대기업 ±3%
                break;
        }

        return Random.Range(-baseVolatility, baseVolatility);
    }

    // 언어 변경시 호출 (LocalizationManager에서)
    public void OnLanguageChanged()
    {
        UpdateStockNames();
        UpdateAllUI();

        if (enableDebugLog)
            Debug.Log("🌍 언어 변경으로 UI 업데이트 완료");
    }

    // 종목명 언어 업데이트
    void UpdateStockNames()
    {
        if (CSVLocalizationManager.Instance == null) return;

        foreach (var stock in allStocks)
        {
            string localizedName = CSVLocalizationManager.Instance.GetStockName(stock.stockKey);
            stock.UpdateDisplayName(localizedName);
        }
    }

    // 특정 종목 데이터 가져오기
    public StockData GetStockData(string stockKey)
    {
        return allStocks.FirstOrDefault(s => s.stockKey == stockKey);
    }

    // 모든 종목 데이터 가져오기
    public List<StockData> GetAllStocks()
    {
        return new List<StockData>(allStocks); // 복사본 반환
    }

    // 섹터별 종목 가져오기
    public List<StockData> GetStocksBySector(StockSector sector)
    {
        return allStocks.Where(s => s.sector == sector).ToList();
    }

    // 현재 게임 상태 정보
    public bool IsGameFinished => currentTurn >= maxTurns;
    public float GameProgress => (float)currentTurn / maxTurns;
    public int RemainingTurns => Mathf.Max(0, maxTurns - currentTurn);

    // 테스트용 메서드들
    [ContextMenu("가격 랜덤 변동")]
    void TestRandomChange()
    {
        ApplyPriceChanges();
        CalculateRankings();
        UpdateAllUI();
    }

    [ContextMenu("UI 강제 업데이트")]
    void TestUIUpdate()
    {
        UpdateAllUI();
    }

    [ContextMenu("게임 리셋")]
    /// <summary>
    /// 게임 리셋 (GameManager용)
    /// </summary>
    public void ResetGame()
    {
        currentTurn = 1;
        holdings.Clear();

        // 포트폴리오 매니저도 리셋
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.ResetPortfolio();
        }

        InitializeStocks();
        UpdateAllUI();

        if (enableDebugLog)
            Debug.Log("🔄 StockManager 리셋 완료");
    }

    [ContextMenu("테스트 매수 (SmartTech 2주)")]
    void TestBuy()
    {
        BuyStock("SmartTech", 2);
    }
    /// <summary>
    /// 전체 시장에 변동률 적용 (이벤트용)
    /// </summary>
    public void ApplyGlobalChange(float changeRate)
    {
        foreach (var stock in allStocks)
        {
            stock.UpdatePrice(changeRate);
        }

        CalculateRankings();

        if (enableDebugLog)
            Debug.Log($"🌍 전체 시장 변동: {changeRate:+0.0;-0.0}%");
    }

    /// <summary>
    /// 특정 섹터에 변동률 적용 (이벤트용)
    /// </summary>
    public void ApplySectorChange(StockSector sector, float changeRate)
    {
        var affectedStocks = allStocks.Where(s => s.sector == sector).ToList();

        foreach (var stock in affectedStocks)
        {
            stock.UpdatePrice(changeRate);
        }

        CalculateRankings();

        if (enableDebugLog)
            Debug.Log($"📊 {sector} 섹터 변동: {changeRate:+0.0;-0.0}% ({affectedStocks.Count}개 종목)");
    }

    /// <summary>
    /// 랜덤 가격 변동 적용 (기존 ApplyPriceChanges를 public으로)
    /// </summary>
    public void ApplyRandomPriceChanges()
    {
        ApplyPriceChanges(); // 기존 private 메서드 호출
    }

    /// <summary>
    /// 수수료 없는 기본 매수 (GameManager의 수수료 포함 매수를 위해)
    /// </summary>
    public bool BuyStockBasic(string stockKey, int quantity)
    {
        return BuyStock(stockKey, quantity); // 기존 BuyStock 메서드 사용
    }

    /// <summary>
    /// 수수료 없는 기본 매도 (GameManager의 수수료 포함 매도를 위해)
    /// </summary>
    public bool SellStockBasic(string stockKey, int quantity)
    {
        return SellStock(stockKey, quantity); // 기존 SellStock 메서드 사용
    }

}
