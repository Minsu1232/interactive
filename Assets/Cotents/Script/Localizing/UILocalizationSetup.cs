using UnityEngine;
using TMPro;

/// <summary>
/// UI의 모든 텍스트에 LocalizedText 컴포넌트를 자동으로 설정하는 헬퍼 스크립트
/// Inspector에서 한 번에 설정할 수 있도록 도움
/// </summary>
public class UILocalizationSetup : MonoBehaviour
{
    [Header("메인 UI 텍스트들")]
    [SerializeField] private TextMeshProUGUI mainTitle;
    [SerializeField] private TextMeshProUGUI mainSubtitle;
    [SerializeField] private TextMeshProUGUI cardExperienceTitle;
    [SerializeField] private TextMeshProUGUI cardNewsTitle;
    [SerializeField] private TextMeshProUGUI cardExperienceDesc;
    [SerializeField] private TextMeshProUGUI ctaText;
    [SerializeField] private TextMeshProUGUI koreanButtonText;
    [SerializeField] private TextMeshProUGUI englishButtonText;

    [Header("특별 설정")]
    [SerializeField] private float cardExperienceTitleEnglishSize = 48f;

    private float cardExperienceTitleOriginalSize = 80f;

    void Start()
    {
       
            SetupAllLocalizedTexts();
        

        // 언어 변경 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
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

    // 언어 변경 시 호출
    void OnLanguageChanged(Language newLanguage)
    {
        // Card Experience Title 크기 조정
        if (cardExperienceTitle != null)
        {
            if (newLanguage == Language.English)
            {
                cardExperienceTitle.fontSize = cardExperienceTitleEnglishSize;
            }
            else
            {
                cardExperienceTitle.fontSize = cardExperienceTitleOriginalSize;
            }
        }
    }

    // 모든 텍스트에 LocalizedText 컴포넌트 설정
    [ContextMenu("모든 로컬라이징 텍스트 설정")]
    public void SetupAllLocalizedTexts()
    {
        // 메인 타이틀
        SetupLocalizedText(mainTitle, "main_title");

        // 서브타이틀
        SetupLocalizedText(mainSubtitle, "main_subtitle");

        // 카드 제목들
        SetupLocalizedText(cardExperienceTitle, "card_experience_title");
        SetupLocalizedText(cardNewsTitle, "card_news_title");

        // 카드 설명
        SetupLocalizedText(cardExperienceDesc, "card_experience_desc");

        // CTA 텍스트
        SetupLocalizedText(ctaText, "cta_text");

        // 언어 버튼들
        SetupLocalizedText(koreanButtonText, "language_korean");
        SetupLocalizedText(englishButtonText, "language_english");

        Debug.Log("✅ 모든 UI 텍스트에 로컬라이징 설정 완료!");
    }

    // 개별 텍스트에 LocalizedText 컴포넌트 설정
    private void SetupLocalizedText(TextMeshProUGUI textComponent, string localizationKey)
    {
        if (textComponent == null)
        {
            Debug.LogWarning($"⚠️ 텍스트 컴포넌트가 null입니다: {localizationKey}");
            return;
        }

        // 이미 LocalizedText가 있는지 확인
        LocalizedText localizedText = textComponent.GetComponent<LocalizedText>();

        if (localizedText == null)
        {
            // LocalizedText 컴포넌트 추가
            localizedText = textComponent.gameObject.AddComponent<LocalizedText>();
        }

        // 로컬라이징 키 설정
        localizedText.localizationKey = localizationKey;

        // 즉시 업데이트 (에디터에서만)
        if (Application.isPlaying && CSVLocalizationManager.Instance != null)
        {
            localizedText.UpdateText();
        }

        Debug.Log($"✅ {textComponent.name}에 로컬라이징 키 '{localizationKey}' 설정 완료");
    }

    // 특정 텍스트만 개별 설정
    [ContextMenu("메인 타이틀만 설정")]
    public void SetupMainTitle() => SetupLocalizedText(mainTitle, "main_title");

    [ContextMenu("서브타이틀만 설정")]
    public void SetupMainSubtitle() => SetupLocalizedText(mainSubtitle, "main_subtitle");

    [ContextMenu("CTA 텍스트만 설정")]
    public void SetupCTAText() => SetupLocalizedText(ctaText, "cta_text");

    // 모든 로컬라이징 강제 업데이트
    [ContextMenu("모든 텍스트 강제 업데이트")]
    public void ForceUpdateAllTexts()
    {
        if (CSVLocalizationManager.Instance == null)
        {
            Debug.LogError("❌ CSVLocalizationManager가 없습니다!");
            return;
        }

        var allLocalizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (var localizedText in allLocalizedTexts)
        {
            localizedText.UpdateText();
        }

        Debug.Log($"🔄 {allLocalizedTexts.Length}개 텍스트 업데이트 완료");
    }

    // 현재 언어 변경 테스트
    [ContextMenu("한국어로 변경")]
    public void SwitchToKorean()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.SwitchToKorean();
        }
    }

    [ContextMenu("영어로 변경")]
    public void SwitchToEnglish()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.SwitchToEnglish();
        }
    }
}