using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 게임 룰 패널을 관리하는 스크립트
/// 로컬라이징 지원 및 애니메이션 효과 포함
/// </summary>
public class GameRulesPanel : MonoBehaviour
{
    [Header("패널 UI 컴포넌트")]
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("텍스트 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI mainDescText;
    [SerializeField] private TextMeshProUGUI subDescText;
    [SerializeField] private TextMeshProUGUI bonusTitleText;
    [SerializeField] private TextMeshProUGUI gradeTitleText;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("등급 텍스트들")]
    [SerializeField] private TextMeshProUGUI geniusText;
    [SerializeField] private TextMeshProUGUI expertText;
    [SerializeField] private TextMeshProUGUI normalText;
    [SerializeField] private TextMeshProUGUI retryText;

    [Header("버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    [Header("보너스 카드들")]
    [SerializeField] private TextMeshProUGUI[] bonusCardTexts = new TextMeshProUGUI[5];

    [Header("애니메이션 설정")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float slideDistance = 30f;

    [Header("이벤트")]
    public Action OnGameStart;
    public Action OnPanelClosed;

    private bool isVisible = false;
    private RectTransform panelRect;

    void Awake()
    {
        panelRect = rulesPanel.GetComponent<RectTransform>();

        // 버튼 이벤트 연결
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // 초기 상태 설정
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(false);
        }
    }

    void Start()
    {
        // 로컬라이징 텍스트 설정
        SetupLocalizedTexts();

        // 언어 변경 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        // 보너스 카드 텍스트 설정
        SetupBonusCards();
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
    /// 모든 텍스트에 LocalizedText 컴포넌트 설정
    /// </summary>
    void SetupLocalizedTexts()
    {
        // 각 텍스트에 LocalizedText 추가 및 키 설정
        AddLocalizedText(titleText, "rules_title");
        AddLocalizedText(mainDescText, "rules_main_desc");
        AddLocalizedText(subDescText, "rules_sub_desc");
        AddLocalizedText(bonusTitleText, "rules_bonus_title");
        AddLocalizedText(gradeTitleText, "rules_grade_title");
        AddLocalizedText(tipText, "rules_tip_text");

        // 등급 텍스트들
        AddLocalizedText(geniusText, "rules_grade_genius");
        AddLocalizedText(expertText, "rules_grade_expert");
        AddLocalizedText(normalText, "rules_grade_normal");
        AddLocalizedText(retryText, "rules_grade_retry");

        // 버튼 텍스트
        AddLocalizedText(startButtonText, "rules_start_button");
    }

    /// <summary>
    /// 보너스 카드들 텍스트 설정 (GameManager의 보너스율 사용)
    /// </summary>
    void SetupBonusCards()
    {
        if (bonusCardTexts.Length < 5) return;

        // GameManager에서 실제 보너스율 가져오기
        float bonus5 = GameManager.Instance?.GetDiversificationBonusRate(5) ?? 20f;
        float bonus4 = GameManager.Instance?.GetDiversificationBonusRate(4) ?? 15f;
        float bonus3 = GameManager.Instance?.GetDiversificationBonusRate(3) ?? 10f;
        float bonus2 = GameManager.Instance?.GetDiversificationBonusRate(2) ?? 5f;
        float bonus1 = GameManager.Instance?.GetDiversificationBonusRate(1) ?? -10f;

        // 언어에 따른 텍스트 설정
        Language currentLang = CSVLocalizationManager.Instance?.currentLanguage ?? Language.Korean;

        if (currentLang == Language.Korean)
        {
            bonusCardTexts[0].text = $"5분야\n{bonus5:+0;-0}%";
            bonusCardTexts[1].text = $"4분야\n{bonus4:+0;-0}%";
            bonusCardTexts[2].text = $"3분야\n{bonus3:+0;-0}%";
            bonusCardTexts[3].text = $"2분야\n{bonus2:+0;-0}%";
            bonusCardTexts[4].text = $"1분야\n{bonus1:+0;-0}%";
        }
        else
        {
            bonusCardTexts[0].text = $"5 Sectors\n{bonus5:+0;-0}%";
            bonusCardTexts[1].text = $"4 Sectors\n{bonus4:+0;-0}%";
            bonusCardTexts[2].text = $"3 Sectors\n{bonus3:+0;-0}%";
            bonusCardTexts[3].text = $"2 Sectors\n{bonus2:+0;-0}%";
            bonusCardTexts[4].text = $"1 Sector\n{bonus1:+0;-0}%";
        }
    }

    /// <summary>
    /// 텍스트에 LocalizedText 컴포넌트 추가
    /// </summary>
    void AddLocalizedText(TextMeshProUGUI textComponent, string key)
    {
        if (textComponent == null) return;

        LocalizedText localizedText = textComponent.GetComponent<LocalizedText>();
        if (localizedText == null)
        {
            localizedText = textComponent.gameObject.AddComponent<LocalizedText>();
        }

        localizedText.localizationKey = key;
        localizedText.UpdateText();
    }

    /// <summary>
    /// 언어 변경 시 호출
    /// </summary>
    void OnLanguageChanged(Language newLanguage)
    {
        // 보너스 카드 텍스트 업데이트
        SetupBonusCards();
    }

    /// <summary>
    /// 룰 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (isVisible) return;

        rulesPanel.SetActive(true);
        isVisible = true;

        // 페이드 인 애니메이션
        StartCoroutine(FadeInAnimation());
    }

    /// <summary>
    /// 룰 패널 숨기기
    /// </summary>
    public void HidePanel()
    {
        if (!isVisible) return;

        // 페이드 아웃 애니메이션
        StartCoroutine(FadeOutAnimation());
    }

    /// <summary>
    /// 페이드 인 애니메이션
    /// </summary>
    System.Collections.IEnumerator FadeInAnimation()
    {
        // 초기 상태 설정
        panelCanvasGroup.alpha = 0f;
        panelRect.anchoredPosition = new Vector2(0, -slideDistance);

        float elapsedTime = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 targetPos = Vector2.zero;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;

            // Ease out 곡선
            progress = 1f - Mathf.Pow(1f - progress, 3f);

            // 알파 및 위치 업데이트
            panelCanvasGroup.alpha = progress;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

            yield return null;
        }

        // 최종 상태 설정
        panelCanvasGroup.alpha = 1f;
        panelRect.anchoredPosition = targetPos;
    }

    /// <summary>
    /// 페이드 아웃 애니메이션
    /// </summary>
    System.Collections.IEnumerator FadeOutAnimation()
    {
        float elapsedTime = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 targetPos = new Vector2(0, slideDistance);

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;

            // Ease in 곡선
            progress = Mathf.Pow(progress, 3f);

            // 알파 및 위치 업데이트
            panelCanvasGroup.alpha = 1f - progress;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

            yield return null;
        }

        // 최종 상태 설정
        panelCanvasGroup.alpha = 0f;
        panelRect.anchoredPosition = targetPos;
        rulesPanel.SetActive(false);
        isVisible = false;

        // 패널 닫힘 이벤트 발생
        OnPanelClosed?.Invoke();
    }

    /// <summary>
    /// 시작 버튼 클릭 시 호출
    /// </summary>
    void OnStartButtonClicked()
    {
        // 게임 시작 이벤트 발생
        OnGameStart?.Invoke();

        // 패널 숨기기
        HidePanel();
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출
    /// </summary>
    void OnCloseButtonClicked()
    {
        HidePanel();
    }

    /// <summary>
    /// 외부에서 패널 토글
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    /// <summary>
    /// 패널이 표시 중인지 확인
    /// </summary>
    public bool IsVisible => isVisible;
}