using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameHistoryManager;

/// <summary>
/// EventItemUI.cs - 완전한 키 기반 로컬라이징 최종 버전
/// 구성: [이벤트 아이콘] [이벤트명 + 교육 설명 + 섹터 영향]
/// ✅ Contains() 방식 완전 제거, 키 기반 매핑으로 언어 무관하게 동작
/// ✅ 실제 적용된 핵심 섹터 변화율만 표시 (isGlobal 제외)
/// </summary>
public class EventItemUI : MonoBehaviour
{
    [Header("UI 컴포넌트들")]
    public Image eventIcon;                     // 이벤트 아이콘 (색상으로 구분)
    public TextMeshProUGUI eventTitleText;      // "🎯 AI 기술 혁신!" - 이벤트 제목
    public TextMeshProUGUI educationText;       // "💡 기술주와 반도체 섹터에 긍정적 영향" - 교육 설명
    public TextMeshProUGUI impactText;          // "📈 기술주 +15%, 반도체 +25%" - 섹터별 영향

    [Header("색상 설정")]
    public Color positiveColor = Color.green;   // 긍정적 영향 색상
    public Color negativeColor = Color.red;     // 부정적 영향 색상
    public Color neutralColor = Color.gray;     // 중립적 영향 색상

    [Header("이벤트 아이콘 색상")]
    public Color aiEventColor = Color.blue;     // AI/기술 이벤트
    public Color energyEventColor = Color.yellow; // 에너지 이벤트
    public Color rateEventColor = Color.cyan;   // 금리 이벤트
    public Color cryptoEventColor = Color.magenta; // 가상자산 이벤트
    public Color corporateEventColor = Color.black; // 기업 이벤트

    [Header("디버그")]
    public bool enableDebugLog = false;

    private EventRecord eventData;
    private EventInfo eventInfo;
    private bool isInitialized = false;

    void Start()
    {
        // 로컬라이징 매니저 초기화 대기
        StartCoroutine(WaitForLocalizationAndInitialize());
    }

    void OnDestroy()
    {
        // 언어 변경 이벤트 구독 해제
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 로컬라이징 매니저 초기화 대기 후 설정
    /// </summary>
    System.Collections.IEnumerator WaitForLocalizationAndInitialize()
    {
        // CSVLocalizationManager 초기화 완료까지 대기
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return null;
        }

        // 언어 변경 이벤트 구독
        CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

        isInitialized = true;

        // 이미 데이터가 설정되어 있으면 UI 업데이트
        if (eventData != null)
        {
            UpdateUI();
        }

        if (enableDebugLog)
            Debug.Log("✅ EventItemUI 로컬라이징 초기화 완료");
    }

    /// <summary>
    /// 언어 변경 이벤트 처리
    /// </summary>
    /// <param name="newLanguage">새로운 언어</param>
    void OnLanguageChanged(Language newLanguage)
    {
        if (eventData != null)
        {
            UpdateUI();

            if (enableDebugLog)
                Debug.Log($"🌍 EventItemUI 언어 변경 적용: {newLanguage}");
        }
    }

    /// <summary>
    /// 이벤트 데이터 설정 및 UI 업데이트
    /// </summary>
    public void SetData(EventRecord eventRecord)
    {
        eventData = eventRecord;

        // 로컬라이징이 준비되었으면 즉시 업데이트, 아니면 대기
        if (isInitialized)
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// 전체 UI 업데이트 - 완전한 키 기반 로컬라이징 적용
    /// </summary>
    void UpdateUI()
    {
        if (eventData == null || !isInitialized) return;

        var locManager = CSVLocalizationManager.Instance;
        if (locManager == null) return;

        // ✅ 키 기반 이벤트 분석
        eventInfo = AnalyzeEventByKey(eventData.eventName);

        // 이벤트 아이콘 색상 설정
        UpdateEventIcon(eventInfo.eventCategory);

        // 이벤트 제목 설정 (로컬라이징 적용)
        UpdateEventTitle(eventInfo, locManager);

        // 교육 설명 설정 (로컬라이징 적용)
        UpdateEducationText(eventInfo, locManager);

        // 섹터별 영향 설정 (로컬라이징 적용)
        UpdateImpactText(eventInfo, locManager);

        if (enableDebugLog)
        {
            Debug.Log($"📰 이벤트 UI 업데이트 (키 기반): {eventData.eventName}");
            Debug.Log($"  이벤트 키: {eventInfo.eventKey}");
            Debug.Log($"  교육 키: {eventInfo.educationKey}");
            Debug.Log($"  영향 섹터: {eventInfo.affectedSectors.Count}개");
        }
    }

    /// <summary>
    /// ✅ 완전히 개선된 키 기반 이벤트 분석 - 실제 적용된 변화율 기반
    /// 핵심 섹터 영향만 간단하게 표시 (isGlobal 효과 제외)
    /// </summary>
    EventInfo AnalyzeEventByKey(string eventName)
    {
        var eventInfo = new EventInfo
        {
            originalName = eventName,
            eventCategory = EventCategory.General,
            educationKey = "event_education_default",
            emoji = "📰"
        };

        // ✅ GameManager에서 실제 이벤트 데이터 찾기 (키 기반)
        var actualEvent = FindEventInGameManager(eventName);

        if (actualEvent != null)
        {
            // ✅ eventKey 기반으로 정확한 매핑
            eventInfo.eventKey = actualEvent.eventKey;
            eventInfo.eventCategory = actualEvent.GetEventCategory();

            // 🔍 핵심: 실제 적용된 섹터별 변화율 수집 (isGlobal 제외)
            foreach (var effect in actualEvent.effects)
            {
                // 전역 효과는 제외하고 특정 섹터 효과만 처리
                if (!effect.isGlobal)
                {
                    // 실제 적용된 평균 변화율 계산
                    float actualChangeRate = GetActualSectorChangeRate(effect);

                    eventInfo.affectedSectors.Add(new SectorImpact
                    {
                        sector = effect.sector,
                        rate = Mathf.Abs(actualChangeRate), // 실제 적용된 변화율 사용
                        isPositive = actualChangeRate > 0
                    });
                }
            }

            // ✅ eventKey 기반으로 이모지와 교육 키 결정
            DetermineEventPropertiesByKey(eventInfo, actualEvent.eventKey);
        }
        else
        {
            // 폴백: 이벤트를 찾을 수 없는 경우
            eventInfo.emoji = "📰";
            eventInfo.educationKey = "event_education_default";
            if (enableDebugLog)
                Debug.LogWarning($"⚠️ GameManager에서 이벤트를 찾을 수 없음: {eventName}");
        }

        return eventInfo;
    }

    /// <summary>
    /// 🆕 실제 적용된 섹터 변화율 계산 메서드
    /// 개별 변동이 있는 경우 평균값으로 계산, 없으면 기본값 사용
    /// </summary>
    float GetActualSectorChangeRate(StockEffect effect)
    {
        if (effect.useIndividualVariation)
        {
            // 개별 변동이 있는 경우: 기본값 + 변동 범위의 평균
            float avgVariation = (effect.variationMin + effect.variationMax) / 2f;
            return effect.changeRate + avgVariation;
        }
        else
        {
            // 개별 변동이 없는 경우: 기본 변화율 그대로 사용
            return effect.changeRate;
        }
    }

    /// <summary>
    /// GameManager에서 이벤트 찾기 (제목 매칭)
    /// 현재 표시된 이벤트명과 실제 이벤트 데이터를 연결하는 중요한 메서드
    /// </summary>
    TurnEvent FindEventInGameManager(string eventName)
    {
        if (GameManager.Instance?.scheduledEvents == null) return null;

        foreach (var kvp in GameManager.Instance.scheduledEvents)
        {
            // ✅ 로컬라이징된 제목과 비교
            // 현재 UI에 표시된 이벤트명이 어떤 실제 이벤트인지 찾아냄
            string localizedTitle = GameManager.Instance.GetLocalizedEventTitle(kvp.Value);
            if (localizedTitle == eventName)
            {
                if (enableDebugLog)
                    Debug.Log($"✅ 이벤트 발견 (로컬라이징된 제목 매칭): {eventName} → {kvp.Value.eventKey}");
                return kvp.Value; // 여기서 실제 effects 데이터를 포함한 TurnEvent 반환
            }

            // 폴백: 레거시 title 필드와도 비교
            if (kvp.Value.title == eventName)
            {
                if (enableDebugLog)
                    Debug.Log($"✅ 이벤트 발견 (레거시 제목 매칭): {eventName} → {kvp.Value.eventKey}");
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// ✅ eventKey 기반으로 이벤트 속성 결정 (완전한 키 기반)
    /// </summary>
    void DetermineEventPropertiesByKey(EventInfo eventInfo, string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            eventInfo.emoji = "📰";
            eventInfo.educationKey = "event_education_default";
            return;
        }

        // ✅ eventKey 기반 매핑 (언어에 무관)
        switch (eventKey)
        {
            case "ai_innovation":
                eventInfo.emoji = "🎯";
                eventInfo.educationKey = "event_education_tech";
                break;

            case "energy_policy":
                eventInfo.emoji = "⚡";
                eventInfo.educationKey = "event_education_energy";
                break;

            case "interest_rate":
                eventInfo.emoji = "🏦";
                eventInfo.educationKey = "event_education_interest";
                break;

            case "crypto_regulation":
                eventInfo.emoji = "💰";
                eventInfo.educationKey = "event_education_crypto";
                break;

            default:
                eventInfo.emoji = "📰";
                eventInfo.educationKey = "event_education_default";
                break;
        }

        if (enableDebugLog)
            Debug.Log($"🔑 키 기반 속성 설정: {eventKey} → {eventInfo.emoji} + {eventInfo.educationKey}");
    }

    /// <summary>
    /// 이벤트 아이콘 업데이트
    /// </summary>
    void UpdateEventIcon(EventCategory eventCategory)
    {
        if (eventIcon == null) return;

        eventIcon.color = eventCategory switch
        {
            EventCategory.Technology => aiEventColor,
            EventCategory.Energy => energyEventColor,
            EventCategory.Interest => rateEventColor,
            EventCategory.Crypto => cryptoEventColor,
            EventCategory.Corporate => corporateEventColor,
            _ => neutralColor
        };
    }

    /// <summary>
    /// ✅ 이벤트 제목 업데이트 - 이미 로컬라이징된 제목 사용
    /// </summary>
    void UpdateEventTitle(EventInfo eventInfo, CSVLocalizationManager locManager)
    {
        if (eventTitleText == null) return;

        // ✅ 이미 로컬라이징된 제목 + 이모지
        eventTitleText.text = $"{eventInfo.emoji} {eventInfo.originalName}";
    }

    /// <summary>
    /// ✅ 교육 설명 업데이트 - 완전한 로컬라이징 적용
    /// </summary>
    void UpdateEducationText(EventInfo eventInfo, CSVLocalizationManager locManager)
    {
        if (educationText == null) return;

        // 로컬라이징된 교육 텍스트 사용
        string localizedEducation = locManager.GetLocalizedText(eventInfo.educationKey);

        // 이모지 💡 추가
        educationText.text = $"💡 {localizedEducation}";
        educationText.color = neutralColor;
    }

    /// <summary>
    /// ✅ 섹터별 영향 업데이트 - 핵심 섹터만 간단하게 표시
    /// isGlobal 효과는 제외하고 실제 적용된 변화율만 표시
    /// </summary>
    void UpdateImpactText(EventInfo eventInfo, CSVLocalizationManager locManager)
    {
        if (impactText == null) return;

        // 핵심 섹터 영향이 없으면 빈 텍스트
        if (eventInfo.affectedSectors.Count == 0)
        {
            impactText.text = "";
            return;
        }

        string impactString = "";
        bool hasPositive = false;
        bool hasNegative = false;

        // 🔍 핵심 섹터의 실제 변화율만 표시 (간단하게)
        foreach (var sectorImpact in eventInfo.affectedSectors)
        {
            string emoji = sectorImpact.isPositive ? "📈" : "📉";
            string sign = sectorImpact.isPositive ? "+" : "-";
            string sectorName = GetLocalizedSectorName(sectorImpact.sector, locManager);

            // 💡 실제 적용된 변화율을 정수로 간단하게 표시
            impactString += $"{emoji} {sectorName} {sign}{sectorImpact.rate:0}% ";

            if (sectorImpact.isPositive) hasPositive = true;
            else hasNegative = true;
        }

        impactText.text = impactString.Trim();

        // 색상 설정 - 핵심 영향에 따라
        if (hasPositive && hasNegative)
            impactText.color = neutralColor;    // 혼재시 중립색
        else if (hasPositive)
            impactText.color = positiveColor;   // 전체 긍정시 녹색
        else
            impactText.color = negativeColor;   // 전체 부정시 빨간색
    }

    /// <summary>
    /// ✅ 섹터 표시명 가져오기 - 올바른 키 사용
    /// 섹터 이름을 현재 언어에 맞게 로컬라이징
    /// </summary>
    string GetLocalizedSectorName(StockSector sector, CSVLocalizationManager locManager)
    {
        string sectorKey = sector switch
        {
            StockSector.TECH => "sector_tech",     // 기술주
            StockSector.SEM => "sector_sem",       // 반도체
            StockSector.EV => "sector_ev",         // 전기차
            StockSector.CORP => "sector_corp",     // 기업
            StockSector.CRYPTO => "sector_crypto", // 가상자산
            _ => "sector_tech" // 기본값
        };

        return locManager.GetLocalizedText(sectorKey);
    }
}

// ✅ 최종 완성된 데이터 구조체들
[System.Serializable]
public class EventInfo
{
    public string originalName;                     // 원본 이벤트명 (이미 로컬라이징됨)
    public string eventKey;                         // ✅ 추가: 이벤트 키 (예: "ai_innovation")
    public EventCategory eventCategory;             // ✅ 수정: EventType → EventCategory
    public string emoji;                            // 이모지
    public string educationKey;                     // 교육 텍스트 로컬라이징 키
    public List<SectorImpact> affectedSectors = new List<SectorImpact>();
}

[System.Serializable]
public class SectorImpact
{
    public StockSector sector;
    public float rate;
    public bool isPositive;
}