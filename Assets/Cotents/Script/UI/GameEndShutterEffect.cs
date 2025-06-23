using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 게임 종료 시 셔터 내리는 효과 + 엔드씬 전환
/// </summary>
public class GameEndShutterEffect : MonoBehaviour
{
    [Header("셔터 효과")]
    [SerializeField] private Canvas shutterCanvas;           // 셔터용 캔버스 (다른 UI 위에 표시)
    [SerializeField] private GameObject shutterObject;
    [SerializeField] private string endSceneName = "EndScene"; // 전환할 씬 이름

    [Header("애니메이션 설정")]
    [SerializeField] private float shutterDownDuration = 1.5f;   // 셔터 내려오는 시간
    [SerializeField] private float fadeInDuration = 0.5f;       // 페이드인 시간
    [SerializeField] private float loadingDisplayTime = 2f;     // 로딩 화면 표시 시간

    [Header("로딩 텍스트")]
    [SerializeField] private string loadingText = "결과 분석 중...";
    [SerializeField] private float textFontSize = 48f;

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    // 동적 생성되는 요소들
    
    private Image shutterImage;
    private RectTransform shutterRect;
    private GameObject loadingTextObject;

    // 싱글톤 패턴 (간단하게)
    private static GameEndShutterEffect instance;
    public static GameEndShutterEffect Instance => instance;

    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 게임 종료 셔터 효과 시작 (외부에서 호출)
    /// </summary>
    public void StartEndGameShutter()
    {
        if (enableDebugLog)
            Debug.Log("🚪 게임 종료 셔터 효과 시작");

        StartCoroutine(EndGameShutterSequence());
    }

    /// <summary>
    /// 게임 종료 셔터 시퀀스
    /// </summary>
    IEnumerator EndGameShutterSequence()
    {
        // 1. 셔터 생성 및 준비
        CreateShutter();

        // 2. 셔터 내리기 (위에서 아래로)
        yield return StartCoroutine(DropShutter());

        // 3. 페이드인 (검은 화면으로)
        yield return StartCoroutine(FadeToBlack());

        // 4. 로딩 텍스트 표시
        yield return StartCoroutine(ShowLoadingText());

        // 5. 엔드씬 전환
        SceneManager.LoadScene(endSceneName);
    }

    /// <summary>
    /// 셔터 오브젝트 생성
    /// </summary>
    void CreateShutter()
    {
        // 셔터용 캔버스가 없으면 자동으로 찾기
        if (shutterCanvas == null)
        {
            shutterCanvas = FindObjectOfType<Canvas>();
        }

        if (shutterCanvas == null)
        {
            Debug.LogError("❌ 셔터용 캔버스를 찾을 수 없습니다!");
            return;
        }

      
        shutterObject.transform.SetParent(shutterCanvas.transform, false);

        // Image 컴포넌트 추가
        shutterImage = shutterObject.AddComponent<Image>();
        
        shutterImage.raycastTarget = false; // 클릭 방지

        // RectTransform 설정
        shutterRect = shutterObject.GetComponent<RectTransform>();
        shutterRect.anchorMin = Vector2.zero;
        shutterRect.anchorMax = Vector2.one;
        shutterRect.sizeDelta = Vector2.zero;
        shutterRect.anchoredPosition = Vector2.zero;

        // 화면 위쪽에 숨겨두기
        shutterRect.anchoredPosition = new Vector2(0, Screen.height);

        // 최상위 레이어로 설정
        shutterObject.transform.SetAsLastSibling();

        if (enableDebugLog)
            Debug.Log("🎬 게임 종료 셔터 생성 완료");
    }

    /// <summary>
    /// 셔터 내리기 (위에서 아래로)
    /// </summary>
    IEnumerator DropShutter()
    {
        if (shutterRect == null)
        {
            Debug.LogError("❌ 셔터가 생성되지 않았습니다!");
            yield break;
        }

        // 위에서 아래로 내려오는 애니메이션
        shutterRect.DOAnchorPosY(0, shutterDownDuration)
                   .SetEase(Ease.OutQuart);

        yield return new WaitForSeconds(shutterDownDuration);

        if (enableDebugLog)
            Debug.Log("📉 셔터 내리기 완료");
    }

    /// <summary>
    /// 검은 화면으로 페이드인
    /// </summary>
    IEnumerator FadeToBlack()
    {
        if (shutterImage == null)
        {
            yield break;
        }

        // 셔터를 더 진한 검은색으로 페이드
        shutterImage.DOColor(Color.black, fadeInDuration)
                   .SetEase(Ease.InQuad);

        yield return new WaitForSeconds(fadeInDuration);

        if (enableDebugLog)
            Debug.Log("⚫ 페이드인 완료");
    }

    /// <summary>
    /// 로딩 텍스트 표시
    /// </summary>
    IEnumerator ShowLoadingText()
    {
        // 로딩 텍스트 오브젝트 생성
        loadingTextObject = new GameObject("LoadingText");
        loadingTextObject.transform.SetParent(shutterCanvas.transform, false);

        // TextMeshPro 컴포넌트 추가 (TMPro가 있다면)
        TMPro.TextMeshProUGUI loadingTextComponent = null;
        try
        {
            loadingTextComponent = loadingTextObject.AddComponent<TMPro.TextMeshProUGUI>();
        }
        catch
        {
            // TMPro가 없다면 일반 Text 사용
            var textComponent = loadingTextObject.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = loadingText;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = (int)textFontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
        }

        if (loadingTextComponent != null)
        {
            loadingTextComponent.text = loadingText;
            loadingTextComponent.fontSize = textFontSize;
            loadingTextComponent.color = Color.white;
            loadingTextComponent.alignment = TMPro.TextAlignmentOptions.Center;
        }

        // RectTransform 설정 (화면 중앙)
        RectTransform textRect = loadingTextObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // 최상위 레이어
        loadingTextObject.transform.SetAsLastSibling();

        // 텍스트 페이드인
        CanvasGroup textCanvasGroup = loadingTextObject.AddComponent<CanvasGroup>();
        textCanvasGroup.alpha = 0f;
        textCanvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);

        // 로딩 시간 대기
        yield return new WaitForSeconds(loadingDisplayTime);

        if (enableDebugLog)
            Debug.Log("📝 로딩 텍스트 표시 완료");
    }

    /// <summary>
    /// 컨텍스트 메뉴에서 테스트용
    /// </summary>
    [ContextMenu("게임 종료 셔터 테스트")]
    public void TestEndGameShutter()
    {
        StartEndGameShutter();
    }

    void OnDestroy()
    {
        // 생성된 오브젝트들 정리
        if (shutterObject != null)
            Destroy(shutterObject);
        if (loadingTextObject != null)
            Destroy(loadingTextObject);

        DOTween.KillAll();
    }
}