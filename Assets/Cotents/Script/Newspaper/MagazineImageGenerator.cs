using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;

/// <summary>
/// 매거진용 개인화 라이프스타일 이미지 생성 매니저
/// 백그라운드에서 실시간 생성하고, 실패시 폴백 이미지 사용
/// </summary>
public class MagazineImageGenerator : MonoBehaviour
{
    [Header("UI 참조")]
    public Image lifestyleImageComponent;        // 생성된 이미지를 표시할 UI Image
    public Button printButton;                   // 신문 인쇄 버튼 (생성 완료시 활성화)
    public GameObject loadingIndicator;          // 로딩 표시기 (선택사항)

    [Header("폴백 이미지 설정")]
    public Sprite fallbackImageUpper;      // 상류층 이미지
    public Sprite fallbackImageMiddleUpper; // 중상류층 이미지  
    public Sprite fallbackImageMiddle;     // 평범층 이미지
    public Sprite fallbackImageLower;      // 하류층 이미지

    [Header("생성된 이미지 저장 경로")]
    public string generatedImagePath = "Assets/Contents/Image/";  // 생성된 이미지 저장 폴더
    public float generationTimeout = 45f;       // 생성 타임아웃 (45초)
    public bool enableRealTimeGeneration = true; // 실시간 생성 활성화

    [Header("디버그")]
    public bool enableDebugLog = true;
    public bool useTestMode = false;            // 테스트용 (실제 생성 없이 폴백만)

    // 현재 상태
    private bool isGenerating = false;
    private bool imageReady = false;
    private GameResult currentGameResult;
    private Coroutine generationCoroutine;

    // 이벤트
    public System.Action<bool, string> OnImageGenerationComplete;

    void Start()
    {
        InitializeImageGenerator();
    }

    /// <summary>
    /// 이미지 생성기 초기화
    /// </summary>
    void InitializeImageGenerator()
    {
        printButton.GetComponentInChildren<TextMeshProUGUI>().text = CSVLocalizationManager.Instance.GetLocalizedText("result_button_generating_profile");
        // 신문 인쇄 버튼 비활성화
        if (printButton != null)
        {
            printButton.interactable = false;

        }

        // 로딩 인디케이터 숨김
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }

        if (enableDebugLog)
            Debug.Log("🎨 MagazineImageGenerator 초기화 완료");
    }

    /// <summary>
    /// 게임 결과 기반 이미지 생성 시작 (메인 메서드)
    /// </summary>
    public void GenerateImageFromGameResult(GameResult gameResult)
    {
        if (isGenerating)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 이미 이미지 생성이 진행 중입니다.");
            return;
        }

        currentGameResult = gameResult;

        if (useTestMode)
        {
            // 테스트 모드: 바로 폴백 이미지 사용
            StartCoroutine(LoadFallbackImageCoroutine());
        }
        else if (enableRealTimeGeneration && ComfyUIClient.Instance != null)
        {
            // 실시간 생성 시도
            generationCoroutine = StartCoroutine(GeneratePersonalizedImageCoroutine());
        }
        else
        {
            // ComfyUI 사용 불가시 폴백 이미지 사용
            StartCoroutine(LoadFallbackImageCoroutine());
        }

        if (enableDebugLog)
            Debug.Log($"🎨 라이프스타일 이미지 생성 시작: {gameResult.lifestyleGrade}");
    }

    /// <summary>
    /// 개인화 이미지 생성 코루틴
    /// </summary>
    IEnumerator GeneratePersonalizedImageCoroutine()
    {
        isGenerating = true;
        ShowLoadingState(true);

        // 개인화 프롬프트 생성
        string personalizedPrompt = CreatePersonalizedPrompt(currentGameResult);
        string fileName = $"magazine_lifestyle_{System.DateTime.Now:yyyyMMdd_HHmmss}";

        if (enableDebugLog)
            Debug.Log($"🎨 개인화 프롬프트: {personalizedPrompt}");

        // ComfyUI로 이미지 생성 시작
        bool generationStarted = false;
        string resultPath = "";

        yield return StartCoroutine(ComfyUIClient.Instance.GeneratePersonalizedLifestyle(
            personalizedPrompt,
            CreateUserStatsFromGameResult(currentGameResult),
            fileName,
            (success, path) => {
                generationStarted = true;
                if (success)
                {
                    resultPath = path;
                    if (enableDebugLog)
                        Debug.Log($"✅ 이미지 생성 성공: {path}");
                }
                else
                {
                    if (enableDebugLog)
                        Debug.LogWarning($"⚠️ 이미지 생성 실패: {path}");
                }
            }
        ));

        // 타임아웃 대기
        float elapsedTime = 0f;
        while (!generationStarted && elapsedTime < generationTimeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }

        if (generationStarted && !string.IsNullOrEmpty(resultPath))
        {
            // 생성 성공: 이미지 로드
            yield return StartCoroutine(LoadGeneratedImageCoroutine(resultPath));
        }
        else
        {
            // 생성 실패 또는 타임아웃: 폴백 이미지 사용
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 이미지 생성 실패 또는 타임아웃 - 폴백 이미지 사용");

            yield return StartCoroutine(LoadFallbackImageCoroutine());
        }

        isGenerating = false;
        ShowLoadingState(false);
    }

    /// <summary>
    /// 생성된 이미지 로드
    /// </summary>
    IEnumerator LoadGeneratedImageCoroutine(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            if (enableDebugLog)
                Debug.LogError($"❌ 생성된 이미지 파일을 찾을 수 없음: {imagePath}");

            yield return StartCoroutine(LoadFallbackImageCoroutine());
            yield break;
        }

        // 파일에서 텍스처 로드
        byte[] imageData = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);

        if (texture.LoadImage(imageData))
        {
            Sprite imageSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);

            if (lifestyleImageComponent != null)
            {
                lifestyleImageComponent.sprite = imageSprite;
            }

            OnImageGenerationComplete?.Invoke(true, "개인화 이미지 생성 완료");
            SetImageReady(true);

            if (enableDebugLog)
                Debug.Log("✅ 개인화 이미지 로드 완료");
        }
        else
        {
            if (enableDebugLog)
                Debug.LogError("❌ 이미지 데이터 로드 실패");

            yield return StartCoroutine(LoadFallbackImageCoroutine());
        }
    }

    /// <summary>
    /// 폴백 이미지 로드 (Inspector에서 할당된 스프라이트 사용)
    /// </summary>
    IEnumerator LoadFallbackImageCoroutine()
    {
        Sprite fallbackSprite = GetFallbackSprite(currentGameResult.lifestyleGrade);

        if (fallbackSprite == null)
        {
            if (enableDebugLog)
                Debug.LogError($"❌ {currentGameResult.lifestyleGrade}에 해당하는 폴백 이미지가 할당되지 않았습니다!");

            OnImageGenerationComplete?.Invoke(false, "폴백 이미지 없음");
            yield break;
        }

        if (enableDebugLog)
            Debug.Log($"🔄 폴백 이미지 적용: {fallbackSprite.name} ({currentGameResult.lifestyleGrade})");

        // 폴백 이미지 적용
        if (lifestyleImageComponent != null)
        {
            lifestyleImageComponent.sprite = fallbackSprite;
        }

        OnImageGenerationComplete?.Invoke(true, "폴백 이미지 로드 완료");
        SetImageReady(true);
        printButton.GetComponentInChildren<TextMeshProUGUI>().text = CSVLocalizationManager.Instance.GetLocalizedText("result_button_print");
        if (enableDebugLog)
            Debug.Log("✅ 폴백 이미지 적용 완료");

        yield return null;
    }

    /// <summary>
    /// 개인화 프롬프트 생성 (유명 투자자 스타일 추가)
    /// </summary>
    string CreatePersonalizedPrompt(GameResult gameResult)
    {
        string basePrompt = "";
        string investorStyle = "";

        // 라이프스타일 등급에 따른 기본 프롬프트
        switch (gameResult.lifestyleGrade)
        {
            case LifestyleGrade.Upper:
                basePrompt = "luxury penthouse interior, premium furniture, city skyline view, elegant modern lifestyle";
                break;
            case LifestyleGrade.MiddleUpper:
                basePrompt = "modern apartment interior, comfortable living space, stylish furniture, upper middle class lifestyle";
                break;
            case LifestyleGrade.Middle:
                basePrompt = "cozy home interior, simple modern furniture, everyday comfortable living, middle class lifestyle";
                break;
            case LifestyleGrade.Lower:
                basePrompt = "modest room interior, basic furniture, simple living space, minimalist lifestyle";
                break;
        }

        // 투자 스타일에 따른 추가 요소
        var investmentStyle = AnalyzeInvestmentStyleWithCelebrity(gameResult);
        investorStyle = $", {investmentStyle.styleKeyword}";

        // 수익률에 따른 추가 요소
        if (gameResult.profitRate > 50f)
        {
            basePrompt += ", success indicators, achievement symbols";
        }
        else if (gameResult.profitRate < 0f)
        {
            basePrompt += ", modest atmosphere, practical elements";
        }

        // 분산투자에 따른 추가 요소
        if (gameResult.maxSectorsDiversified >= 4)
        {
            basePrompt += ", organized space, strategic planning elements";
        }

        return basePrompt + investorStyle;
    }

    /// <summary>
    /// 게임 결과를 사용자 통계로 변환
    /// </summary>
    System.Collections.Generic.Dictionary<string, object> CreateUserStatsFromGameResult(GameResult gameResult)
    {
        return new System.Collections.Generic.Dictionary<string, object>
        {
            ["finalReturn"] = gameResult.profitRate / 100f,
            ["lifestyleGrade"] = gameResult.lifestyleGrade.ToString(),
            ["diversificationLevel"] = gameResult.maxSectorsDiversified,
            ["totalProfit"] = gameResult.totalProfit
        };
    }

    /// <summary>
    /// 라이프스타일 등급에 따른 폴백 스프라이트 반환
    /// </summary>
    Sprite GetFallbackSprite(LifestyleGrade grade)
    {
        return grade switch
        {
            LifestyleGrade.Upper => fallbackImageUpper,
            LifestyleGrade.MiddleUpper => fallbackImageMiddleUpper,
            LifestyleGrade.Middle => fallbackImageMiddle,
            LifestyleGrade.Lower => fallbackImageLower,
            _ => fallbackImageMiddle // 기본값: middle
        };
    }

    /// <summary>
    /// 로딩 상태 표시
    /// </summary>
    void ShowLoadingState(bool isLoading)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(isLoading);
        }
    }

    /// <summary>
    /// 이미지 준비 완료 상태 설정
    /// </summary>
    void SetImageReady(bool ready)
    {
        imageReady = ready;

        // 신문 인쇄 버튼 활성화
        if (printButton != null)
        {
            printButton.interactable = ready;
        }

        if (enableDebugLog)
            Debug.Log($"🎨 이미지 준비 상태: {ready}");
    }

    /// <summary>
    /// 강제 이미지 생성 중단
    /// </summary>
    public void CancelGeneration()
    {
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
            generationCoroutine = null;
        }

        isGenerating = false;
        ShowLoadingState(false);

        // 폴백 이미지 로드
        if (currentGameResult != null)
        {
            StartCoroutine(LoadFallbackImageCoroutine());
        }

        if (enableDebugLog)
            Debug.Log("🛑 이미지 생성 중단됨");
    }

    #region 외부 인터페이스

    /// <summary>
    /// 현재 생성 중인지 확인
    /// </summary>
    public bool IsGenerating => isGenerating;

    /// <summary>
    /// 이미지 준비 완료 여부
    /// </summary>
    public bool IsImageReady => imageReady;

    /// <summary>
    /// 투자 스타일과 유명 투자자 매칭 분석
    /// </summary>
    InvestmentStyleWithCelebrity AnalyzeInvestmentStyleWithCelebrity(GameResult gameResult)
    {
        int totalTurns = gameResult.totalTurns;
        int totalTrades = 12; // 임시값 (실제로는 GameResult에서 가져와야 함)
        int maxSectors = gameResult.maxSectorsDiversified;

        // 거래 밀도 계산
        float tradeIntensity = (float)totalTrades / totalTurns;

        // 1순위: 분산투자 달인
        if (maxSectors >= 4)
        {
            return new InvestmentStyleWithCelebrity
            {
                styleKey = "style_balanced_growth_investor",
                celebrityNameKor = "레이 달리오",
                celebrityNameEng = "Ray Dalio",
                styleKeyword = "systematic diversified approach",
                description = "위험 분산을 통한 안정적 성장 추구"
            };
        }

        // 2순위: 거래 강도로 분류
        if (tradeIntensity >= 2.0f && gameResult.profitRate > 20f)
        {
            return new InvestmentStyleWithCelebrity
            {
                styleKey = "style_active_trader",
                celebrityNameKor = "조지 소로스",
                celebrityNameEng = "George Soros",
                styleKeyword = "aggressive trading philosophy",
                description = "시장 기회를 포착하는 적극적 매매"
            };
        }

        // 3순위: 고수익 집중 투자
        if (maxSectors <= 2 && gameResult.profitRate > 30f)
        {
            return new InvestmentStyleWithCelebrity
            {
                styleKey = "style_focused_investor",
                celebrityNameKor = "워런 버핏",
                celebrityNameEng = "Warren Buffett",
                styleKeyword = "value investment concentration",
                description = "선별된 우수 기업에 장기 집중 투자"
            };
        }

        // 4순위: 신중한 성장 투자
        if (tradeIntensity <= 1.0f && gameResult.profitRate >= 0f)
        {
            return new InvestmentStyleWithCelebrity
            {
                styleKey = "style_cautious_investor",
                celebrityNameKor = "벤저민 그레이엄",
                celebrityNameEng = "Benjamin Graham",
                styleKeyword = "conservative value approach",
                description = "신중한 분석을 통한 안전 투자"
            };
        }

        // 5순위: 성장 추구형
        if (gameResult.profitRate > 15f)
        {
            return new InvestmentStyleWithCelebrity
            {
                styleKey = "style_growth_investor",
                celebrityNameKor = "피터 린치",
                celebrityNameEng = "Peter Lynch",
                styleKeyword = "growth opportunity focus",
                description = "성장 기업 발굴을 통한 수익 창출"
            };
        }

        // 기본값
        return new InvestmentStyleWithCelebrity
        {
            styleKey = "style_steady_investor",
            celebrityNameKor = "존 보글",
            celebrityNameEng = "John Bogle",
            styleKeyword = "steady index approach",
            description = "꾸준하고 안정적인 장기 투자"
        };
    }

    #region 디버그 메서드

    [ContextMenu("테스트 이미지 생성")]
    void TestImageGeneration()
    {
        var testResult = new GameResult
        {
            lifestyleGrade = LifestyleGrade.MiddleUpper,
            profitRate = 35.0f,
            maxSectorsDiversified = 4,
            totalProfit = 350000
        };

        GenerateImageFromGameResult(testResult);
    }

    [ContextMenu("폴백 이미지 테스트")]
    void TestFallbackImage()
    {
        var testResult = new GameResult
        {
            lifestyleGrade = LifestyleGrade.Upper,
            profitRate = 50.0f,
            maxSectorsDiversified = 5,
            totalProfit = 500000
        };

        currentGameResult = testResult;
        StartCoroutine(LoadFallbackImageCoroutine());
    }

    /// <summary>
    /// 유명 투자자 정보 반환 (매거진 표시용)
    /// </summary>
    public InvestmentStyleWithCelebrity GetCelebrityInvestorInfo(GameResult gameResult)
    {
        return AnalyzeInvestmentStyleWithCelebrity(gameResult);
    }

    #endregion

    #region 디버그 메서드



    [ContextMenu("유명 투자자 매칭 테스트")]
    void TestCelebrityMatching()
    {
        var testResults = new GameResult[]
        {
            new GameResult { profitRate = 45f, maxSectorsDiversified = 5, totalTurns = 10 }, // 레이 달리오
            new GameResult { profitRate = 35f, maxSectorsDiversified = 2, totalTurns = 10 }, // 워런 버핏  
            new GameResult { profitRate = 25f, maxSectorsDiversified = 3, totalTurns = 10 }, // 조지 소로스
            new GameResult { profitRate = 15f, maxSectorsDiversified = 2, totalTurns = 10 }, // 벤저민 그레이엄
            new GameResult { profitRate = 20f, maxSectorsDiversified = 3, totalTurns = 10 }  // 피터 린치
        };

        foreach (var result in testResults)
        {
            var celebrity = AnalyzeInvestmentStyleWithCelebrity(result);
            Debug.Log($"📊 수익률: {result.profitRate}%, 분산도: {result.maxSectorsDiversified} → {celebrity.celebrityNameKor} ({celebrity.description})");
        }
    }

    #endregion
}

/// <summary>
/// 유명 투자자 정보를 포함한 투자 스타일 데이터
/// </summary>
[System.Serializable]
public class InvestmentStyleWithCelebrity
{
    public string styleKey;           // 스타일 키
    public string celebrityNameKor;   // 유명 투자자 이름 (한국어)
    public string celebrityNameEng;   // 유명 투자자 이름 (영어)
    public string styleKeyword;       // 이미지 생성용 키워드
    public string description;        // 스타일 설명
}
#endregion