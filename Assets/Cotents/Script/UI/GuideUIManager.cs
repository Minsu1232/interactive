using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// 게임 가이드 UI 매니저 - 로컬라이징 지원
/// </summary>
public class GuideUIManager : MonoBehaviour
{
    [Header("📖 가이드 패널 UI")]
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button guideToggleButton; // 외부에서 가이드 열기 버튼

    [Header("📝 메인 텍스트 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI titleText;           // 제목
    [SerializeField] private TextMeshProUGUI subtitleText;        // 부제목

    [Header("📊 게임 정보 박스")]
    [SerializeField] private TextMeshProUGUI gameInfoTitleText;   // "🎯 게임 정보" 
    [SerializeField] private TextMeshProUGUI totalTurnsText;      // "총 턴\n10"
    [SerializeField] private TextMeshProUGUI turnTimeText;        // "턴 시간\n30초"
    [SerializeField] private TextMeshProUGUI initialMoneyText;    // "초기자금\n100만원"
    [SerializeField] private TextMeshProUGUI tradingFeeText;      // "수수료\n0.25%"
    [SerializeField] private TextMeshProUGUI gameObjectiveText;   // "🏆 게임 목표\n10턴 동안 주식을 사고팔아 최대 수익을 만드세요!"

    [Header("🖱️ 기본 조작 박스")]
    [SerializeField] private TextMeshProUGUI controlsTitleText;   // "🖱️ 기본 조작"
    [SerializeField] private TextMeshProUGUI buyGuideText;        // "📈 매수하기 주식 구매\n주식을 클릭하고 매수하기 버튼으로 구매"
    [SerializeField] private TextMeshProUGUI sellGuideText;       // "📉 매도 주식 판매\n보유 주식을 클릭하고 매도 버튼으로 판매"
    [SerializeField] private TextMeshProUGUI quantityGuideText;   // "🔢 수량 조절\n+1%, +5%, +10%, 전액 버튼 사용"

    [Header("⚡ 특별 기능 박스")]
    [SerializeField] private TextMeshProUGUI featuresTitleText;   // "⚡ 특별 기능"
    [SerializeField] private TextMeshProUGUI nextTurnGuideText;   // "⏭️ 다음 턴 턴 넘기기\n거래를 마쳤다면 즉시 다음 턴으로"
    [SerializeField] private TextMeshProUGUI stopGuideText;       // "⏸️ 5초 스탑 시간 정지\n급할 때 5초간 시간을 멈춤 (턴당 1회)"
    [SerializeField] private TextMeshProUGUI newsGuideText;       // "📺 뉴스 티커\n화면 상단 뉴스에서 다음 턴 이벤트 확인"

    [Header("💡 투자 팁 박스")]
    [SerializeField] private TextMeshProUGUI tipsTitleText;       // "💡 투자 팁"
    [SerializeField] private TextMeshProUGUI diversificationText; // "🌈 분산투자 보너스\n여러 섹터 투자시 게임 끝에 보너스"
    [SerializeField] private TextMeshProUGUI eventTipsText;       // "📰 이벤트 활용\n3, 5, 7, 9턴에 이벤트 발생"
    [SerializeField] private TextMeshProUGUI feeManagementText;   // "💰 수수료 관리\n매매시마다 0.25% 수수료 발생"

    [Header("🚀 빠른 가이드 박스")]
    [SerializeField] private TextMeshProUGUI quickGuideTitleText; // "🚀 빠른 시작 가이드"
    [SerializeField] private TextMeshProUGUI quickStepsText;      // "1단계: 뉴스 확인 → 여러 섹터 분산투자\n2단계: 이벤트 전에 해당 섹터 매수\n3단계: 이벤트 후 높은 가격에 매도\n4단계: 5초 스탑은 정말 급할 때만!\n5단계: 마지막 턴에 모든 주식 현금화"

    // 내부 변수
    private bool isGuideOpen = false;

    // 싱글톤 (옵션)
    public static GuideUIManager Instance { get; private set; }

    #region Unity 생명주기

    void Awake()
    {
        // 싱글톤 설정 (선택사항)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 초기 설정
        if (guidePanel != null)
            guidePanel.SetActive(false);
    }

    void Start()
    {
        SetupButtons();

        // 로컬라이징 매니저 이벤트 구독
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

            // 초기화 완료 후 텍스트 업데이트
            if (CSVLocalizationManager.Instance.IsInitialized)
            {
                UpdateAllTexts();
            }
            else
            {
                // 초기화 대기
                StartCoroutine(WaitForLocalizationAndUpdate());
            }
        }
        else
        {
            // 로컬라이징 매니저가 없으면 기본 텍스트 설정
            SetDefaultTexts();
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

    #endregion

    #region 버튼 설정 및 이벤트

    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGuide);
        }

        if (guideToggleButton != null)
        {
            guideToggleButton.onClick.AddListener(ToggleGuide);
        }
    }

    /// <summary>
    /// 가이드 열기/닫기 토글
    /// </summary>
    public void ToggleGuide()
    {
        if (isGuideOpen)
            CloseGuide();
        else
            OpenGuide();
    }

    /// <summary>
    /// 가이드 열기
    /// </summary>
    public void OpenGuide()
    {
        if (guidePanel == null) return;

        guidePanel.SetActive(true);
        isGuideOpen = true;

        // 텍스트 업데이트 (언어가 바뀌었을 수도 있음)
        UpdateAllTexts();
    }

    /// <summary>
    /// 가이드 닫기
    /// </summary>
    public void CloseGuide()
    {
        if (guidePanel == null) return;

        guidePanel.SetActive(false);
        isGuideOpen = false;
    }

    #endregion

    #region 로컬라이징 처리

    /// <summary>
    /// 언어 변경 이벤트 처리
    /// </summary>
    void OnLanguageChanged(Language newLanguage)
    {
        UpdateAllTexts();
    }

    /// <summary>
    /// 로컬라이징 초기화 대기 후 텍스트 업데이트
    /// </summary>
    System.Collections.IEnumerator WaitForLocalizationAndUpdate()
    {
        while (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }

        UpdateAllTexts();
    }

    /// <summary>
    /// 모든 텍스트 업데이트
    /// </summary>
    void UpdateAllTexts()
    {
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            SetDefaultTexts();
            return;
        }

        // 메인 제목/부제목
        SetText(titleText, "guide_title");
        SetText(subtitleText, "guide_subtitle");

        // 각 박스별 텍스트 설정
        SetGameInfoTexts();
        SetControlsTexts();
        SetFeaturesTexts();
        SetTipsTexts();
        SetQuickGuideTexts();
    }

    /// <summary>
    /// 안전한 텍스트 설정
    /// </summary>
    void SetText(TextMeshProUGUI textComponent, string key)
    {
        if (textComponent == null) return;

        string localizedText = CSVLocalizationManager.Instance.GetLocalizedText(key);
        textComponent.text = localizedText;
    }

    #endregion

    #region 박스별 텍스트 설정

    /// <summary>
    /// 게임 정보 박스 텍스트들 설정
    /// </summary>
    void SetGameInfoTexts()
    {
        var loc = CSVLocalizationManager.Instance;

        // 제목
        if (gameInfoTitleText != null)
            gameInfoTitleText.text = $"🎯 {loc.GetLocalizedText("guide_game_info_title")}";

        // 각각의 통계 박스들
        if (totalTurnsText != null)
            totalTurnsText.text = $"{loc.GetLocalizedText("guide_total_turns")}\n{loc.GetLocalizedText("guide_turns_value")}";

        if (turnTimeText != null)
            turnTimeText.text = $"{loc.GetLocalizedText("guide_turn_time")}\n{loc.GetLocalizedText("guide_time_value")}";

        if (initialMoneyText != null)
            initialMoneyText.text = $"{loc.GetLocalizedText("guide_initial_money")}\n{loc.GetLocalizedText("guide_money_value")}";

        if (tradingFeeText != null)
            tradingFeeText.text = $"{loc.GetLocalizedText("guide_trading_fee")}\n{loc.GetLocalizedText("guide_fee_value")}";

        // 게임 목표
        if (gameObjectiveText != null)
        {
            gameObjectiveText.text = $"🏆 {loc.GetLocalizedText("guide_objective")}\n\n{loc.GetLocalizedText("guide_objective_desc")}";
        }
    }

    /// <summary>
    /// 기본 조작 박스 텍스트들 설정
    /// </summary>
    void SetControlsTexts()
    {
        var loc = CSVLocalizationManager.Instance;

        // 제목
        if (controlsTitleText != null)
            controlsTitleText.text = $"🖱️ {loc.GetLocalizedText("guide_controls_title")}";

        // 매수 가이드
        if (buyGuideText != null)
        {
            buyGuideText.text = $"📈 {loc.GetLocalizedText("button_buy")} {loc.GetLocalizedText("guide_stock_purchase")}\n\n{loc.GetLocalizedText("guide_buy_desc")}";
        }

        // 매도 가이드
        if (sellGuideText != null)
        {
            sellGuideText.text = $"📉 {loc.GetLocalizedText("button_sell")} {loc.GetLocalizedText("guide_stock_sell")}\n\n{loc.GetLocalizedText("guide_sell_desc")}";
        }

        // 수량 조절 가이드
        if (quantityGuideText != null)
        {
            quantityGuideText.text = $"🔢 {loc.GetLocalizedText("guide_quantity_control")}\n\n{loc.GetLocalizedText("guide_quantity_desc")}";
        }
    }

    /// <summary>
    /// 특별 기능 박스 텍스트들 설정
    /// </summary>
    void SetFeaturesTexts()
    {
        var loc = CSVLocalizationManager.Instance;

        // 제목
        if (featuresTitleText != null)
            featuresTitleText.text = $"⚡ {loc.GetLocalizedText("guide_features_title")}";

        // 다음 턴 가이드
        if (nextTurnGuideText != null)
        {
            nextTurnGuideText.text = $"⏭️ {loc.GetLocalizedText("button_next_turn")} {loc.GetLocalizedText("guide_skip_turn")}\n\n{loc.GetLocalizedText("guide_skip_desc")}";
        }

        // 5초 스탑 가이드
        if (stopGuideText != null)
        {
            stopGuideText.text = $"⏸️ {loc.GetLocalizedText("guide_5sec_stop")} {loc.GetLocalizedText("guide_time_stop")}\n\n{loc.GetLocalizedText("guide_stop_desc")}";
        }

        // 뉴스 가이드
        if (newsGuideText != null)
        {
            newsGuideText.text = $"📺 {loc.GetLocalizedText("guide_news_ticker")}\n\n{loc.GetLocalizedText("guide_news_desc")}";
        }
    }

    /// <summary>
    /// 투자 팁 박스 텍스트들 설정
    /// </summary>
    void SetTipsTexts()
    {
        var loc = CSVLocalizationManager.Instance;

        // 제목
        if (tipsTitleText != null)
            tipsTitleText.text = $"💡 {loc.GetLocalizedText("guide_tips_title")}";

        // 분산투자 팁
        if (diversificationText != null)
        {
            diversificationText.text = $"🌈 {loc.GetLocalizedText("invest_diversification")} {loc.GetLocalizedText("guide_bonus")}\n\n{loc.GetLocalizedText("guide_diversification_desc")}";
        }

        // 이벤트 활용 팁
        if (eventTipsText != null)
        {
            eventTipsText.text = $"📰 {loc.GetLocalizedText("guide_event_usage")}\n\n{loc.GetLocalizedText("guide_event_desc")}";
        }

        // 수수료 관리 팁
        if (feeManagementText != null)
        {
            feeManagementText.text = $"💰 {loc.GetLocalizedText("guide_fee_management")}\n\n{loc.GetLocalizedText("guide_fee_desc")}";
        }
    }

    /// <summary>
    /// 빠른 가이드 박스 텍스트들 설정
    /// </summary>
    void SetQuickGuideTexts()
    {
        var loc = CSVLocalizationManager.Instance;

        // 제목
        if (quickGuideTitleText != null)
            quickGuideTitleText.text = $"🚀 {loc.GetLocalizedText("guide_quick_start")}";

        // 단계별 가이드
        if (quickStepsText != null)
        {
            quickStepsText.text = $"{loc.GetLocalizedText("guide_step1")}\n" +
                                 $"{loc.GetLocalizedText("guide_step2")}\n" +
                                 $"{loc.GetLocalizedText("guide_step3")}\n" +
                                 $"{loc.GetLocalizedText("guide_step4")}\n" +
                                 $"{loc.GetLocalizedText("guide_step5")}";
        }
    }

    #endregion

    #region 기본 텍스트 (로컬라이징 없을 때)

    /// <summary>
    /// 기본 텍스트 설정 (로컬라이징 실패시)
    /// </summary>
    void SetDefaultTexts()
    {
        // 메인 제목/부제목
        if (titleText != null)
            titleText.text = "🎮 게임 가이드";
        if (subtitleText != null)
            subtitleText.text = "AI 투자 시뮬레이터 플레이 방법";

        // 게임 정보 박스
        if (gameInfoTitleText != null)
            gameInfoTitleText.text = "🎯 게임 정보";
        if (totalTurnsText != null)
            totalTurnsText.text = "총 턴\n10";
        if (turnTimeText != null)
            turnTimeText.text = "턴 시간\n30초";
        if (initialMoneyText != null)
            initialMoneyText.text = "초기자금\n100만원";
        if (tradingFeeText != null)
            tradingFeeText.text = "수수료\n0.25%";
        if (gameObjectiveText != null)
            gameObjectiveText.text = "🏆 게임 목표\n10턴 동안 주식을 사고팔아 최대 수익을 만드세요!";

        // 기본 조작 박스
        if (controlsTitleText != null)
            controlsTitleText.text = "🖱️ 기본 조작";
        if (buyGuideText != null)
            buyGuideText.text = "📈 매수하기 주식 구매\n주식을 클릭하고 매수하기 버튼으로 구매";
        if (sellGuideText != null)
            sellGuideText.text = "📉 매도 주식 판매\n보유 주식을 클릭하고 매도 버튼으로 판매";
        if (quantityGuideText != null)
            quantityGuideText.text = "🔢 수량 조절\n+1%, +5%, +10%, 전액 버튼 사용";

        // 특별 기능 박스
        if (featuresTitleText != null)
            featuresTitleText.text = "⚡ 특별 기능";
        if (nextTurnGuideText != null)
            nextTurnGuideText.text = "⏭️ 다음 턴 턴 넘기기\n거래를 마쳤다면 즉시 다음 턴으로";
        if (stopGuideText != null)
            stopGuideText.text = "⏸️ 5초 스탑 시간 정지\n급할 때 5초간 시간을 멈춤 (턴당 1회)";
        if (newsGuideText != null)
            newsGuideText.text = "📺 뉴스 티커\n화면 상단 뉴스에서 다음 턴 이벤트 확인";

        // 투자 팁 박스
        if (tipsTitleText != null)
            tipsTitleText.text = "💡 투자 팁";
        if (diversificationText != null)
            diversificationText.text = "🌈 분산투자 보너스\n여러 섹터 투자시 게임 끝에 보너스";
        if (eventTipsText != null)
            eventTipsText.text = "📰 이벤트 활용\n3, 5, 7, 9턴에 이벤트 발생";
        if (feeManagementText != null)
            feeManagementText.text = "💰 수수료 관리\n매매시마다 0.25% 수수료 발생";

        // 빠른 가이드 박스
        if (quickGuideTitleText != null)
            quickGuideTitleText.text = "🚀 빠른 시작 가이드";
        if (quickStepsText != null)
            quickStepsText.text = "1단계: 뉴스 확인 → 여러 섹터 분산투자\n2단계: 이벤트 전에 해당 섹터 매수\n3단계: 이벤트 후 높은 가격에 매도\n4단계: 5초 스탑은 정말 급할 때만!\n5단계: 마지막 턴에 모든 주식 현금화";
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 가이드 열려있는지 확인
    /// </summary>
    public bool IsGuideOpen => isGuideOpen;

    /// <summary>
    /// 특정 박스만 업데이트 (성능 최적화용)
    /// </summary>
    public void UpdateSection(string sectionName)
    {
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            SetDefaultTexts();
            return;
        }

        switch (sectionName.ToLower())
        {
            case "gameinfo": SetGameInfoTexts(); break;
            case "controls": SetControlsTexts(); break;
            case "features": SetFeaturesTexts(); break;
            case "tips": SetTipsTexts(); break;
            case "quickguide": SetQuickGuideTexts(); break;
            default: UpdateAllTexts(); break;
        }
    }

    #endregion

    #region 디버그 메서드

    [ContextMenu("가이드 열기")]
    void DebugOpenGuide() => OpenGuide();

    [ContextMenu("가이드 닫기")]
    void DebugCloseGuide() => CloseGuide();

    [ContextMenu("텍스트 업데이트")]
    void DebugUpdateTexts() => UpdateAllTexts();

    [ContextMenu("언어 토글")]
    void DebugToggleLanguage()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.ToggleLanguage();
        }
    }

    #endregion
}