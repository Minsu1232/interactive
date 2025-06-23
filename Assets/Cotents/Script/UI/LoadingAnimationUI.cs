using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// 어디서든 사용 가능한 글로벌 로딩 UI (씬 전환에도 살아남음)
/// 싱글톤 패턴 + DontDestroyOnLoad 적용
/// </summary>
public class LoadingAnimationUI : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static LoadingAnimationUI Instance { get; private set; }

    [Header("UI 컴포넌트")]
    public TextMeshProUGUI loadingText;
    public Image backgroundPanel;
    public CanvasGroup canvasGroup;
    public Canvas canvas;

    [Header("애니메이션 설정")]
    [Range(0.2f, 1.0f)]
    public float dotAnimationSpeed = 0.5f;

    [Range(0.1f, 0.5f)]
    public float fadeSpeed = 0.2f;

    [Header("텍스트 설정")]
    public string baseTextKey = "loading_saving";
    public string fallbackText = "저장중입니다";
    public int maxDots = 3;

    [Header("다국어 지원")]
    public bool useLocalization = true;

    [Header("Canvas 설정")]
    public int sortingOrder = 9999;

    [Header("디버그")]
    public bool enableDebugLog = false;

    // Private 변수들
    private Coroutine animationCoroutine;
    private string currentBaseText;
    private bool isShowing = false;

    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeComponents();

            if (enableDebugLog)
            {
                Debug.Log("🌟 글로벌 로딩 UI 초기화 완료");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeComponents()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (backgroundPanel == null) backgroundPanel = GetComponent<Image>();
        if (canvas == null) canvas = GetComponent<Canvas>();

        // Canvas 설정
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        gameObject.SetActive(false);
    }

    // ===========================
    // 🌟 메인 API (정적 메서드)
    // ===========================

    public static void Show(string textKey = "loading_saving")
    {
        if (Instance != null)
        {
            Instance.StartShow(textKey);
        }
    }

    public static void Hide()
    {
        if (Instance != null)
        {
            Instance.StartHide();
        }
    }

    public static bool IsShowing()
    {
        return Instance != null && Instance.isShowing;
    }

    public static void UpdateText(string textKey, string newFallback = "")
    {
        if (Instance != null)
        {
            Instance.ChangeText(textKey, newFallback);
        }
    }

    // ===========================
    // 🎯 내부 구현 메서드들
    // ===========================

    void StartShow(string textKey)
    {
        if (isShowing) return;

        baseTextKey = textKey;
        UpdateBaseText();

        if (enableDebugLog)
        {
            Debug.Log($"🔄 로딩 시작: {currentBaseText}");
        }

        gameObject.SetActive(true);
        isShowing = true;
        StartCoroutine(ShowCoroutine());
    }

    void ChangeText(string textKey, string newFallback)
    {
        baseTextKey = textKey;

        if (!string.IsNullOrEmpty(newFallback))
        {
            fallbackText = newFallback;
        }

        UpdateBaseText();

        if (enableDebugLog)
        {
            Debug.Log($"🔄 로딩 텍스트 변경: {currentBaseText}");
        }
    }

    void StartHide()
    {
        if (!isShowing) return;

        if (enableDebugLog)
        {
            Debug.Log("✅ 로딩 종료");
        }

        isShowing = false;
        StartCoroutine(HideCoroutine());
    }

    IEnumerator ShowCoroutine()
    {
        // 페이드 인
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f));

        // 점 애니메이션 시작
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(DotAnimationLoop());
    }

    IEnumerator HideCoroutine()
    {
        // 애니메이션 중지
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        // 페이드 아웃
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f));
        gameObject.SetActive(false);
    }

    IEnumerator DotAnimationLoop()
    {
        int currentDots = 0;

        while (isShowing)
        {
            string dots = new string('.', currentDots);

            if (loadingText != null)
            {
                loadingText.text = currentBaseText + dots;
            }

            currentDots = (currentDots + 1) % (maxDots + 1);
            yield return new WaitForSeconds(dotAnimationSpeed);
        }
    }

    IEnumerator FadeCanvasGroup(float fromAlpha, float toAlpha)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < fadeSpeed)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / fadeSpeed);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = toAlpha;
    }

    void UpdateBaseText()
    {
        if (useLocalization && CSVLocalizationManager.Instance != null)
        {
            currentBaseText = CSVLocalizationManager.Instance.GetLocalizedText(baseTextKey);

            if (string.IsNullOrEmpty(currentBaseText) || currentBaseText == baseTextKey)
            {
                currentBaseText = fallbackText;
            }
        }
        else
        {
            currentBaseText = fallbackText;
        }
    }

    // 에디터 테스트용
    void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.Space))
        {
            if (isShowing) Hide();
            else Show("loading_test");
        }
    }
}

// =============================
// 🎯 편의 헬퍼 클래스
// =============================
public static class GlobalLoading
{
    public static void ShowSaving() => LoadingAnimationUI.Show("loading_saving");
    public static void ShowDataLoading() => LoadingAnimationUI.Show("loading_data");
    public static void ShowProcessing() => LoadingAnimationUI.Show("loading_processing");
    public static void ShowPDFGenerating() => LoadingAnimationUI.Show("loading_pdf");
    public static void ShowGameLoading() => LoadingAnimationUI.Show("loading_game");

    /// <summary>
    /// 커스텀 메시지로 로딩 표시 (CSV 없을 때)
    /// </summary>
    public static void ShowCustom(string message)
    {
        LoadingAnimationUI.UpdateText("custom", message);
        LoadingAnimationUI.Show("custom");
    }

    /// <summary>
    /// 런타임에 텍스트 변경 (진행 상황 업데이트)
    /// </summary>
    public static void UpdateText(string csvKey, string fallbackText = "")
    {
        LoadingAnimationUI.UpdateText(csvKey, fallbackText);
    }

    public static void Hide() => LoadingAnimationUI.Hide();
    public static bool IsShowing() => LoadingAnimationUI.IsShowing();
}