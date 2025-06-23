using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// EndScene 셔터 매니저 - 게임씬에서 넘어온 셔터를 올리면서 정산표 표시
/// </summary>
public class EndSceneShutterManager : MonoBehaviour
{
    [Header("셔터 설정")]
    [SerializeField] private Image shutterImage;             // 셔터 이미지 (GameScene에서 이어받음)
    [SerializeField] private CanvasGroup shutterGroup;       // 셔터 그룹 (페이드용)
    private RectTransform shutterRect;                       // 셔터의 RectTransform

    [Header("로딩 텍스트")]
    [SerializeField] private TextMeshProUGUI loadingText;    // "정산중입니다..." 텍스트
    [SerializeField] private CanvasGroup loadingTextGroup;   // 로딩 텍스트 그룹

    [Header("정산표 UI")]
    [SerializeField] private CanvasGroup resultUIGroup;      // 정산표 전체 그룹
    [SerializeField] private GameObject[] resultElements;    // 정산표 개별 요소들

    [Header("애니메이션 설정")]
    [SerializeField] private float shutterRiseDuration = 2f;     // 셔터 올라가는 시간
    [SerializeField] private float minLoadingTime = 3f;          // 최소 로딩 시간
    [SerializeField] private float maxLoadingTime = 5f;          // 최대 로딩 시간
    [SerializeField] private float dotAnimationSpeed = 0.5f;     // 점 애니메이션 속도

    [Header("로딩 메시지")]
    [SerializeField] private string loadingBaseText = "calculating_results"; // CSV 키값
    [SerializeField] private string loadingFallbackText = "정산중입니다";     // 기본 텍스트

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool forceComplete = false;         // 강제 완료 (테스트용)

    // 상태 관리
    private bool isDataReady = false;           // 데이터 준비 완료 여부
    private bool isLoadingComplete = false;     // 로딩 완료 여부
    private Coroutine dotAnimationCoroutine;    // 점 애니메이션 코루틴

    // 이벤트
    public System.Action OnShutterOpened;       // 셔터 열림 완료 이벤트

    void Start()
    {
        StartCoroutine(EndSceneIntroSequence());
    }

    /// <summary>
    /// EndScene 인트로 시퀀스
    /// </summary>
    IEnumerator EndSceneIntroSequence()
    {
        if (enableDebugLog)
            Debug.Log("📊 EndScene 셔터 인트로 시작");

        // 1. 초기 설정
        SetupInitialState();

        // 2. 로딩 텍스트 표시 및 데이터 로딩 시작
        StartDataLoading();
        yield return StartCoroutine(ShowLoadingText());

        // 3. 최소 시간 대기 또는 데이터 준비 완료까지
        yield return StartCoroutine(WaitForDataOrTimeout());

        // 4. 셔터 올리기
        yield return StartCoroutine(RaiseShutter());

        // 5. 정산표 표시
        yield return StartCoroutine(ShowResultUI());

        // 6. 완료
        OnShutterOpened?.Invoke();

        if (enableDebugLog)
            Debug.Log("✅ EndScene 인트로 완료!");
    }

    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    void SetupInitialState()
    {
        // 셔터 설정
        if (shutterImage != null)
        {
            shutterImage.gameObject.SetActive(true);
            shutterRect = shutterImage.rectTransform;

            // 셔터를 화면 중앙에 위치 (GameScene에서 닫힌 상태로 이어받음)
            shutterRect.anchoredPosition = Vector2.zero;
        }

        // 로딩 텍스트 숨김
        if (loadingTextGroup != null)
        {
            loadingTextGroup.alpha = 0f;
        }

        // 정산표 UI 숨김
        if (resultUIGroup != null)
        {
            resultUIGroup.alpha = 0f;
            resultUIGroup.interactable = false;
        }

        // 개별 정산표 요소들 숨김
        if (resultElements != null)
        {
            foreach (var element in resultElements)
            {
                if (element != null)
                    element.SetActive(false);
            }
        }

        if (enableDebugLog)
            Debug.Log("🎬 EndScene 초기 상태 설정 완료");
    }

    /// <summary>
    /// 데이터 로딩 시작 (비동기)
    /// </summary>
    void StartDataLoading()
    {
        StartCoroutine(LoadGameDataCoroutine());
    }

    /// <summary>
    /// 게임 데이터 로딩 코루틴
    /// </summary>
    IEnumerator LoadGameDataCoroutine()
    {
        if (enableDebugLog)
            Debug.Log("📋 게임 데이터 로딩 시작...");

        // InvestmentResultManager와 연동하여 실제 데이터 로딩 대기
        yield return StartCoroutine(WaitForInvestmentResultManagerReady());

        isDataReady = true;

        if (enableDebugLog)
            Debug.Log("✅ 게임 데이터 로딩 완료!");
    }

    /// <summary>
    /// InvestmentResultManager 준비 대기
    /// </summary>
    IEnumerator WaitForInvestmentResultManagerReady()
    {
        float timeout = 10f; // 최대 10초 대기
        float elapsed = 0f;

        // InvestmentResultManager를 찾아서 연동
        InvestmentResultManager resultManager = FindObjectOfType<InvestmentResultManager>();

        if (resultManager == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ InvestmentResultManager를 찾을 수 없음 - 기본 타이밍으로 진행");
            yield break;
        }

        if (enableDebugLog)
            Debug.Log("🔗 InvestmentResultManager 연동 대기 중...");

        // GameResult 설정 및 UI 업데이트 대기
        while (!isDataReady && elapsed < timeout)
        {
            // InvestmentResultManager가 데이터를 스스로 로드했는지 체크
            if (resultManager.gameObject.activeInHierarchy)
            {
                // 잠시 대기 후 완료 처리 (InvestmentResultManager가 Start()에서 자동으로 처리)
                yield return new WaitForSeconds(1f);
                isDataReady = true;
                break;
            }

            // 강제 완료 체크
            if (forceComplete)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("⏰ InvestmentResultManager 대기 시간 초과 - 강제 진행");
        }
        else if (isDataReady)
        {
            if (enableDebugLog)
                Debug.Log("✅ InvestmentResultManager 데이터 설정 완료!");
        }
    }

    /// <summary>
    /// 로딩 텍스트 표시
    /// </summary>
    IEnumerator ShowLoadingText()
    {
        if (loadingText == null || loadingTextGroup == null)
            yield break;

        // 로컬라이징된 기본 텍스트 설정
        string baseText = GetLocalizedText(loadingBaseText, loadingFallbackText);
        loadingText.text = baseText;

        // 텍스트 페이드인
        loadingTextGroup.DOFade(1f, 0.8f).SetEase(Ease.OutQuad);

        // 점 애니메이션 시작
        dotAnimationCoroutine = StartCoroutine(DotLoadingAnimation(baseText));

        yield return new WaitForSeconds(0.8f);
    }

    /// <summary>
    /// 점 로딩 애니메이션 ("정산중입니다", "정산중입니다.", "정산중입니다..", "정산중입니다...")
    /// </summary>
    IEnumerator DotLoadingAnimation(string baseText)
    {
        int dotCount = 0;

        while (!isLoadingComplete)
        {
            // 점 개수에 따른 텍스트 업데이트
            string dots = new string('.', dotCount);
            loadingText.text = baseText + dots;

            // 점 개수 순환 (0 → 1 → 2 → 3 → 0)
            dotCount = (dotCount + 1) % 4;

            yield return new WaitForSeconds(dotAnimationSpeed);
        }
    }

    /// <summary>
    /// 데이터 준비 완료 또는 최대 시간까지 대기
    /// </summary>
    IEnumerator WaitForDataOrTimeout()
    {
        float elapsed = 0f;

        while (elapsed < maxLoadingTime && !isDataReady)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최소 시간은 보장
        if (elapsed < minLoadingTime)
        {
            yield return new WaitForSeconds(minLoadingTime - elapsed);
        }

        isLoadingComplete = true;

        // 점 애니메이션 정지
        if (dotAnimationCoroutine != null)
        {
            StopCoroutine(dotAnimationCoroutine);
        }

        // 로딩 텍스트 페이드아웃
        if (loadingTextGroup != null)
        {
            loadingTextGroup.DOFade(0f, 0.5f).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(0.5f);

        if (enableDebugLog)
            Debug.Log($"📊 데이터 로딩 대기 완료 (경과시간: {elapsed:F1}초, 데이터준비: {isDataReady})");
    }

    /// <summary>
    /// 셔터 올리기 (위로 올라가며 사라짐)
    /// </summary>
    IEnumerator RaiseShutter()
    {
        if (shutterRect == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 셔터 이미지가 없어서 셔터 효과를 건너뜁니다");
            yield break;
        }

        if (enableDebugLog)
            Debug.Log("🔝 셔터 올리기 시작");

        // 셔터를 위로 올려서 사라지게 함
        Vector2 targetPos = new Vector2(0, Screen.height + 100);
        shutterRect.DOAnchorPos(targetPos, shutterRiseDuration).SetEase(Ease.OutQuart);

        yield return new WaitForSeconds(shutterRiseDuration);

        // 셔터 완전히 숨기기
        if (shutterImage != null)
        {
            shutterImage.gameObject.SetActive(false);
        }

        if (enableDebugLog)
            Debug.Log("✅ 셔터 올리기 완료");
    }

    /// <summary>
    /// 정산표 UI 표시
    /// </summary>
    IEnumerator ShowResultUI()
    {
        // 개별 정산표 요소들 활성화
        if (resultElements != null)
        {
            foreach (var element in resultElements)
            {
                if (element != null)
                    element.SetActive(true);
            }
        }

        // 정산표 UI 그룹 페이드인
        if (resultUIGroup != null)
        {
            resultUIGroup.interactable = true;
            resultUIGroup.DOFade(1f, 1f).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(1f);

        if (enableDebugLog)
            Debug.Log("📋 정산표 UI 표시 완료");
    }

    /// <summary>
    /// 로컬라이징 텍스트 가져오기
    /// </summary>
    string GetLocalizedText(string key, string fallback)
    {
        if (CSVLocalizationManager.Instance != null)
        {
            return CSVLocalizationManager.Instance.GetLocalizedText(key);
        }
        return fallback;
    }

    /// <summary>
    /// InvestmentResultManager에서 데이터 설정 완료 시 호출 (선택사항)
    /// </summary>
    public void OnInvestmentResultDataReady()
    {
        isDataReady = true;

        if (enableDebugLog)
            Debug.Log("✅ InvestmentResultManager에서 데이터 준비 완료 알림 받음");
    }

    /// <summary>
    /// 테스트용 강제 완료
    /// </summary>
    [ContextMenu("강제 완료")]
    public void ForceCompleteLoading()
    {
        forceComplete = true;
        isDataReady = true;

        if (enableDebugLog)
            Debug.Log("⚡ 강제 완료 실행");
    }

    void OnDestroy()
    {
        DOTween.KillAll();

        if (dotAnimationCoroutine != null)
        {
            StopCoroutine(dotAnimationCoroutine);
        }
    }
}