using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 결과 씬 전체 관리자 - 게임에서 결과 씬으로 전환시 초기화 담당
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    [Header("결과 매니저 참조")]
    public InvestmentResultManager resultManager;
    [Header("매거진 참조")]
    public MagazineManager magazineManager;          // 매거진 매니저 참조
    [Header("전환 효과")]
    public GameObject loadingPanel;             // 로딩 패널
    public float loadingDuration = 2f;          // 로딩 시간

    [Header("디버그")]
    public bool enableDebugLog = true;
    public bool useTestData = false;            // 테스트용 샘플 데이터 사용

    void Start()
    {
        StartCoroutine(InitializeResultScene());
    }

    /// <summary>
    /// 결과 씬 초기화
    /// </summary>
    IEnumerator InitializeResultScene()
    {
        // 로딩 패널 표시
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // 로컬라이징 매니저 대기
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        // 결과 매니저 대기
        while (resultManager == null)
        {
            resultManager = InvestmentResultManager.Instance;
            yield return null;
        }

        // ✅ 매거진 매니저 대기 추가
        while (magazineManager == null)
        {
            magazineManager = FindFirstObjectByType<MagazineManager>();
            yield return null;
        }

        // 로딩 시간 대기
        yield return new WaitForSeconds(loadingDuration);

        // 게임 결과 데이터 가져오기 및 설정
        yield return StartCoroutine(LoadGameResultData());

        // ✅ 백그라운드에서 매거진 이미지 생성 시작
        if (magazineManager != null && !useTestData)
        {
            GameResult gameResult = null;

            if (GameManager.Instance != null)
            {
                gameResult = GameManager.Instance.CalculateFinalResult();
            }

            if (gameResult != null)
            {
                magazineManager.GenerateMagazine(gameResult);

                if (enableDebugLog)
                    Debug.Log("🎨 백그라운드 매거진 이미지 생성 시작");
            }
        }

        // 로딩 패널 숨김
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (enableDebugLog)
            Debug.Log("✅ 결과 씬 초기화 완료 (이미지 생성 중)");
    }

    /// <summary>
    /// 게임 결과 데이터 로드
    /// </summary>
    IEnumerator LoadGameResultData()
    {
        GameResult gameResult = null;

        if (useTestData)
        {
            // 테스트용 샘플 데이터 생성
            gameResult = CreateTestGameResult();
        }
        else
        {
            // 실제 게임 데이터 가져오기
            if (GameManager.Instance != null)
            {
                // GameManager에서 최종 결과 계산 (분산투자 보너스 포함)
                gameResult = GameManager.Instance.CalculateFinalResult();
            }
            else
            {
                // GameManager가 없으면 현재 상태 기반으로 결과 생성
                gameResult = CreateResultFromCurrentState();
            }

            // 데이터가 없으면 테스트 데이터 사용
            if (gameResult == null)
            {
                Debug.LogWarning("⚠️ 게임 데이터를 찾을 수 없어 테스트 데이터를 사용합니다.");
                gameResult = CreateTestGameResult();
            }
        }

        // 결과 매니저에 데이터 설정
        if (resultManager != null && gameResult != null)
        {
            resultManager.SetGameResult(gameResult);
        }

        yield return null;
    }

    /// <summary>
    /// 현재 게임 상태를 기반으로 결과 생성 (GameHistoryManager 없이)
    /// </summary>
    GameResult CreateResultFromCurrentState()
    {
        int initialCash = 1000000; // 기본값
        int currentCash = UIManager.Instance?.GetCurrentCash() ?? initialCash;
        int totalAsset = UIManager.Instance?.GetTotalAsset() ?? initialCash;

        // 분산투자 정보 (GameManager가 있으면 가져오기)
        float diversificationBonus = 0f;
        int maxSectorsDiversified = 0;

        if (GameManager.Instance != null)
        {
            maxSectorsDiversified = GameManager.Instance.MaxSectorsDiversified;
            diversificationBonus = GameManager.Instance.GetDiversificationBonusRate(maxSectorsDiversified);
        }

        return new GameResult
        {
            initialCash = initialCash,
            finalAsset = totalAsset,
            totalProfit = totalAsset - initialCash,
            profitRate = ((float)(totalAsset - initialCash) / initialCash) * 100f,
            lifestyleGrade = DetermineLifestyleGrade(totalAsset),
            totalTurns = GameManager.Instance?.MaxTurns ?? 10,
            taxPaid = 0,
            diversificationBonus = diversificationBonus,
            maxSectorsDiversified = maxSectorsDiversified
        };
    }

    /// <summary>
    /// GameHistoryManager의 GameResult를 우리 GameResult로 변환
    /// </summary>
    GameResult ConvertHistoryResultToGameResult(GameHistoryManager.GameResult historyResult)
    {
        if (historyResult == null)
        {
            Debug.LogWarning("⚠️ GameHistoryManager 결과가 null입니다.");
            return CreateTestGameResult();
        }

        // GameHistoryManager.GameResult → 우리 GameResult 변환
        return new GameResult
        {
            initialCash = (int)historyResult.initialMoney,
            finalAsset = (int)historyResult.finalAssets,
            totalProfit = (int)historyResult.totalProfit,
            profitRate = historyResult.profitPercent,
            lifestyleGrade = DetermineLifestyleGrade((int)historyResult.finalAssets),
            totalTurns = GameManager.Instance?.MaxTurns ?? 10,
            taxPaid = 0, // GameHistoryManager에는 세금 개념이 없음
            diversificationBonus = 0f, // GameManager에서 계산해야 함
            maxSectorsDiversified = 0 // GameManager에서 계산해야 함
        };
    }

    /// <summary>
    /// 최종 자산에 따른 라이프스타일 등급 결정
    /// </summary>
    LifestyleGrade DetermineLifestyleGrade(int finalAsset)
    {
        // GameManager의 기준과 동일하게 적용
        if (finalAsset >= 1500000)      // 150만원 이상
            return LifestyleGrade.Upper;
        else if (finalAsset >= 1300000) // 130만원 이상
            return LifestyleGrade.MiddleUpper;
        else if (finalAsset >= 1000000) // 100만원 이상
            return LifestyleGrade.Middle;
        else
            return LifestyleGrade.Lower;  // 100만원 미만
    }

    /// <summary>
    /// 테스트용 게임 결과 생성
    /// </summary>
    GameResult CreateTestGameResult()
    {
        return new GameResult
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
    }

    /// <summary>
    /// 외부에서 호출 - 특정 결과로 씬 시작 (GameManager에서 사용)
    /// </summary>
    public static void LoadResultSceneWithData(GameResult result)
    {
        // PlayerPrefs나 정적 변수로 데이터 전달
        ResultDataCarrier.GameResult = result;
        SceneManager.LoadScene("ResultScene");
    }

    /// <summary>
    /// 메인 메뉴로 돌아가기
    /// </summary>
    public void GoToMainMenu()
    {
        // 데이터 정리
        ResultDataCarrier.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }

        SceneManager.LoadScene("StartScene");
    }

    /// <summary>
    /// 게임 다시하기
    /// </summary>
    public void RestartGame()
    {
        // 데이터 정리
        ResultDataCarrier.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }

        SceneManager.LoadScene("GameScene");
    }

    #region 디버그 메서드

    [ContextMenu("테스트 데이터로 다시 로드")]
    void DebugReloadWithTestData()
    {
        useTestData = true;
        StartCoroutine(LoadGameResultData());
    }

    [ContextMenu("현재 결과 데이터 출력")]
    void DebugPrintResultData()
    {
        if (resultManager != null)
        {
            Debug.Log("📊 현재 결과 데이터:");
            // resultManager의 디버그 메서드 호출
        }
        else
        {
            Debug.LogWarning("⚠️ ResultManager가 없습니다.");
        }
    }

    #endregion
}

/// <summary>
/// 씬 간 데이터 전달용 정적 클래스
/// </summary>
public static class ResultDataCarrier
{
    public static GameResult GameResult { get; set; }
    public static bool HasData => GameResult != null;

    public static void Clear()
    {
        GameResult = null;
    }
}