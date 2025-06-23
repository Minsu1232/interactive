using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 언어 선택 버튼 컨트롤러 - 패널 온/오프 방식
/// 성능 최적화를 위해 색상 전환 대신 패널 활성화/비활성화 사용
/// </summary>
public class LanguageButtonController : MonoBehaviour
{
    [Header("버튼 설정")]
    [SerializeField] private Language buttonLanguage; // 이 버튼이 담당하는 언어

    [Header("UI 컴포넌트")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject activePanel; // 선택된 상태의 그라디언트 패널
    [SerializeField] private GameObject inactivePanel; // 선택되지 않은 상태의 패널
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("텍스트 색상")]
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(1f, 1f, 1f, 0.7f);

    [Header("애니메이션 설정")]
    [SerializeField] private bool enableClickAnimation = true;
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float clickScale = 0.95f;

    private bool isCurrentLanguage = false;
    private Vector3 originalScale;

    void Start()
    {
        // 초기 설정
        originalScale = transform.localScale;

        // 버튼 클릭 이벤트 연결
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // 로컬라이징 매니저 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

            // 현재 언어 확인하여 초기 상태 설정
            UpdateButtonState(CSVLocalizationManager.Instance.currentLanguage);
        }
        else
        {
            // 로컬라이징 매니저가 아직 없으면 기본 언어(한국어)로 초기화
            UpdateButtonState(Language.Korean);
        }

        // 호버 효과 설정
        if (enableHoverEffect)
        {
            SetupHoverEffects();
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // 버튼 클릭 처리
    void OnButtonClick()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.SetLanguage(buttonLanguage);
        }

        // 클릭 애니메이션 효과
        if (enableClickAnimation)
        {
            StartCoroutine(ClickAnimation());
        }
    }

    // 언어 변경 이벤트 처리
    void OnLanguageChanged(Language newLanguage)
    {
        UpdateButtonState(newLanguage);
    }

    // 버튼 상태 업데이트 (패널 온/오프)
    void UpdateButtonState(Language currentLanguage)
    {
        bool shouldBeActive = (currentLanguage == buttonLanguage);

        if (isCurrentLanguage == shouldBeActive) return; // 상태가 동일하면 무시

        isCurrentLanguage = shouldBeActive;

        // 패널 상태 전환
        if (shouldBeActive)
        {
            // 활성 상태: 그라디언트 패널 켜기
            if (activePanel != null) activePanel.SetActive(true);
            if (inactivePanel != null) inactivePanel.SetActive(false);

            // 텍스트 색상 변경
            if (buttonText != null) buttonText.color = activeTextColor;
        }
        else
        {
            // 비활성 상태: 일반 패널 켜기
            if (activePanel != null) activePanel.SetActive(false);
            if (inactivePanel != null) inactivePanel.SetActive(true);

            // 텍스트 색상 변경
            if (buttonText != null) buttonText.color = inactiveTextColor;
        }
    }

    // 호버 효과 설정
    void SetupHoverEffects()
    {
        // EventTrigger를 사용하여 마우스 이벤트 처리
        var eventTrigger = gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // 마우스 진입
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnPointerEnter(); });
        eventTrigger.triggers.Add(pointerEnter);

        // 마우스 나가기
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnPointerExit(); });
        eventTrigger.triggers.Add(pointerExit);
    }

    // 마우스 진입 시
    void OnPointerEnter()
    {
        if (!isCurrentLanguage) // 비활성 버튼만 호버 효과
        {
            StartCoroutine(ScaleAnimation(hoverScale, 0.1f));
        }
    }

    // 마우스 나가기 시
    void OnPointerExit()
    {
        StartCoroutine(ScaleAnimation(1f, 0.1f));
    }

    // 클릭 애니메이션
    System.Collections.IEnumerator ClickAnimation()
    {
        // 살짝 줄어들었다가 원래대로
        yield return StartCoroutine(ScaleAnimation(clickScale, 0.05f));
        yield return StartCoroutine(ScaleAnimation(1f, 0.1f));
    }

    // 스케일 애니메이션
    System.Collections.IEnumerator ScaleAnimation(float targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
    }

    // 강제로 상태 설정 (디버그용)
    [ContextMenu("활성 상태로 설정")]
    public void SetActiveState()
    {
        UpdateButtonState(buttonLanguage);
    }

    [ContextMenu("비활성 상태로 설정")]
    public void SetInactiveState()
    {
        Language otherLanguage = (buttonLanguage == Language.Korean) ? Language.English : Language.Korean;
        UpdateButtonState(otherLanguage);
    }

    // 현재 상태 확인
    [ContextMenu("현재 상태 확인")]
    void PrintCurrentState()
    {
        Debug.Log($"버튼 언어: {buttonLanguage}");
        Debug.Log($"현재 활성 상태: {isCurrentLanguage}");
        Debug.Log($"활성 패널 상태: {(activePanel != null ? activePanel.activeSelf : "null")}");
        Debug.Log($"비활성 패널 상태: {(inactivePanel != null ? inactivePanel.activeSelf : "null")}");
    }
}