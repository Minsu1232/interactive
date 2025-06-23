using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// 포트폴리오 정보를 관리하고 UI에 표시하는 매니저
/// GameManager와 연동하여 분산투자 보너스를 별표로 표시
/// 평단가 계산 및 실시간 업데이트 시스템 포함
/// 수익률 계산 문제 해결 (전량 매도시 0% 문제 수정)
/// </summary>
public class PortfolioManager : MonoBehaviour
{
    [Header("포트폴리오 요약 UI")]
    public TextMeshProUGUI totalReturnRateText;     // 총 수익률
    public TextMeshProUGUI holdingCountText;        // 보유 종목 수
    public TextMeshProUGUI diversificationBonusText;// 분산투자 보너스 (별표 포함)
    public TextMeshProUGUI cashRatioText;           // 현금 비중

    [Header("추가 정보 UI (선택사항)")]
    public TextMeshProUGUI totalInvestmentText;     // 총 투자금액
    public TextMeshProUGUI totalStockValueText;     // 주식 평가액
    public TextMeshProUGUI totalProfitLossText;     // 총 손익
    public TextMeshProUGUI bestStockText;           // 최고 수익 종목
    public TextMeshProUGUI worstStockText;          // 최저 수익 종목

    [Header("라벨 UI (로컬라이징)")]
    public TextMeshProUGUI totalReturnLabel;        // "총 수익률" 라벨
    public TextMeshProUGUI holdingCountLabel;       // "보유 종목" 라벨
    public TextMeshProUGUI diversificationLabel;    // "분산투자" 라벨
    public TextMeshProUGUI cashRatioLabel;          // "현금 비중" 라벨

    [Header("게임 설정")]
    public int initialCash = 1000000;               // 초기 자금 100만원

    [Header("분산투자 보너스 설정 (폴백용)")]
    public float fiveSectorBonus = 20f;             // 5개 섹터 분산: +20%
    public float fourSectorBonus = 15f;             // 4개 섹터 분산: +15%
    public float threeSectorBonus = 10f;            // 3개 섹터 분산: +10%
    public float twoSectorBonus = 5f;               // 2개 섹터 분산: +5%
    public float oneSectorPenalty = -10f;           // 1개 섹터 올인: -10%

    [Header("색상 설정")]
    public Color profitColor = Color.red;           // 수익 색상 (빨강)
    public Color lossColor = Color.blue;            // 손실 색상 (파랑)
    public Color neutralColor = Color.gray;         // 중립 색상 (회색)
    public Color bonusColor = Color.green;          // 보너스 색상 (초록)
    public Color maxBonusColor = Color.yellow;      // 최고 보너스 색상 (금색)

    [Header("별표 UI 설정")]
    public bool enableStarAnimation = true;         // 별 달성 애니메이션 효과

    [Header("디버그")]
    public bool enableDebugLog = true;

    // 투자 기록 (매수가 기록용) - 평단가 계산의 핵심
    private Dictionary<string, List<PurchaseRecord>> purchaseHistory = new Dictionary<string, List<PurchaseRecord>>();
    private bool isInitialized = false;
    private int lastMaxSectors = 0; // 애니메이션용 이전 최대 섹터 수

    // 🔧 새로 추가: 실현 손익 추적
    private int totalRealizedProfit = 0;  // 매도를 통해 실현된 총 손익

    // HaveStockItemUI 참조 (평단가 업데이트용)
    private List<HaveStockItemUI> haveStockUIItems = new List<HaveStockItemUI>();

    // 싱글톤 패턴
    private static PortfolioManager instance;
    public static PortfolioManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<PortfolioManager>();
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
        // 로컬라이징 매니저 초기화 대기 후 시작
        StartCoroutine(WaitForLocalizationAndInitialize());
    }

    /// <summary>
    /// 로컬라이징 초기화 완료 후 포트폴리오 시작
    /// </summary>
    IEnumerator WaitForLocalizationAndInitialize()
    {
        // CSVLocalizationManager 초기화 완료까지 대기
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        if (enableDebugLog)
            Debug.Log("⏳ PortfolioManager: 로컬라이징 초기화 완료, 포트폴리오 시작");

        // 로컬라이징 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        // 초기 라벨 설정
        UpdateStaticLabels();

        // 초기 UI 업데이트
        UpdatePortfolioUI();

        isInitialized = true;

        if (enableDebugLog)
            Debug.Log("✅ PortfolioManager 초기화 완료");
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
    /// 언어 변경시 호출
    /// </summary>
    void OnLanguageChanged(Language newLanguage)
    {
        UpdateStaticLabels();
        UpdatePortfolioUI(); // 데이터도 다시 업데이트

        if (enableDebugLog)
            Debug.Log($"🌍 PortfolioManager 언어 변경: {newLanguage}");
    }

    /// <summary>
    /// 정적 라벨들 업데이트 (로컬라이징)
    /// </summary>
    void UpdateStaticLabels()
    {
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
            return;

        var locManager = CSVLocalizationManager.Instance;

        // 라벨들 로컬라이징
        if (totalReturnLabel != null)
            totalReturnLabel.text = locManager.GetLocalizedText("portfolio_total_return");

        if (holdingCountLabel != null)
            holdingCountLabel.text = locManager.GetLocalizedText("portfolio_holding_count");

        if (diversificationLabel != null)
            diversificationLabel.text = locManager.GetLocalizedText("portfolio_diversification");

        if (cashRatioLabel != null)
            cashRatioLabel.text = locManager.GetLocalizedText("portfolio_cash_ratio");

        if (enableDebugLog)
            Debug.Log("🏷️ 포트폴리오 라벨 로컬라이징 완료");
    }

    #region 투자 기록 관리 (평단가 계산 핵심)

    /// <summary>
    /// 매수 기록 추가 메서드
    /// 새로운 매수 시 평균 매수가를 자동으로 계산하여 업데이트
    /// </summary>
    /// <param name="stockKey">종목 키</param>
    /// <param name="quantity">매수 수량</param>
    /// <param name="pricePerShare">주당 매수가</param>
    public void RecordPurchase(string stockKey, int quantity, int pricePerShare)
    {
        if (!purchaseHistory.ContainsKey(stockKey))
        {
            purchaseHistory[stockKey] = new List<PurchaseRecord>();
        }

        purchaseHistory[stockKey].Add(new PurchaseRecord
        {
            quantity = quantity,
            pricePerShare = pricePerShare,
            timestamp = System.DateTime.Now
        });

        // ✅ 평균 매수가 계산 및 로그
        float newAvgPrice = CalculateAveragePurchasePrice(stockKey);

        if (enableDebugLog)
        {
            Debug.Log($"📝 매수 기록: {stockKey} {quantity}주 @{pricePerShare:N0}원");
            Debug.Log($"💰 새로운 평단가: {newAvgPrice:N0}원");
        }

        // ✅ HaveStockItemUI에 평단가 업데이트 알림
        UpdateHaveStockItemAveragePrice(stockKey, newAvgPrice);

        // UI 업데이트
        UpdatePortfolioUI();
    }

    /// <summary>
    /// 🔧 수정된 매도 기록 처리 메서드 (FIFO 방식)
    /// 매도 후 남은 수량의 평균 매수가를 재계산하고 실현 손익 기록
    /// </summary>
    /// <param name="stockKey">종목 키</param>
    /// <param name="quantity">매도 수량</param>
    public void RecordSale(string stockKey, int quantity)
    {
        if (!purchaseHistory.ContainsKey(stockKey)) return;

        // 🔧 먼저 실현 손익 기록
        RecordRealizedProfit(stockKey, quantity);

        var records = purchaseHistory[stockKey];
        int remainingToSell = quantity;

        // FIFO (First In, First Out) 방식으로 매도 처리
        for (int i = 0; i < records.Count && remainingToSell > 0; i++)
        {
            if (records[i].quantity <= remainingToSell)
            {
                remainingToSell -= records[i].quantity;
                records.RemoveAt(i);
                i--; // 인덱스 조정
            }
            else
            {
                records[i].quantity -= remainingToSell;
                remainingToSell = 0;
            }
        }

        // ✅ 매도 후 평균 매수가 재계산
        float newAvgPrice = 0f;
        if (records.Count > 0)
        {
            newAvgPrice = CalculateAveragePurchasePrice(stockKey);
        }

        // 모든 기록이 매도되면 키 제거
        if (records.Count == 0)
        {
            purchaseHistory.Remove(stockKey);
        }

        if (enableDebugLog)
        {
            Debug.Log($"📝 매도 기록: {stockKey} {quantity}주 처리 완료");
            if (newAvgPrice > 0)
                Debug.Log($"💰 매도 후 평단가: {newAvgPrice:N0}원");
            else
                Debug.Log($"💰 전량 매도 완료");
        }

        // ✅ HaveStockItemUI에 평단가 업데이트 알림
        UpdateHaveStockItemAveragePrice(stockKey, newAvgPrice);

        // UI 업데이트
        UpdatePortfolioUI();
    }

    /// <summary>
    /// 🔧 새로 추가: 매도시 실현 손익 기록
    /// FIFO 방식으로 매도되는 주식들의 손익을 정확히 계산
    /// </summary>
    void RecordRealizedProfit(string stockKey, int quantity)
    {
        if (!purchaseHistory.ContainsKey(stockKey)) return;

        var stock = StockManager.Instance?.GetStockData(stockKey);
        if (stock == null) return;

        // 실현 손익 계산 (FIFO 방식으로 매도되는 주식들의 손익)
        var records = purchaseHistory[stockKey];
        int remainingToSell = quantity;
        int realizedProfitForThisSale = 0;

        foreach (var record in records.ToList()) // ToList()로 복사본 생성
        {
            if (remainingToSell <= 0) break;

            int sellQuantityFromThisRecord = Mathf.Min(record.quantity, remainingToSell);
            int costBasis = record.pricePerShare * sellQuantityFromThisRecord;
            int saleProceeds = stock.currentPrice * sellQuantityFromThisRecord;

            realizedProfitForThisSale += (saleProceeds - costBasis);
            remainingToSell -= sellQuantityFromThisRecord;
        }

        totalRealizedProfit += realizedProfitForThisSale;

        if (enableDebugLog)
        {
            Debug.Log($"💰 실현 손익 기록: {stockKey}");
            Debug.Log($"  이번 매도 손익: {realizedProfitForThisSale:N0}원");
            Debug.Log($"  누적 실현 손익: {totalRealizedProfit:N0}원");
        }
    }

    /// <summary>
    /// ✅ HaveStockItemUI의 평단가를 업데이트하는 메서드 (새로 추가)
    /// 매수/매도 시 해당 종목의 UI에 새로운 평단가를 전달
    /// </summary>
    /// <param name="stockKey">종목 키</param>
    /// <param name="newAvgPrice">새로운 평균 매수가</param>
    private void UpdateHaveStockItemAveragePrice(string stockKey, float newAvgPrice)
    {
        // StockManager에서 HaveStockItemUI 리스트 가져오기
        if (StockManager.Instance != null)
        {
            // StockManager의 UpdateHaveStockUI 호출로 최신 평단가 반영
            StockManager.Instance.UpdateAllUI();
        }

        if (enableDebugLog)
            Debug.Log($"🔄 {stockKey} 평단가 UI 업데이트 요청: {newAvgPrice:N0}원");
    }

    /// <summary>
    /// 평균 매수가 계산 메서드 (가중평균 계산)
    /// 모든 매수 기록을 기반으로 가중평균 계산
    /// </summary>
    /// <param name="stockKey">종목 키</param>
    /// <returns>평균 매수가 (원)</returns>
    float CalculateAveragePurchasePrice(string stockKey)
    {
        if (!purchaseHistory.ContainsKey(stockKey)) return 0f;

        var records = purchaseHistory[stockKey];
        if (records.Count == 0) return 0f;

        int totalQuantity = 0;
        int totalValue = 0;

        foreach (var record in records)
        {
            totalQuantity += record.quantity;
            totalValue += record.quantity * record.pricePerShare;
        }

        return totalQuantity > 0 ? (float)totalValue / totalQuantity : 0f;
    }

    /// <summary>
    /// 외부에서 평균 매수가 조회하는 공개 메서드
    /// StockManager나 다른 시스템에서 사용
    /// </summary>
    /// <param name="stockKey">종목 키</param>
    /// <returns>평균 매수가 (원)</returns>
    public float GetAveragePurchasePrice(string stockKey)
    {
        return CalculateAveragePurchasePrice(stockKey);
    }

    #endregion

    #region 포트폴리오 UI 업데이트

    /// <summary>
    /// 포트폴리오 UI 전체 업데이트
    /// </summary>
    public void UpdatePortfolioUI()
    {
        if (!isInitialized || StockManager.Instance == null || UIManager.Instance == null)
            return;

        var holdings = StockManager.Instance.GetAllHoldings();
        int currentCash = UIManager.Instance.GetCurrentCash();
        int totalAsset = UIManager.Instance.GetTotalAsset();

        // 기본 정보 계산
        UpdateBasicInfo(holdings, currentCash, totalAsset);

        // 🌟 별표 분산투자 보너스 계산 (GameManager 연동)
        UpdateDiversificationBonusWithStars(holdings);

        // 수익률 계산 (🔧 수정된 방식)
        UpdateReturnRate(holdings, currentCash, totalAsset);

        // 추가 정보 업데이트 (UI가 있는 경우)
        UpdateDetailedInfo(holdings);

        if (enableDebugLog)
            Debug.Log("📊 포트폴리오 UI 업데이트 완료 (수익률 계산 수정 포함)");
    }

    /// <summary>
    /// 기본 정보 업데이트 (보유 종목 수, 현금 비중)
    /// </summary>
    void UpdateBasicInfo(Dictionary<string, int> holdings, int currentCash, int totalAsset)
    {
        // 보유 종목 수
        if (holdingCountText != null)
        {
            holdingCountText.text = $"{holdings.Count}";
        }

        // 현금 비중
        if (cashRatioText != null && totalAsset > 0)
        {
            float cashRatio = (float)currentCash / totalAsset * 100f;
            cashRatioText.text = $"{cashRatio:F0}%";
        }
    }

    #endregion

    #region 별표 분산투자 보너스 시스템

    /// <summary>
    /// 별표로 분산투자 보너스 표시 (메인 메서드)
    /// </summary>
    void UpdateDiversificationBonusWithStars(Dictionary<string, int> holdings)
    {
        if (diversificationBonusText == null) return;

        // GameManager가 있으면 별표 진행도로 표시
        if (GameManager.Instance != null)
        {
            UpdateDiversificationBonusFromGameManager(holdings);
            return;
        }

        // 폴백: GameManager가 없으면 기존 방식 (별표 포함)
        UpdateDiversificationBonusLegacy(holdings);
    }

    /// <summary>
    /// GameManager 기준 별표 분산투자 보너스 표시 - 수정된 버전
    /// </summary>
    void UpdateDiversificationBonusFromGameManager(Dictionary<string, int> holdings)
    {
        int maxSectors = GameManager.Instance.MaxSectorsDiversified;

        // 🌟 아직 투자한 적이 없으면 중립 표시
        if (maxSectors == 0)
        {
            diversificationBonusText.text = "☆☆☆☆☆";
            diversificationBonusText.color = neutralColor;

            if (enableDebugLog)
                Debug.Log("⭐ 분산투자: 아직 투자 없음");
            return;
        }

        float bonusRate = GetGameManagerBonusRate(maxSectors);
        string starProgress = CreateStarProgress(maxSectors);

        // 보너스 + 별표 표시
        diversificationBonusText.text = $"{bonusRate:+0;-0}% {starProgress}";
        diversificationBonusText.color = GetBonusColor(bonusRate);

        // 새로운 별 달성 애니메이션 체크
        if (enableStarAnimation && maxSectors > lastMaxSectors && lastMaxSectors > 0)
        {
            StartCoroutine(AnimateNewStarAchievement(lastMaxSectors, maxSectors));
        }
        lastMaxSectors = maxSectors;

        if (enableDebugLog)
            Debug.Log($"⭐ 분산투자 진행도: {starProgress} = {bonusRate:+0;-0}%");
    }

    /// <summary>
    /// 현재 보유 종목의 섹터 수 계산 (사용 안함 - 삭제 예정)
    /// </summary>
    int GetCurrentSectorCount(Dictionary<string, int> holdings)
    {
        var uniqueSectors = new HashSet<StockSector>();
        foreach (var holding in holdings)
        {
            var stock = StockManager.Instance.GetStockData(holding.Key);
            if (stock != null)
            {
                uniqueSectors.Add(stock.sector);
            }
        }
        return uniqueSectors.Count;
    }

    /// <summary>
    /// 별표 진행도 문자열 생성
    /// </summary>
    string CreateStarProgress(int achievedSectors)
    {
        string stars = "";

        for (int i = 1; i <= 5; i++)  // 최대 5개 섹터
        {
            if (i <= achievedSectors)
                stars += "★";  // 달성한 섹터: 채워진 별
            else
                stars += "☆";  // 미달성 섹터: 빈 별
        }

        return stars;
    }

    /// <summary>
    /// 새로운 별 달성 애니메이션 (코루틴)
    /// </summary>
    IEnumerator AnimateNewStarAchievement(int fromSectors, int toSectors)
    {
        for (int i = fromSectors + 1; i <= toSectors; i++)
        {
            float bonusRate = GetGameManagerBonusRate(i);
            string stars = CreateStarProgress(i);

            // 반짝이는 효과
            diversificationBonusText.text = $"{bonusRate:+0;-0}% {stars} ✨";
            diversificationBonusText.color = maxBonusColor;

            yield return new WaitForSeconds(0.5f);

            // 일반 표시로 복구
            diversificationBonusText.text = $"{bonusRate:+0;-0}% {stars}";
            diversificationBonusText.color = GetBonusColor(bonusRate);
        }

        if (enableDebugLog)
            Debug.Log($"🎉 새로운 분산투자 기록 달성! {CreateStarProgress(toSectors)}");
    }

    /// <summary>
    /// GameManager와 동일한 보너스율 계산
    /// </summary>
    float GetGameManagerBonusRate(int sectorCount)
    {
        switch (sectorCount)
        {
            case 0:
            case 1:
                return -10f; // ☆☆☆☆☆ 또는 ★☆☆☆☆ : -10%
            case 2:
                return 5f;   // ★★☆☆☆ : +5%
            case 3:
                return 10f;  // ★★★☆☆ : +10%
            case 4:
                return 15f;  // ★★★★☆ : +15%
            case 5:
            default:
                return 20f;  // ★★★★★ : +20%
        }
    }

    /// <summary>
    /// 보너스율에 따른 색상 결정
    /// </summary>
    Color GetBonusColor(float bonusRate)
    {
        if (bonusRate >= 20f)
            return maxBonusColor;      // 20%: 금색 (최고!)
        else if (bonusRate >= 15f)
            return Color.cyan;         // 15%: 청록색 (훌륭)
        else if (bonusRate > 0)
            return bonusColor;         // 양수: 초록 (좋음)
        else if (bonusRate < 0)
            return lossColor;          // 음수: 빨강 (위험)
        else
            return neutralColor;       // 0: 회색
    }

    /// <summary>
    /// 기존 방식 분산투자 보너스 (폴백용) - 수정된 버전
    /// </summary>
    void UpdateDiversificationBonusLegacy(Dictionary<string, int> holdings)
    {
        // 보유 종목들의 섹터 종류 계산
        var uniqueSectors = new HashSet<StockSector>();

        foreach (var holding in holdings)
        {
            var stock = StockManager.Instance.GetStockData(holding.Key);
            if (stock != null)
            {
                uniqueSectors.Add(stock.sector);
            }
        }

        int sectorCount = uniqueSectors.Count;

        // 🌟 투자한 적이 없으면 중립 표시
        if (sectorCount == 0)
        {
            diversificationBonusText.text = "☆☆☆☆☆";
            diversificationBonusText.color = neutralColor;
            return;
        }

        float bonusRate = GetLegacyBonusRate(sectorCount);
        string stars = CreateStarProgress(sectorCount);

        // UI 업데이트 (별표 포함)
        diversificationBonusText.text = $"{bonusRate:+0;-0}% {stars}";
        diversificationBonusText.color = GetBonusColor(bonusRate);

        if (enableDebugLog)
            Debug.Log($"📈 분산투자 보너스 (Legacy): {stars} = {bonusRate:+0;-0}%");
    }

    /// <summary>
    /// 기존 보너스율 계산 (폴백용)
    /// </summary>
    float GetLegacyBonusRate(int sectorCount)
    {
        switch (sectorCount)
        {
            case 0:
            case 1:
                return oneSectorPenalty;
            case 2:
                return twoSectorBonus;
            case 3:
                return threeSectorBonus;
            case 4:
                return fourSectorBonus;
            case 5:
            default:
                return fiveSectorBonus;
        }
    }

    #endregion

    #region 수익률 계산 (🔧 수정된 부분)

    /// <summary>
    /// 🔧 수정된 수익률 계산 및 업데이트 메서드
    /// 전량 매도시 0% 문제 해결
    /// </summary>
    void UpdateReturnRate(Dictionary<string, int> holdings, int currentCash, int totalAsset)
    {
        if (totalReturnRateText == null) return;

        // 🔧 수정: 간단하고 명확한 수익률 계산 방식 사용
        float returnRate = CalculateAccurateReturnRate(holdings, currentCash);

        Color returnColor;
        if (returnRate > 0)
            returnColor = profitColor;     // 수익: 빨강
        else if (returnRate < 0)
            returnColor = lossColor;       // 손실: 파랑
        else
            returnColor = neutralColor;    // 보합: 회색

        // UI 업데이트
        totalReturnRateText.text = $"{returnRate:+0.0;-0.0}%";
        totalReturnRateText.color = returnColor;

        if (enableDebugLog)
        {
            Debug.Log($"💰 총 수익률 (수정된 계산): {returnRate:+0.0;-0.0}%");
        }
    }

    /// <summary>
    /// ✅ 수정된 정확한 수익률 계산 메서드
    /// 모든 주식을 팔았을 때도 정확한 수익률 계산
    /// </summary>
    float CalculateAccurateReturnRate(Dictionary<string, int> holdings, int currentCash)
    {
        // 🔧 수정: 현재 총자산 = 현금 + 주식가치
        int currentStockValue = 0;

        // 현재 보유 중인 주식의 가치 계산
        foreach (var holding in holdings)
        {
            var stock = StockManager.Instance.GetStockData(holding.Key);
            if (stock != null)
            {
                currentStockValue += stock.currentPrice * holding.Value;
            }
        }

        int currentTotalAsset = currentCash + currentStockValue;

        // 🔧 핵심 수정: 초기자금 대비 현재 총자산의 변화율로 계산
        if (initialCash <= 0) return 0f;

        float returnRate = ((float)(currentTotalAsset - initialCash) / initialCash) * 100f;

        if (enableDebugLog)
        {
            Debug.Log($"💰 수익률 계산 (수정된 방식):");
            Debug.Log($"  초기 자금: {initialCash:N0}원");
            Debug.Log($"  현재 현금: {currentCash:N0}원");
            Debug.Log($"  현재 주식가치: {currentStockValue:N0}원");
            Debug.Log($"  현재 총자산: {currentTotalAsset:N0}원");
            Debug.Log($"  수익: {currentTotalAsset - initialCash:N0}원");
            Debug.Log($"  수익률: {returnRate:F2}%");

            // 🔧 전량 매도 상황 감지 및 로깅
            if (holdings.Count == 0)
            {
                Debug.Log($"🔍 전량 매도 상황 감지:");
                Debug.Log($"  매도 후 현금: {currentCash:N0}원");
                Debug.Log($"  초기 대비 차이: {currentCash - initialCash:N0}원");
                Debug.Log($"  실현 수익률: {returnRate:F2}%");
            }
        }

        return returnRate;
    }

    /// <summary>
    /// 🔧 헬퍼 메서드: 현재 보유 주식의 총 투자액 계산
    /// </summary>
    float CalculateTotalInvestedAmount(Dictionary<string, int> holdings)
    {
        float totalInvested = 0f;

        foreach (var holding in holdings)
        {
            float avgPrice = GetAveragePurchasePrice(holding.Key);
            if (avgPrice > 0)
            {
                totalInvested += avgPrice * holding.Value;
            }
        }

        return totalInvested;
    }

    #endregion

    #region 상세 정보 업데이트

    /// <summary>
    /// 상세 정보 업데이트 - 평단가 기반으로 정확한 손익 계산
    /// </summary>
    void UpdateDetailedInfo(Dictionary<string, int> holdings)
    {
        // 투자한 종목이 없으면 상세 정보 초기 상태로
        if (holdings.Count == 0)
        {
            if (totalInvestmentText != null)
                totalInvestmentText.text = "투자 없음";
            if (totalStockValueText != null)
                totalStockValueText.text = "보유 없음";
            if (totalProfitLossText != null)
            {
                totalProfitLossText.text = "±0원";
                totalProfitLossText.color = neutralColor;
            }
            if (bestStockText != null)
                bestStockText.text = "-";
            if (worstStockText != null)
                worstStockText.text = "-";
            return;
        }

        int totalInvestment = 0;
        int currentStockValue = 0;
        string bestStock = "";
        string worstStock = "";
        float bestReturn = float.MinValue;
        float worstReturn = float.MaxValue;

        // ✅ 평단가 기반으로 각 종목별 수익률 계산
        foreach (var holding in holdings)
        {
            var stock = StockManager.Instance.GetStockData(holding.Key);
            if (stock == null) continue;

            // 현재 평가액
            int currentValue = stock.currentPrice * holding.Value;
            currentStockValue += currentValue;

            // 평균 매수가 기반 투자 금액
            float avgPurchasePrice = GetAveragePurchasePrice(holding.Key);
            if (avgPurchasePrice > 0)
            {
                int investedAmount = (int)(avgPurchasePrice * holding.Value);
                totalInvestment += investedAmount;

                // 개별 종목 수익률 (평단가 기준)
                float stockReturn = ((float)(currentValue - investedAmount) / investedAmount) * 100f;

                if (stockReturn > bestReturn)
                {
                    bestReturn = stockReturn;
                    bestStock = stock.displayName;
                }

                if (stockReturn < worstReturn)
                {
                    worstReturn = stockReturn;
                    worstStock = stock.displayName;
                }
            }
        }

        // UI 업데이트 (로컬라이징된 단위 사용)
        string currencyUnit = "원"; // 기본값
        if (CSVLocalizationManager.Instance != null)
        {
            currencyUnit = CSVLocalizationManager.Instance.GetLocalizedText("ui_currency_unit");
        }

        if (totalInvestmentText != null)
            totalInvestmentText.text = $"₩{totalInvestment:N0}{currencyUnit}";

        if (totalStockValueText != null)
            totalStockValueText.text = $"₩{currentStockValue:N0}{currencyUnit}";

        if (totalProfitLossText != null)
        {
            int profitLoss = currentStockValue - totalInvestment;
            totalProfitLossText.text = $"{profitLoss:+#,0;-#,0}{currencyUnit}";
            totalProfitLossText.color = profitLoss >= 0 ? profitColor : lossColor;
        }

        if (bestStockText != null && !string.IsNullOrEmpty(bestStock))
        {
            bestStockText.text = $"{bestStock} ({bestReturn:+0.0;-0.0}%)";
            bestStockText.color = profitColor;
        }

        if (worstStockText != null && !string.IsNullOrEmpty(worstStock))
        {
            worstStockText.text = $"{worstStock} ({worstReturn:+0.0;-0.0}%)";
            worstStockText.color = lossColor;
        }
    }

    #endregion

    #region 외부 호출 인터페이스

    /// <summary>
    /// 외부에서 호출 (StockManager 연동) - 매수 시 평단가 계산
    /// </summary>
    public void OnStockPurchased(string stockKey, int quantity, int pricePerShare)
    {
        RecordPurchase(stockKey, quantity, pricePerShare);
    }

    /// <summary>
    /// 🔧 수정된 외부 호출 메서드 (StockManager 연동) - 매도 시 평단가 재계산
    /// 실현 손익 추적 기능 추가
    /// </summary>
    public void OnStockSold(string stockKey, int quantity)
    {
        RecordSale(stockKey, quantity);
    }

    /// <summary>
    /// 🔧 수정된 게임 리셋시 UI도 초기 상태로 (평단가 기록 포함)
    /// 실현 손익도 함께 초기화
    /// </summary>
    public void ResetPortfolio()
    {
        purchaseHistory.Clear(); // ✅ 평단가 기록 초기화
        lastMaxSectors = 0;
        totalRealizedProfit = 0; // 🔧 실현 손익도 초기화

        // UI 초기화
        if (diversificationBonusText != null)
        {
            diversificationBonusText.text = "☆☆☆☆☆";
            diversificationBonusText.color = neutralColor;
        }

        if (totalReturnRateText != null)
        {
            totalReturnRateText.text = "0.0%";
            totalReturnRateText.color = neutralColor;
        }

        UpdatePortfolioUI();

        if (enableDebugLog)
            Debug.Log("🔄 포트폴리오 리셋 완료 (평단가 기록 및 실현 손익 초기화 포함)");
    }

    /// <summary>
    /// 강제 UI 업데이트 (외부 호출용)
    /// </summary>
    public void ForceUpdateUI()
    {
        if (isInitialized)
        {
            UpdateStaticLabels();
            UpdatePortfolioUI();
        }
    }

    #endregion

    #region 디버그 메서드들

    /// <summary>
    /// 포트폴리오 정보 출력 (평단가 포함)
    /// </summary>
    [ContextMenu("포트폴리오 정보 출력")]
    void PrintPortfolioInfo()
    {
        Debug.Log("📊 === 포트폴리오 상세 정보 (평단가 포함) ===");

        foreach (var history in purchaseHistory)
        {
            Debug.Log($"📈 {history.Key}:");
            foreach (var record in history.Value)
            {
                Debug.Log($"  - {record.quantity}주 @{record.pricePerShare:N0}원 ({record.timestamp})");
            }
            float avgPrice = CalculateAveragePurchasePrice(history.Key);
            Debug.Log($"  💰 평균 매수가: {avgPrice:N0}원");
        }

        // 분산투자 현황
        if (GameManager.Instance != null)
        {
            int maxSectors = GameManager.Instance.MaxSectorsDiversified;
            string stars = CreateStarProgress(maxSectors);
            Debug.Log($"⭐ 분산투자 현황: {stars} ({maxSectors}/5 섹터)");
        }

        // 🔧 실현 손익 정보
        Debug.Log($"💰 누적 실현 손익: {totalRealizedProfit:N0}원");
    }

    [ContextMenu("포트폴리오 UI 강제 업데이트")]
    void ForceUpdateUIContext()
    {
        ForceUpdateUI();
        Debug.Log("🔄 포트폴리오 UI 강제 업데이트 완료");
    }

    [ContextMenu("별표 애니메이션 테스트")]
    void TestStarAnimation()
    {
        if (enableStarAnimation)
        {
            StartCoroutine(AnimateNewStarAchievement(2, 4));
            Debug.Log("⭐ 별표 애니메이션 테스트 실행");
        }
    }

    [ContextMenu("평단가 테스트")]
    void TestAveragePrice()
    {
        // 테스트 매수 시뮬레이션
        RecordPurchase("SmartTech", 1, 45000);  // 1주 45,000원
        RecordPurchase("SmartTech", 2, 50000);  // 2주 50,000원 (평단가: 48,333원)

        float avgPrice = GetAveragePurchasePrice("SmartTech");
        Debug.Log($"🧪 평단가 테스트 결과: {avgPrice:N0}원 (예상: 48,333원)");
    }

    [ContextMenu("수익률 계산 테스트")]
    void TestReturnRateCalculation()
    {
        // 🔧 수익률 계산 테스트
        var testHoldings = new Dictionary<string, int>();
        int testCash = 1100000; // 110만원 (10만원 수익)

        float testReturnRate = CalculateAccurateReturnRate(testHoldings, testCash);
        Debug.Log($"🧪 전량 매도 수익률 테스트: {testReturnRate:F2}% (예상: +10.00%)");
    }

    #endregion
}

/// <summary>
/// 매수 기록 구조체 (평단가 계산용)
/// </summary>
[System.Serializable]
public class PurchaseRecord
{
    public int quantity;            // 매수량
    public int pricePerShare;       // 주당 매수가
    public System.DateTime timestamp; // 매수 시점
}