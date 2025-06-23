using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 매거진 시스템 통합 관리자
/// 텍스트 생성 + 이미지 생성 + UI 바인딩 + 출력을 한 곳에서 관리
/// </summary>
public class MagazineManager : MonoBehaviour
{
    [Header("매거진 데이터")]
    [SerializeField] private MagazineContentData magazineContent; // Inspector에서 확인 가능
    [SerializeField] private MagazineImageGenerator imageGenerator; // ✅ 이미지 생성기

    [Header("생성 컴포넌트들")]
    public NewspaperContentGenerator contentGenerator;      // 텍스트 생성기
    public MagazineImageGenerator imageGenerator2;          // 이미지 생성기 (Inspector 할당용)

    [Header("UI 바인딩 - 헤더")]
    public TextMeshProUGUI magazineTitleText;              // "INVESTOR"
    public TextMeshProUGUI magazineSubtitleText;           // "AI Investment Analysis"
    public TextMeshProUGUI issueInfoText;                  // "ISSUE 01\n2024년 12월 25일"

    [Header("UI 바인딩 - 1면 투자 프로필")]
    public TextMeshProUGUI coverHeadlineText;              // "Investment Profile"
    public TextMeshProUGUI investmentStyleLabelText;       // "Investment Style"
    public TextMeshProUGUI investmentStyleText;            // "BALANCED GROWTH INVESTOR"  
    public TextMeshProUGUI profileDescriptionText;         // 투자 스타일 설명

    [Header("UI 바인딩 - 하단 분석")]
    public TextMeshProUGUI corePhilosophyTitleText;        // "Core Philosophy"
    public TextMeshProUGUI corePhilosophyText;             // 핵심 철학 내용
    public TextMeshProUGUI marketStrategyTitleText;        // "Market Strategy"
    public TextMeshProUGUI marketStrategyText;             // 시장 전략 내용
    public TextMeshProUGUI expertQuoteText;                // 전문가 인용구
    public TextMeshProUGUI expertSourceText;               // "AI Investment Research Institute"

    [Header("이미지 UI")]
    public Image lifestyleImageComponent;                  // AI 생성 라이프스타일 이미지
    public GameObject imageLoadingIndicator;               // 이미지 로딩 표시기

    [Header("매거진 상태 UI")]
    public GameObject magazinePanel;                       // 전체 매거진 패널
    public GameObject generationPanel;                     // 생성 중 패널
    public TextMeshProUGUI statusText;                     // 상태 메시지
    public Slider progressSlider;                          // 진행도 바

    [Header("액션 버튼들")]
    public Button generateButton;                          // 매거진 생성 버튼
    public Button regenerateButton;                        // 다시 생성 버튼
    public Button printButton;                             // 출력 버튼
    public Button saveButton;                              // 저장 버튼

    [Header("매거진 설정")]
    public bool autoGenerateOnStart = true;              // 시작시 자동 생성
    public bool showProgressIndicator = true;             // 진행도 표시

    [Header("디버그")]
    public bool enableDebugLog = true;
    public bool useTestData = false;                       // 테스트 데이터 사용

    // 현재 상태
    private bool isGenerating = false;
    private GameResult currentGameResult;
    private GenerationStep currentStep = GenerationStep.None;

    // 생성 단계
    private enum GenerationStep
    {
        None,
        GeneratingText,
        GeneratingImage,
        UpdatingUI,
        Complete
    }

    // 이벤트
    public System.Action<MagazineContentData> OnMagazineGenerated;
    public System.Action<bool> OnMagazineReady; // true = 성공, false = 실패

    #region Unity 생명주기

    void Start()
    {
        InitializeMagazineManager();

        // 자동으로 기존 게임 데이터로 매거진 생성
        if (autoGenerateOnStart)
        {
            StartCoroutine(AutoGenerateFromExistingData());
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (imageGenerator != null)
        {
            imageGenerator.OnImageGenerationComplete -= OnImageGenerationComplete;
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 매거진 매니저 초기화
    /// </summary>
    void InitializeMagazineManager()
    {
        // 매거진 데이터 초기화
        if (magazineContent == null)
        {
            magazineContent = new MagazineContentData();
        }
        magazineContent.Initialize();

        // 컴포넌트 검증
        ValidateComponents();

        // 버튼 이벤트 설정
        SetupButtonEvents();

        // ✅ 이미지 생성기 이벤트 구독
        if (imageGenerator != null)
        {
            imageGenerator.OnImageGenerationComplete += OnImageGenerationComplete;
        }

        // 초기 UI 상태 설정
        SetGenerationUIState(false);

        if (enableDebugLog)
            Debug.Log("✅ MagazineManager 초기화 완료");
    }

    /// <summary>
    /// 필수 컴포넌트 검증
    /// </summary>
    void ValidateComponents()
    {
        if (contentGenerator == null)
        {
            contentGenerator = GetComponent<NewspaperContentGenerator>();
            if (contentGenerator == null)
                Debug.LogWarning("⚠️ NewspaperContentGenerator가 없습니다!");
        }

        // ✅ 이미지 생성기 검증
        if (imageGenerator == null)
        {
            imageGenerator = imageGenerator2; // Inspector에서 할당된 것 사용
            if (imageGenerator == null)
                imageGenerator = GetComponent<MagazineImageGenerator>();
            if (imageGenerator == null)
                Debug.LogWarning("⚠️ MagazineImageGenerator가 없습니다!");
        }

        // 필수 UI 요소 체크
        if (investmentStyleText == null)
            Debug.LogWarning("⚠️ 핵심 UI 요소가 할당되지 않았습니다!");
    }

    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    void SetupButtonEvents()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(() => GenerateTestMagazine());

        if (regenerateButton != null)
            regenerateButton.onClick.AddListener(() => RegenerateMagazine());

        if (printButton != null)
            printButton.onClick.AddListener(() => PrintMagazine());

        if (saveButton != null)
            saveButton.onClick.AddListener(() => SaveMagazine());
    }

    #endregion

    #region 매거진 생성

    IEnumerator AutoGenerateFromExistingData()
    {
        yield return new WaitForSeconds(0.5f); // 매니저들 초기화 대기

        GameResult gameResult = null;

        // 1순위: GameManager에서 이미 계산된 최종 결과 가져오기
        if (GameManager.Instance != null)
        {
            gameResult = GameManager.Instance.CalculateFinalResult();

            if (enableDebugLog)
                Debug.Log($"✅ GameManager에서 최종 결과 가져옴: 수익률 {gameResult.profitRate:F1}%");
        }

        // 매거진 생성
        if (gameResult != null)
        {
            GenerateMagazine(gameResult);
        }
        else
        {
            Debug.LogError("❌ 매거진 생성 실패: GameResult가 null입니다!");
        }
    }

    /// <summary>
    /// 게임 결과로 매거진 생성 (메인 메서드)
    /// </summary>
    public void GenerateMagazine(GameResult gameResult)
    {
        if (isGenerating)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 매거진 생성이 이미 진행 중입니다.");
            return;
        }

        currentGameResult = gameResult;
        StartCoroutine(GenerateMagazineCoroutine());
    }

    /// <summary>
    /// 테스트용 매거진 생성
    /// </summary>
    [ContextMenu("테스트 매거진 생성")]
    public void GenerateTestMagazine()
    {
        var testResult = CreateTestGameResult();
        GenerateMagazine(testResult);
    }

    /// <summary>
    /// 랜덤 투자자 스타일로 매거진 생성
    /// </summary>
    [ContextMenu("랜덤 투자자 매거진 생성")]
    public void GenerateRandomInvestorMagazine()
    {
        var investorTypes = new System.Func<GameResult>[]
        {
            CreateBalancedGrowthInvestorResult,
            CreateActiveTraderResult,
            CreateFocusedInvestorResult,
            CreateGrowthInvestorResult,
            CreateCautiousInvestorResult,
            CreateSteadyInvestorResult
        };

        var randomType = investorTypes[UnityEngine.Random.Range(0, investorTypes.Length)];
        var testResult = randomType();

        if (enableDebugLog)
            Debug.Log($"🎲 랜덤 투자자 타입으로 매거진 생성: 수익률 {testResult.profitRate:F1}%");

        GenerateMagazine(testResult);
    }

    /// <summary>
    /// 모든 투자자 스타일 순차 테스트
    /// </summary>
    [ContextMenu("전체 투자자 스타일 테스트")]
    public void TestAllInvestorStyles()
    {
        StartCoroutine(TestAllInvestorStylesCoroutine());
    }

    /// <summary>
    /// 투자자 스타일별 순차 테스트 코루틴
    /// </summary>
    IEnumerator TestAllInvestorStylesCoroutine()
    {
        var testMethods = new System.Func<GameResult>[]
        {
            CreateBalancedGrowthInvestorResult,
            CreateActiveTraderResult,
            CreateFocusedInvestorResult,
            CreateGrowthInvestorResult,
            CreateCautiousInvestorResult,
            CreateSteadyInvestorResult,
            CreateFailedInvestorResult
        };

        var styleNames = new string[]
        {
            "밸런스형 (레이 달리오)",
            "액티브형 (조지 소로스)",
            "집중투자형 (워런 버핏)",
            "성장투자형 (피터 린치)",
            "신중투자형 (벤저민 그레이엄)",
            "안정투자형 (존 보글)",
            "투자 실패형 (학습 필요)"
        };

        for (int i = 0; i < testMethods.Length; i++)
        {
            if (enableDebugLog)
                Debug.Log($"🧪 테스트 {i + 1}/{testMethods.Length}: {styleNames[i]}");

            var testResult = testMethods[i]();
            GenerateMagazine(testResult);

            // 매거진 생성 완료 대기
            yield return new WaitUntil(() => !IsGenerating);
            yield return new WaitForSeconds(2f); // 결과 확인 시간

            if (enableDebugLog)
                Debug.Log($"✅ {styleNames[i]} 테스트 완료");
        }

        if (enableDebugLog)
            Debug.Log("🎉 모든 투자자 스타일 테스트 완료!");
    }

    /// <summary>
    /// 매거진 다시 생성 (이미지만)
    /// </summary>
    public void RegenerateMagazine()
    {
        if (currentGameResult == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 이전 게임 결과가 없어서 다시 생성할 수 없습니다.");
            return;
        }

        if (enableDebugLog)
            Debug.Log("🔄 매거진 다시 생성 (이미지만)");

        // ✅ 이미지만 다시 생성
        if (imageGenerator != null)
        {
            imageGenerator.GenerateImageFromGameResult(currentGameResult);
        }
    }

    /// <summary>
    /// 매거진 생성 코루틴 (전체 프로세스)
    /// </summary>
    IEnumerator GenerateMagazineCoroutine()
    {
        isGenerating = true;
        SetGenerationUIState(true);

        // 1단계: 텍스트 생성
        currentStep = GenerationStep.GeneratingText;
        UpdateStatusText("텍스트 콘텐츠 생성 중...");
        UpdateProgress(0.2f);

        yield return StartCoroutine(GenerateTextContent());

        // 2단계: 이미지 생성 시작
        currentStep = GenerationStep.GeneratingImage;
        UpdateStatusText("AI 라이프스타일 이미지 생성 중...");
        UpdateProgress(0.4f);

        StartImageGeneration();

        // 3단계: UI 업데이트
        currentStep = GenerationStep.UpdatingUI;
        UpdateStatusText("UI 업데이트 중...");
        UpdateProgress(0.8f);

        yield return StartCoroutine(UpdateMagazineUI());

        // 이미지 생성 완료 대기는 비동기로 처리
        // (OnImageGenerationComplete에서 최종 완료 처리)

        if (enableDebugLog)
            Debug.Log("📰 매거진 텍스트 생성 완료, 이미지 생성 대기 중...");
    }

    /// <summary>
    /// 텍스트 콘텐츠 생성
    /// </summary>
    IEnumerator GenerateTextContent()
    {
        if (contentGenerator == null)
        {
            Debug.LogError("❌ NewspaperContentGenerator가 없습니다!");
            yield break;
        }

        // 텍스트 생성 (동기 처리)
        magazineContent = contentGenerator.GenerateMagazineContent(currentGameResult);

        // 생성 시뮬레이션 (실제로는 즉시 완료)
        yield return new WaitForSeconds(0.5f);

        if (enableDebugLog)
            Debug.Log("✅ 텍스트 콘텐츠 생성 완료");
    }

    /// <summary>
    /// 이미지 생성 시작
    /// </summary>
    void StartImageGeneration()
    {
        // ✅ 이미지 생성기로 이미지 생성 시작
        if (imageGenerator == null)
        {
            Debug.LogWarning("⚠️ MagazineImageGenerator가 없어서 이미지 생성을 건너뜁니다.");
            OnImageGenerationComplete(false, "ImageGenerator not found");
            return;
        }

        // ✅ 이미지 생성 시작 (비동기)
        imageGenerator.GenerateImageFromGameResult(currentGameResult);
    }

    /// <summary>
    /// 이미지 생성 완료 콜백
    /// </summary>
    void OnImageGenerationComplete(bool success, string result)
    {
        if (success)
        {
            if (enableDebugLog)
                Debug.Log($"✅ 이미지 생성 완료: {result}");
        }
        else
        {
            if (enableDebugLog)
                Debug.LogWarning($"⚠️ 이미지 생성 실패: {result}");
        }

        // 최종 완료 처리
        StartCoroutine(CompleteMagazineGeneration(success));
    }

    /// <summary>
    /// 매거진 생성 최종 완료
    /// </summary>
    IEnumerator CompleteMagazineGeneration(bool imageSuccess)
    {
        currentStep = GenerationStep.Complete;
        UpdateStatusText("매거진 생성 완료!");
        UpdateProgress(1.0f);

        yield return new WaitForSeconds(0.5f);

        // UI 상태 복구
        SetGenerationUIState(false);
        isGenerating = false;

        // 이벤트 발생
        OnMagazineGenerated?.Invoke(magazineContent);
        OnMagazineReady?.Invoke(true);

        if (enableDebugLog)
        {
            Debug.Log("🎉 매거진 생성 완전 완료!");
            if (magazineContent != null)
                magazineContent.LogDebugInfo();
        }
    }

    #endregion

    #region UI 업데이트

    /// <summary>
    /// 매거진 UI 업데이트
    /// </summary>
    IEnumerator UpdateMagazineUI()
    {
        if (magazineContent == null || !magazineContent.IsValid())
        {
            Debug.LogError("❌ 유효하지 않은 매거진 데이터입니다!");
            yield break;
        }

        // UI 바인딩 - 헤더
        SafeSetText(magazineTitleText, magazineContent.magazineTitle);
        SafeSetText(magazineSubtitleText, magazineContent.magazineSubtitle);
        SafeSetText(issueInfoText, magazineContent.issueInfo);

        yield return new WaitForEndOfFrame();

        // UI 바인딩 - 1면 투자 프로필
        SafeSetText(coverHeadlineText, magazineContent.coverHeadline);
        SafeSetText(investmentStyleLabelText, magazineContent.investmentStyleLabel);
        SafeSetText(investmentStyleText, magazineContent.investmentStyle);

        SafeSetText(profileDescriptionText, magazineContent.profileDescription);

        yield return new WaitForEndOfFrame();

        // UI 바인딩 - 하단 분석
        SafeSetText(corePhilosophyTitleText, magazineContent.corePhilosophyTitle);
        SafeSetText(corePhilosophyText, magazineContent.corePhilosophy);
        SafeSetText(marketStrategyTitleText, magazineContent.marketStrategyTitle);
        SafeSetText(marketStrategyText, magazineContent.marketStrategy);
        SafeSetText(expertQuoteText, magazineContent.expertQuote);
        SafeSetText(expertSourceText, magazineContent.expertSource);

        if (enableDebugLog)
            Debug.Log("✅ 매거진 UI 업데이트 완료");
    }

    /// <summary>
    /// 안전한 텍스트 설정 (null 체크)
    /// </summary>
    void SafeSetText(TextMeshProUGUI textComponent, string content)
    {
        if (textComponent != null && !string.IsNullOrEmpty(content))
        {
            textComponent.text = content;
        }
    }

    /// <summary>
    /// 생성 UI 상태 설정
    /// </summary>
    void SetGenerationUIState(bool generating)
    {
        if (magazinePanel != null)
            magazinePanel.SetActive(!generating);

        if (generationPanel != null)
            generationPanel.SetActive(generating);

        // 버튼 상태
        if (generateButton != null)
            generateButton.interactable = !generating;

        if (regenerateButton != null)
            regenerateButton.interactable = !generating;

        if (printButton != null)
            printButton.interactable = !generating && (magazineContent?.IsValid() ?? false);
    }

    /// <summary>
    /// 상태 텍스트 업데이트
    /// </summary>
    void UpdateStatusText(string status)
    {
        if (statusText != null)
            statusText.text = status;

        if (showProgressIndicator && enableDebugLog)
            Debug.Log($"📊 매거진 생성 상태: {status}");
    }

    /// <summary>
    /// 진행도 업데이트
    /// </summary>
    void UpdateProgress(float progress)
    {
        if (progressSlider != null)
            progressSlider.value = progress;
    }

    #endregion

    #region 액션 메서드

    /// <summary>
    /// 매거진 출력
    /// </summary>
    public void PrintMagazine()
    {
        if (enableDebugLog)
            Debug.Log("🖨️ 매거진 출력 시작...");

        // TODO: PDF 생성 및 출력 시스템 연동
        StartCoroutine(PrintMagazineCoroutine());
    }

    /// <summary>
    /// 매거진 저장
    /// </summary>
    public void SaveMagazine()
    {
        if (enableDebugLog)
            Debug.Log("💾 매거진 저장 시작...");

        // TODO: 매거진 데이터 저장 시스템
        // JSON으로 MagazineContentData 저장 + 이미지 파일 백업
    }

    /// <summary>
    /// 출력 준비 상태 확인
    /// </summary>
    public bool IsReadyToPrint()
    {
        return !isGenerating &&
               magazineContent != null &&
               magazineContent.IsValid() &&
               (imageGenerator == null || !imageGenerator.IsGenerating);
    }

    /// <summary>
    /// 출력 프로세스 코루틴
    /// </summary>
    IEnumerator PrintMagazineCoroutine()
    {
        UpdateStatusText("PDF 생성 중...");
        yield return new WaitForSeconds(2f); // 출력 시뮬레이션

        UpdateStatusText("프린터로 전송 중...");
        yield return new WaitForSeconds(1f);

        if (enableDebugLog)
            Debug.Log("✅ 매거진 출력 완료!");

        UpdateStatusText("출력 완료!");
        yield return new WaitForSeconds(1f);

        UpdateStatusText("");
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 테스트용 게임 결과 생성 (밸런스형 - 레이 달리오 스타일)
    /// </summary>
    GameResult CreateTestGameResult()
    {
        return CreateBalancedGrowthInvestorResult();
    }

    /// <summary>
    /// 밸런스형 투자자 테스트 데이터 (레이 달리오 스타일)
    /// </summary>
    [ContextMenu("테스트: 밸런스형 투자자")]
    public GameResult CreateBalancedGrowthInvestorResult()
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
            diversificationBonus = 15.0f,         // 높은 분산투자 보너스
            maxSectorsDiversified = 4,            // 4개 섹터 분산
            totalTrades = 12                      // 적당한 거래 횟수
        };
    }

    /// <summary>
    /// 액티브 트레이더 테스트 데이터 (조지 소로스 스타일)
    /// </summary>
    [ContextMenu("테스트: 액티브 트레이더")]
    public GameResult CreateActiveTraderResult()
    {
        return new GameResult
        {
            initialCash = 1000000,
            finalAsset = 1450000,
            totalProfit = 450000,
            profitRate = 45.0f,
            lifestyleGrade = LifestyleGrade.Upper,
            totalTurns = 10,
            taxPaid = 5000,
            diversificationBonus = 10.0f,         // 중간 분산투자
            maxSectorsDiversified = 3,            // 3개 섹터
            totalTrades = 28                      // 많은 거래 횟수 (액티브)
        };
    }

    /// <summary>
    /// 집중투자형 테스트 데이터 (워런 버핏 스타일)
    /// </summary>
    [ContextMenu("테스트: 집중투자형")]
    public GameResult CreateFocusedInvestorResult()
    {
        return new GameResult
        {
            initialCash = 1000000,
            finalAsset = 1520000,
            totalProfit = 520000,
            profitRate = 52.0f,
            lifestyleGrade = LifestyleGrade.Upper,
            totalTurns = 10,
            taxPaid = 2000,
            diversificationBonus = 5.0f,          // 낮은 분산투자 (집중투자)
            maxSectorsDiversified = 2,            // 2개 섹터만
            totalTrades = 6                       // 적은 거래 횟수 (장기보유)
        };
    }

    /// <summary>
    /// 성장투자형 테스트 데이터 (피터 린치 스타일)
    /// </summary>
    [ContextMenu("테스트: 성장투자형")]
    public GameResult CreateGrowthInvestorResult()
    {
        return new GameResult
        {
            initialCash = 1000000,
            finalAsset = 1620000,
            totalProfit = 620000,
            profitRate = 62.0f,
            lifestyleGrade = LifestyleGrade.Upper,
            totalTurns = 10,
            taxPaid = 8000,
            diversificationBonus = 10.0f,         // 중간 분산투자
            maxSectorsDiversified = 3,            // 3개 섹터 (기술 중심)
            totalTrades = 18                      // 중간 거래 횟수
        };
    }

    /// <summary>
    /// 신중투자형 테스트 데이터 (벤저민 그레이엄 스타일)
    /// </summary>
    [ContextMenu("테스트: 신중투자형")]
    public GameResult CreateCautiousInvestorResult()
    {
        return new GameResult
        {
            initialCash = 1000000,
            finalAsset = 1180000,
            totalProfit = 180000,
            profitRate = 18.0f,
            lifestyleGrade = LifestyleGrade.Middle,
            totalTurns = 10,
            taxPaid = 1000,
            diversificationBonus = 20.0f,         // 최고 분산투자 (안전중시)
            maxSectorsDiversified = 5,            // 5개 섹터 전부
            totalTrades = 8                       // 적은 거래 (신중함)
        };
    }

    /// <summary>
    /// 안정투자형 테스트 데이터 (존 보글 스타일)
    /// </summary>
    [ContextMenu("테스트: 안정투자형")]
    public GameResult CreateSteadyInvestorResult()
    {
        return new GameResult
        {
            initialCash = 1000000,
            finalAsset = 1250000,
            totalProfit = 250000,
            profitRate = 25.0f,
            lifestyleGrade = LifestyleGrade.MiddleUpper,
            totalTurns = 10,
            taxPaid = 1500,
            diversificationBonus = 15.0f,         // 높은 분산투자
            maxSectorsDiversified = 4,            // 4개 섹터
            totalTrades = 10                      // 적당한 거래 (꾸준함)
        };
    }

    /// <summary>
    /// 투자 실패형 테스트 데이터 (학습 필요)
    /// </summary>
    [ContextMenu("테스트: 투자 실패형")]
    public GameResult CreateFailedInvestorResult()
    {
        return new GameResult
        {
            initialCash = 1000000,
            finalAsset = 850000,
            totalProfit = -150000,
            profitRate = -15.0f,
            lifestyleGrade = LifestyleGrade.Lower,
            totalTurns = 10,
            taxPaid = 0,
            diversificationBonus = 0.0f,          // 분산투자 안함
            maxSectorsDiversified = 1,            // 1개 섹터만
            totalTrades = 35                      // 너무 많은 거래 (수수료 손실)
        };
    }

    #endregion

    #region 다국어 지원

    /// <summary>
    /// CSV에서 다국어 텍스트 가져오기
    /// </summary>
    //private string GetLocalizedText(string key, string fallback = "")
    //{
    //    // LocalizationManager가 있다면 사용
    //    if (LocalizationManager.Instance != null)
    //    {
    //        return LocalizationManager.Instance.GetText(key);
    //    }

    //    // 없다면 fallback 사용
    //    return !string.IsNullOrEmpty(fallback) ? fallback : key;
    //}

    #endregion

    #region 공개 프로퍼티

    /// <summary>
    /// 현재 생성 중인지 확인
    /// </summary>
    public bool IsGenerating => isGenerating;

    /// <summary>
    /// 현재 매거진 데이터 (읽기 전용)
    /// </summary>
    public MagazineContentData CurrentMagazineContent => magazineContent;

    /// <summary>
    /// 현재 생성 단계
    /// </summary>
    public string CurrentStepName => currentStep.ToString();

    #endregion

    #region 디버그 메서드

    [ContextMenu("매거진 상태 출력")]
    void DebugPrintMagazineStatus()
    {
        Debug.Log("=== 매거진 매니저 상태 ===");
        Debug.Log($"생성 중: {isGenerating}");
        Debug.Log($"현재 단계: {currentStep}");
        Debug.Log($"데이터 유효: {magazineContent?.IsValid() ?? false}");

        if (magazineContent != null)
            magazineContent.LogDebugInfo();
    }

    [ContextMenu("UI 강제 업데이트")]
    void DebugForceUpdateUI()
    {
        if (magazineContent != null)
        {
            StartCoroutine(UpdateMagazineUI());
        }
    }

    #endregion
}