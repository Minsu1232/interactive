using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// GameScene 셔터 시스템 - 게임 시작 인트로 + 게임 종료 아웃트로
/// </summary>
public class GameSceneShutterIntro : MonoBehaviour
{
    [Header("셔터 이미지")]
    [SerializeField] private Image shutterImage;             // 셔터 이미지 (전체 화면 크기)
    private RectTransform shutterRect;                       // 셔터의 RectTransform (자동 할당)

    [Header("인트로 텍스트")]
    [SerializeField] private TextMeshProUGUI introText;      // "장이 열립니다" 텍스트
    [SerializeField] private CanvasGroup introTextGroup;     // 텍스트 페이드용 CanvasGroup

    [Header("게임 UI")]
    [SerializeField] private CanvasGroup gameUIGroup;        // 게임 UI 전체 그룹
    [SerializeField] private GameObject[] gameUIElements;    // 개별 게임 UI 요소들

    [Header("셔터 방향 설정")]
    [SerializeField] private ShutterDirection shutterDirection = ShutterDirection.Down; // 셔터 방향

    [Header("애니메이션 설정")]
    [SerializeField] private float textDisplayDuration = 2f;     // 텍스트 표시 시간
    [SerializeField] private float shutterAnimationDuration = 2f; // 셔터 애니메이션 시간

    [Header("사운드 (선택사항)")]
    [SerializeField] private AudioSource shutterSound;      // 셔터 소리

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool skipIntroForTesting = false; // 테스트용 인트로 스킵

    public enum ShutterDirection
    {
        Up,      // 위로 올라가며 열림/내려오며 닫힘
        Down,    // 아래로 내려가며 열림/위로 올라가며 닫힘  
        Left,    // 왼쪽으로 이동하며 열림/오른쪽에서 닫힘
        Right,   // 오른쪽으로 이동하며 열림/왼쪽에서 닫힘
        Split    // 가운데서 양쪽으로 분할
    }

    // 이벤트
    public System.Action OnIntroComplete;

    // 싱글톤 (GameManager에서 쉽게 접근하기 위해)
    private static GameSceneShutterIntro instance;
    public static GameSceneShutterIntro Instance => instance;

    void Awake()
    {
        instance = this;
    }
 
    void Start()
    { // Fix: Use a lambda expression to correctly subscribe to the event
        
        if (skipIntroForTesting)
        {
            SkipIntro();
            return;
        }

        StartCoroutine(PlayShutterIntroSequence());
    }

    // ===============================================
    // 게임 시작 인트로 시퀀스
    // ===============================================

    /// <summary>
    /// 셔터 인트로 시퀀스 실행 (게임 시작 시)
    /// </summary>
    IEnumerator PlayShutterIntroSequence()
    {
        if (enableDebugLog)
            Debug.Log("🏦 GameScene 셔터 인트로 시작");

        // 1. 초기 설정
        SetupInitialState();

        // 2. "장이 열립니다" 텍스트 표시
        yield return StartCoroutine(ShowIntroText());

        // 3. 셔터 열기 효과
        yield return StartCoroutine(OpenShutter());

        // 4. 게임 UI 페이드인
        yield return StartCoroutine(ShowGameUI());

        // 5. 인트로 완료
        OnIntroComplete?.Invoke();

        if (enableDebugLog)
            Debug.Log("✅ 셔터 인트로 완료 - 게임 시작!");
    }

    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    void SetupInitialState()
    {
        // 셔터는 전체 화면을 덮도록 설정
        if (shutterImage != null)
        {
            shutterImage.gameObject.SetActive(true);
            shutterRect = shutterImage.rectTransform; // 자동으로 RectTransform 가져오기
        }

        // 인트로 텍스트 숨김
        if (introTextGroup != null)
        {
            introTextGroup.alpha = 0f;
        }

        // 게임 UI 숨김
        if (gameUIGroup != null)
        {
            gameUIGroup.alpha = 0f;
            gameUIGroup.interactable = false;
        }

        // 개별 UI 요소들도 숨김
        if (gameUIElements != null)
        {
            foreach (var element in gameUIElements)
            {
                if (element != null)
                    element.SetActive(false);
            }
        }

        if (enableDebugLog)
            Debug.Log("🎬 초기 상태 설정 완료");
    }

    /// <summary>
    /// "장이 열립니다" 텍스트 표시
    /// </summary>
    IEnumerator ShowIntroText()
    {
        if (introText != null)
        {
            // 로컬라이징된 텍스트 사용
            string openingText = GetLocalizedText("market_opening", "장이 열립니다");
            introText.text = openingText;
        }

        if (introTextGroup != null)
        {
            // 텍스트 페이드인
            introTextGroup.DOFade(1f, 0.8f).SetEase(Ease.OutQuad);

            // 텍스트 살짝 확대 효과
            if (introText != null)
            {
                introText.transform.localScale = Vector3.one * 0.8f;
                introText.transform.DOScale(1f, 0.8f).SetEase(Ease.OutBack);
            }
        }

        yield return new WaitForSeconds(textDisplayDuration);

        // 텍스트 페이드아웃
        if (introTextGroup != null)
        {
            introTextGroup.DOFade(0f, 0.5f).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 셔터 열기 효과 (게임 시작 시)
    /// </summary>
    IEnumerator OpenShutter()
    {
        if (shutterRect == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 셔터 이미지가 없어서 셔터 효과를 건너뜁니다");
            yield break;
        }

        // 셔터 소리 재생
        if (shutterSound != null)
        {
            shutterSound.Play();
        }

        // 방향에 따른 셔터 열기 애니메이션
        switch (shutterDirection)
        {
            case ShutterDirection.Up:
                shutterRect.DOAnchorPos(new Vector2(0, Screen.height + 100), shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Down:
                shutterRect.DOAnchorPos(new Vector2(0, -Screen.height - 100), shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Left:
                shutterRect.DOAnchorPos(new Vector2(-Screen.width - 100, 0), shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Right:
                shutterRect.DOAnchorPos(new Vector2(Screen.width + 100, 0), shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Split:
                yield return StartCoroutine(OpenShutterSplit());
                yield break;
        }

        yield return new WaitForSeconds(shutterAnimationDuration);
        GameManager.Instance.StartGame();
        // 셔터 완전히 숨기기
        if (shutterImage != null)
        {
            shutterImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 셔터 가운데서 양쪽으로 분할하여 열기
    /// </summary>
    IEnumerator OpenShutterSplit()
    {
        // 셔터를 복제해서 2개로 나누기
        GameObject leftShutter = Instantiate(shutterImage.gameObject, shutterImage.transform.parent);
        GameObject rightShutter = Instantiate(shutterImage.gameObject, shutterImage.transform.parent);

        RectTransform leftRect = leftShutter.GetComponent<RectTransform>();
        RectTransform rightRect = rightShutter.GetComponent<RectTransform>();

        // 크기를 절반으로 조정
        leftRect.sizeDelta = new Vector2(leftRect.sizeDelta.x / 2, leftRect.sizeDelta.y);
        rightRect.sizeDelta = new Vector2(rightRect.sizeDelta.x / 2, rightRect.sizeDelta.y);

        // 위치 조정
        leftRect.anchoredPosition = new Vector2(-leftRect.sizeDelta.x / 2, 0);
        rightRect.anchoredPosition = new Vector2(rightRect.sizeDelta.x / 2, 0);

        // 원본 셔터 숨기기
        shutterImage.gameObject.SetActive(false);

        // 양쪽으로 이동
        leftRect.DOAnchorPosX(-Screen.width, shutterAnimationDuration).SetEase(Ease.OutBack);
        rightRect.DOAnchorPosX(Screen.width, shutterAnimationDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(shutterAnimationDuration);

        // 임시 셔터들 제거
        Destroy(leftShutter);
        Destroy(rightShutter);
    }

    /// <summary>
    /// 게임 UI 표시
    /// </summary>
    IEnumerator ShowGameUI()
    {
        // 개별 UI 요소들 활성화
        if (gameUIElements != null)
        {
            foreach (var element in gameUIElements)
            {
                if (element != null)
                    element.SetActive(true);
            }
        }

        // 게임 UI 그룹 페이드인
        if (gameUIGroup != null)
        {
            gameUIGroup.interactable = true;
            gameUIGroup.DOFade(1f, 1f).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(1f);
    }

    // ===============================================
    // 게임 종료 아웃트로 시퀀스
    // ===============================================

    /// <summary>
    /// 게임 종료 시 셔터 닫기 효과 (GameManager에서 호출용)
    /// </summary>
    public void StartEndGameShutter(string endSceneName = "EndScene")
    {
        if (enableDebugLog)
            Debug.Log("🚪 게임 종료 셔터 시작");

        StartCoroutine(EndGameShutterSequence(endSceneName));
    }

    /// <summary>
    /// 게임 종료 셔터 시퀀스
    /// </summary>
    IEnumerator EndGameShutterSequence(string endSceneName)
    {
        // 1. 셔터 닫기
        yield return StartCoroutine(CloseShutter());

        // 2. 잠시 대기
        yield return new WaitForSeconds(0.5f);

        // 3. 엔드씬 전환
        if (enableDebugLog)
            Debug.Log($"🎯 셔터 닫기 완료 → {endSceneName} 씬 전환");

        SceneManager.LoadScene(endSceneName);
    }

    /// <summary>
    /// 셔터 닫기 효과 (게임 종료 시)
    /// </summary>
    IEnumerator CloseShutter()
    {
        if (shutterRect == null || shutterImage == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 셔터 이미지가 없어서 셔터 효과를 건너뜁니다");
            yield break;
        }

        // 셔터 다시 활성화
        shutterImage.gameObject.SetActive(true);

        // 셔터 소리 재생
        if (shutterSound != null)
        {
            shutterSound.Play();
        }

        // 방향에 따른 셔터 닫기 애니메이션 (열기의 반대)
        switch (shutterDirection)
        {
            case ShutterDirection.Up:
                // 위에서 내려와서 닫힘
                shutterRect.anchoredPosition = new Vector2(0, Screen.height + 100);
                shutterRect.DOAnchorPos(Vector2.zero, shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Down:
                // 아래에서 올라와서 닫힘
                shutterRect.anchoredPosition = new Vector2(0, -Screen.height - 100);
                shutterRect.DOAnchorPos(Vector2.zero, shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Left:
                // 오른쪽에서 와서 닫힘
                shutterRect.anchoredPosition = new Vector2(Screen.width + 100, 0);
                shutterRect.DOAnchorPos(Vector2.zero, shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Right:
                // 왼쪽에서 와서 닫힘
                shutterRect.anchoredPosition = new Vector2(-Screen.width - 100, 0);
                shutterRect.DOAnchorPos(Vector2.zero, shutterAnimationDuration).SetEase(Ease.OutQuart);
                break;
            case ShutterDirection.Split:
                yield return StartCoroutine(CloseShutterSplit());
                yield break;
        }

        yield return new WaitForSeconds(shutterAnimationDuration);

        if (enableDebugLog)
            Debug.Log("🚪 셔터 닫기 완료");
    }

    /// <summary>
    /// 셔터 양쪽에서 와서 가운데에서 만나기
    /// </summary>
    IEnumerator CloseShutterSplit()
    {
        // 셔터를 복제해서 2개로 나누기
        GameObject leftShutter = Instantiate(shutterImage.gameObject, shutterImage.transform.parent);
        GameObject rightShutter = Instantiate(shutterImage.gameObject, shutterImage.transform.parent);

        RectTransform leftRect = leftShutter.GetComponent<RectTransform>();
        RectTransform rightRect = rightShutter.GetComponent<RectTransform>();

        // 크기를 절반으로 조정
        leftRect.sizeDelta = new Vector2(leftRect.sizeDelta.x / 2, leftRect.sizeDelta.y);
        rightRect.sizeDelta = new Vector2(rightRect.sizeDelta.x / 2, rightRect.sizeDelta.y);

        // 시작 위치 (양쪽 끝)
        leftRect.anchoredPosition = new Vector2(-Screen.width, 0);
        rightRect.anchoredPosition = new Vector2(Screen.width, 0);

        // 원본 셔터 숨기기
        shutterImage.gameObject.SetActive(false);

        // 가운데로 이동
        leftRect.DOAnchorPosX(-leftRect.sizeDelta.x / 2, shutterAnimationDuration).SetEase(Ease.OutBack);
        rightRect.DOAnchorPosX(rightRect.sizeDelta.x / 2, shutterAnimationDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(shutterAnimationDuration);

        // 원본 셔터 다시 활성화하고 임시 셔터 제거
        shutterImage.gameObject.SetActive(true);
        shutterRect.anchoredPosition = Vector2.zero;

        Destroy(leftShutter);
        Destroy(rightShutter);
    }

    // ===============================================
    // 유틸리티 메서드들
    // ===============================================

    /// <summary>
    /// 인트로 스킵 (테스트용)
    /// </summary>
    [ContextMenu("인트로 스킵")]
    public void SkipIntro()
    {
        StopAllCoroutines();
        DOTween.KillAll();

        // 즉시 게임 상태로 전환
        if (shutterImage != null)
            shutterImage.gameObject.SetActive(false);

        if (introTextGroup != null)
            introTextGroup.alpha = 0f;

        if (gameUIGroup != null)
        {
            gameUIGroup.alpha = 1f;
            gameUIGroup.interactable = true;
        }

        if (gameUIElements != null)
        {
            foreach (var element in gameUIElements)
            {
                if (element != null)
                    element.SetActive(true);
            }
        }

        OnIntroComplete?.Invoke();

        if (enableDebugLog)
            Debug.Log("⏭️ 인트로 스킵됨");
    }

    /// <summary>
    /// 로컬라이징 텍스트 가져오기 (CSV 로컬라이징 연동)
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
    /// 테스트용 게임 종료 셔터
    /// </summary>
    [ContextMenu("게임 종료 셔터 테스트")]
    public void TestEndGameShutter()
    {
        StartEndGameShutter("EndScene");
    }

    void OnDestroy()
    {
        DOTween.KillAll();
    }
}