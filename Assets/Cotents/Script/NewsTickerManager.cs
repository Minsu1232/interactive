using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 뉴스 티커 시스템 - 상단에 스크롤되는 뉴스 표시
/// GameManager와 연동하여 이벤트 예고 및 실시간 뉴스 표시 (로컬라이징 완전 지원)
/// </summary>
public class NewsTickerManager : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject newsTickerPanel;           // 뉴스 티커 전체 패널
    public TextMeshProUGUI newsLabelText;        // "속보", "뉴스" 라벨
    public TextMeshProUGUI newsContentText;      // 실제 뉴스 내용
    public RectTransform scrollingContent;       // 스크롤되는 콘텐츠 RectTransform
    public Image tickerBackground;               // 티커 배경 이미지

    [Header("애니메이션 설정")]
    [Range(50f, 500f)]
    public float scrollSpeed = 200f;              // 스크롤 속도 (픽셀/초)
    public float scrollResetDelay = 3f;          // 스크롤 완료 후 대기 시간
    public float newsDisplayDuration = 12f;      // 뉴스 총 표시 시간 (초)
    public bool enableAutoScroll = true;         // 자동 스크롤 활성화
    public bool enableInfiniteLoop = false;      // 무한 반복 활성화 (기본값: false)

    [Header("스타일 설정")]
    public Color normalNewsColor = Color.white;     // 일반 뉴스 색상
    public Color breakingNewsColor = Color.yellow;  // 속보 색상
    public Color previewNewsColor = Color.cyan;     // 예고 뉴스 색상

    [Header("배경 그라데이션")]
    public Color normalBgColor = Color.blue;        // 일반 배경
    public Color breakingBgColor = Color.red;       // 속보 배경
    public Color previewBgColor = new Color(0.98f, 0.42f, 0.39f);     // 예고 배경

    [Header("디버그")]
    public bool enableDebugLog = true;
    public bool showTestNews = false;            // 테스트 뉴스 표시

    // 뉴스 데이터
    private Queue<NewsData> newsQueue = new Queue<NewsData>();
    private NewsData currentNews;
    private Coroutine scrollCoroutine;
    private bool isScrolling = false;

    // 싱글톤 패턴
    private static NewsTickerManager instance;
    public static NewsTickerManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<NewsTickerManager>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeNewsTicker();
        StartCoroutine(SubscribeToGameManagerEvents());

        if (showTestNews)
        {
            ShowTestNews();
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= OnTurnChanged;
        }
    }

    /// <summary>
    /// 뉴스 티커 초기화
    /// </summary>
    void InitializeNewsTicker()
    {
        if (newsTickerPanel != null)
        {
            newsTickerPanel.SetActive(false);
        }

        if (scrollingContent != null)
        {
            ResetScrollPosition();
        }

        if (enableDebugLog)
            Debug.Log("📺 뉴스 티커 시스템 초기화 완료");
    }

    /// <summary>
    /// GameManager 이벤트 구독
    /// </summary>
    IEnumerator SubscribeToGameManagerEvents()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        GameManager.Instance.OnTurnChanged += OnTurnChanged;

        if (enableDebugLog)
            Debug.Log("🔗 뉴스 티커: GameManager 이벤트 구독 완료");
    }

    /// <summary>
    /// 턴 변경시 호출 - 이벤트 예고 체크
    /// </summary>
    void OnTurnChanged(int newTurn)
    {
        CheckForNewsPreview(newTurn);

        if (enableDebugLog)
            Debug.Log($"📺 뉴스 티커: 턴 {newTurn} - 예고 뉴스 체크");
    }

    /// <summary>
    /// 다음 턴 이벤트 예고 체크 (수정됨)
    /// </summary>
    void CheckForNewsPreview(int currentTurn)
    {
        int nextTurn = currentTurn + 1;

        if (GameManager.Instance != null)
        {
            var scheduledEvents = GameManager.Instance.GetScheduledEvents();

            if (scheduledEvents.ContainsKey(nextTurn))
            {
                var nextEvent = scheduledEvents[nextTurn];
                ShowPreviewNews(nextEvent, nextTurn);

                if (enableDebugLog)
                    Debug.Log($"📰 예고 뉴스 표시: {nextEvent.eventKey} (턴 {nextTurn})");
            }
        }
    }
    /// <summary>
    /// ✅ 완전히 개선된 로컬라이징된 이벤트 뉴스 표시 (GameManager에서 호출)
    /// </summary>
    public void ShowLocalizedEventNews(LocalizedTurnEvent localizedEvent)
    {
        NewsData eventNews = new NewsData
        {
            type = NewsType.Breaking,
            content = localizedEvent.newsContent,  // ✅ 이미 로컬라이징됨
            relatedEvent = null,                   // LocalizedTurnEvent용이므로 null
            localizedEvent = localizedEvent        // ✅ 로컬라이징된 이벤트 보관
        };

        ShowNews(eventNews);

        if (enableDebugLog)
            Debug.Log($"📺 로컬라이징된 이벤트 뉴스 표시: {localizedEvent.title}");
    }
    /// <summary>
    /// 예고 뉴스 표시
    /// </summary>
    void ShowPreviewNews(TurnEvent turnEvent, int targetTurn)
    {
        // GameManager에서 로컬라이징된 이벤트 가져오기
        var localizedEvent = GameManager.Instance?.GetLocalizedTurnEvent(turnEvent);

        if (localizedEvent == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 로컬라이징된 이벤트를 가져올 수 없음 - 스킵");
            return;
        }

        NewsData previewNews = new NewsData
        {
            type = NewsType.Preview,
            content = localizedEvent.previewContent,  // ✅ 이미 로컬라이징됨
            relatedEvent = turnEvent,
            localizedEvent = localizedEvent
        };

        ShowNews(previewNews);

        if (enableDebugLog)
            Debug.Log($"📰 로컬라이징된 예고 뉴스 표시: {localizedEvent.title} (턴 {targetTurn})");
    }

    /// <summary>
    /// 실제 이벤트 뉴스 표시 (GameManager에서 호출)
    /// </summary>
    public void ShowEventNews(TurnEvent turnEvent)
    {
        // GameManager에서 로컬라이징된 이벤트 가져오기
        var localizedEvent = GameManager.Instance?.GetLocalizedTurnEvent(turnEvent);

        if (localizedEvent != null)
        {
            // 로컬라이징된 버전 사용
            ShowLocalizedEventNews(localizedEvent);
        }
        else
        {
            // 폴백: 기존 방식
            NewsData eventNews = CreateEventNews(turnEvent);
            ShowNews(eventNews);
        }

        if (enableDebugLog)
            Debug.Log($"📺 이벤트 뉴스 표시: {localizedEvent?.title ?? turnEvent.Title}");
    }
    /// <summary>
    /// ✅ 언어 변경시 현재 뉴스 새로고침 - 완전히 개선
    /// </summary>
    public void RefreshCurrentLanguage()
    {
        if (currentNews == null) return;

        // 로컬라이징된 이벤트가 있으면 새로 가져오기
        if (currentNews.localizedEvent != null)
        {
            // eventKey로 다시 로컬라이징
            var turnEvent = FindTurnEventByKey(currentNews.localizedEvent.eventKey);
            if (turnEvent != null && GameManager.Instance != null)
            {
                var refreshedEvent = GameManager.Instance.GetLocalizedTurnEvent(turnEvent);

                // 현재 뉴스 타입에 따라 적절한 내용 사용
                string newContent = currentNews.type switch
                {
                    NewsType.Breaking => refreshedEvent.newsContent,
                    NewsType.Preview => refreshedEvent.previewContent,
                    _ => refreshedEvent.newsContent
                };

                if (newsContentText != null)
                {
                    newsContentText.text = newContent;
                }

                if (enableDebugLog)
                    Debug.Log($"🌍 뉴스 내용 새로고침: {refreshedEvent.title}");
            }
        }
        // 기존 TurnEvent가 있으면 기존 방식으로 처리
        else if (currentNews.relatedEvent != null)
        {
            string newContent = GetLocalizedEventNews(currentNews.relatedEvent);

            if (newsContentText != null)
            {
                newsContentText.text = newContent;
            }
        }

        // 뉴스 라벨도 새로고침
        if (newsLabelText != null)
        {
            newsLabelText.text = GetNewsLabel(currentNews.type);
        }

        if (enableDebugLog)
            Debug.Log("🌍 뉴스 티커 언어 새로고침 완료");
    }
    TurnEvent FindTurnEventByKey(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey) || GameManager.Instance == null)
            return null;

        var scheduledEvents = GameManager.Instance.GetScheduledEvents();

        foreach (var kvp in scheduledEvents)
        {
            if (kvp.Value.eventKey == eventKey)
            {
                return kvp.Value;
            }
        }

        return null;
    }
    /// <summary>
    /// 뉴스 표시 메인 메서드
    /// </summary>
    public void ShowNews(NewsData newsData)
    {
        if (newsData == null) return;

        if (isScrolling)
        {
            newsQueue.Enqueue(newsData);
            return;
        }

        DisplayNews(newsData);
    }

    /// <summary>
    /// 뉴스 UI 표시 및 애니메이션 시작
    /// </summary>
    void DisplayNews(NewsData newsData)
    {
        currentNews = newsData;
        UpdateNewsUI(newsData);

        if (newsTickerPanel != null)
        {
            newsTickerPanel.SetActive(true);
        }

        if (enableAutoScroll && scrollCoroutine == null)
        {
            scrollCoroutine = StartCoroutine(ScrollNewsContent());
        }

        if (enableDebugLog)
            Debug.Log($"📺 뉴스 표시: {newsData.type} - {newsData.content}");
    }

    /// <summary>
    /// 뉴스 UI 업데이트 (텍스트, 색상, 배경)
    /// </summary>
    void UpdateNewsUI(NewsData newsData)
    {
        // 라벨 업데이트
        if (newsLabelText != null)
        {
            newsLabelText.text = GetNewsLabel(newsData.type);
            newsLabelText.color = GetNewsLabelColor(newsData.type);
        }

        // 뉴스 내용 업데이트
        if (newsContentText != null)
        {
            newsContentText.text = newsData.content;
            newsContentText.color = GetNewsTextColor(newsData.type);
        }

        // 배경 색상 업데이트
        if (tickerBackground != null)
        {
            tickerBackground.color = GetBackgroundColor(newsData.type);
        }
    }

    /// <summary>
    /// 스크롤 애니메이션 코루틴
    /// </summary>
    IEnumerator ScrollNewsContent()
    {

        isScrolling = true;

        if (scrollingContent == null)
        {
            yield return new WaitForSecondsRealtime(3f); // 🔧 수정: unscaled 시간 사용
            isScrolling = false;
            yield break;
        }

        ResetScrollPosition();

        float contentWidth = scrollingContent.rect.width;
        float parentWidth = scrollingContent.parent.GetComponent<RectTransform>().rect.width;
        float totalScrollDistance = contentWidth + parentWidth;

        float newsStartTime = Time.realtimeSinceStartup; // 🔧 수정: unscaled 시간 사용
        float scrolledDistance = 0f;
        int repeatCount = 0;
        const int maxRepeats = 2;

        while (isScrolling && currentNews != null)
        {
            if (Time.realtimeSinceStartup - newsStartTime > newsDisplayDuration) // 🔧 수정
            {
                if (enableDebugLog)
                    Debug.Log("📺 뉴스 표시 시간 초과로 종료");
                break;
            }

            // 🔧 수정: unscaledDeltaTime 사용으로 일시정지에 영향받지 않음
            float deltaMove = scrollSpeed * Time.unscaledDeltaTime;
            scrollingContent.anchoredPosition += Vector2.left * deltaMove;
            scrolledDistance += deltaMove;

            if (scrolledDistance >= totalScrollDistance)
            {
                repeatCount++;

                if (!enableInfiniteLoop || repeatCount >= maxRepeats)
                {
                    if (enableDebugLog)
                        Debug.Log($"📺 뉴스 스크롤 완료 (반복횟수: {repeatCount})");
                    break;
                }

                if (newsQueue.Count > 0)
                {
                    if (enableDebugLog)
                        Debug.Log("📺 다음 뉴스로 교체");
                    break;
                }

                ResetScrollPosition();
                scrolledDistance = 0f;
                yield return new WaitForSecondsRealtime(scrollResetDelay); // 🔧 수정
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f); // 🔧 수정

        if (newsQueue.Count > 0)
        {
            NewsData nextNews = newsQueue.Dequeue();
            DisplayNews(nextNews);
        }
        else
        {
            if (newsTickerPanel != null)
            {
                newsTickerPanel.SetActive(false);
            }
            isScrolling = false;

            if (enableDebugLog)
                Debug.Log("📺 뉴스 티커 종료");
        }

        scrollCoroutine = null;
    }
    // 🆕 추가: 현재 뉴스 강제 종료 메서드
    /// <summary>
    /// 현재 뉴스를 즉시 종료하고 다음 뉴스로 넘어감 (턴 변경시 호출)
    /// </summary>
    public void ForceEndCurrentNews()
    {
        if (!isScrolling || currentNews == null) return;

        isScrolling = false;

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        if (newsTickerPanel != null)
        {
            newsTickerPanel.SetActive(false);
        }

        // 큐에 있는 뉴스가 있으면 다음 뉴스 표시
        if (newsQueue.Count > 0)
        {
            NewsData nextNews = newsQueue.Dequeue();
            DisplayNews(nextNews);
        }

        if (enableDebugLog)
            Debug.Log("📺 현재 뉴스 강제 종료됨");
    }
    /// <summary>
    /// 스크롤 위치 초기화
    /// </summary>
    void ResetScrollPosition()
    {
        if (scrollingContent != null && scrollingContent.parent != null)
        {
            RectTransform parentRect = scrollingContent.parent.GetComponent<RectTransform>();

            if (parentRect != null)
            {
                Vector2 pos = scrollingContent.anchoredPosition;
                pos.x = parentRect.rect.width;
                scrollingContent.anchoredPosition = pos;
            }
        }
    }

    #region 뉴스 데이터 생성 (로컬라이징 지원)

    /// <summary>
    /// 예고 뉴스 생성 (로컬라이징)
    /// </summary>
    NewsData CreatePreviewNews(TurnEvent turnEvent, int targetTurn)
    {
        string previewContent = GeneratePreviewContent(turnEvent, targetTurn);

        return new NewsData
        {
            type = NewsType.Preview,
            content = previewContent,
            relatedEvent = turnEvent
        };
    }

    /// <summary>
    /// 이벤트 뉴스 생성 (로컬라이징)
    /// </summary>
    NewsData CreateEventNews(TurnEvent turnEvent)
    {
        string eventContent = GetLocalizedEventNews(turnEvent);

        return new NewsData
        {
            type = NewsType.Breaking,
            content = eventContent,
            relatedEvent = turnEvent
        };
    }

    /// <summary>
    /// 이벤트별 로컬라이징된 뉴스 내용 가져오기 (완전 수정)
    /// </summary>
    string GetLocalizedEventNews(TurnEvent turnEvent)
    {
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ CSVLocalizationManager가 준비되지 않음 - 폴백 사용");
            return turnEvent.description;
        }

        var locManager = CSVLocalizationManager.Instance;

        // 이벤트 제목에 따른 정확한 로컬라이징
        if (turnEvent.title.Contains("AI") || turnEvent.title.Contains("기술혁신") || turnEvent.title.Contains("기술"))
        {
            string localizedNews = locManager.GetLocalizedText("news_event_ai_innovation");
            if (enableDebugLog)
                Debug.Log($"🌍 AI 이벤트 로컬라이징: {localizedNews}");
            return localizedNews;
        }
        else if (turnEvent.title.Contains("에너지") || turnEvent.title.Contains("Energy"))
        {
            string localizedNews = locManager.GetLocalizedText("news_event_energy_policy");
            if (enableDebugLog)
                Debug.Log($"🌍 에너지 이벤트 로컬라이징: {localizedNews}");
            return localizedNews;
        }
        else if (turnEvent.title.Contains("금리") || turnEvent.title.Contains("중앙은행") || turnEvent.title.Contains("Rate") || turnEvent.title.Contains("Bank"))
        {
            string localizedNews = locManager.GetLocalizedText("news_event_rate_hike");
            if (enableDebugLog)
                Debug.Log($"🌍 금리 이벤트 로컬라이징: {localizedNews}");
            return localizedNews;
        }
        else if (turnEvent.title.Contains("가상자산") || turnEvent.title.Contains("규제") || turnEvent.title.Contains("Crypto") || turnEvent.title.Contains("규제"))
        {
            string localizedNews = locManager.GetLocalizedText("news_event_crypto_regulation");
            if (enableDebugLog)
                Debug.Log($"🌍 가상자산 이벤트 로컬라이징: {localizedNews}");
            return localizedNews;
        }
        else if (turnEvent.title.Contains("경제") || turnEvent.title.Contains("불안") || turnEvent.title.Contains("글로벌") || turnEvent.title.Contains("Global"))
        {
            string localizedNews = locManager.GetLocalizedText("news_event_global_crisis");
            if (enableDebugLog)
                Debug.Log($"🌍 경제 이벤트 로컬라이징: {localizedNews}");
            return localizedNews;
        }

        // 기본값 (원본 설명)
        if (enableDebugLog)
            Debug.Log($"🌍 매칭되지 않은 이벤트 - 원본 사용: {turnEvent.title}");
        return turnEvent.description;
    }

    /// <summary>
    /// 예고 콘텐츠 생성 (로컬라이징)
    /// </summary>
    string GeneratePreviewContent(TurnEvent turnEvent, int targetTurn)
    {
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized)
        {
            return "Important announcement scheduled for tomorrow...";
        }

        var locManager = CSVLocalizationManager.Instance;

        // ✅ 키 기반 매핑 (Contains 제거!)
        if (!string.IsNullOrEmpty(turnEvent.previewKey))
        {
            return locManager.GetLocalizedText(turnEvent.previewKey);
        }

        // ✅ eventKey 기반 폴백
        if (!string.IsNullOrEmpty(turnEvent.eventKey))
        {
            string previewKey = turnEvent.eventKey switch
            {
                "ai_innovation" => "news_preview_tech",
                "energy_policy" => "news_preview_energy",
                "interest_rate" => "news_preview_monetary",
                "crypto_regulation" => "news_preview_crypto",
                _ => "news_preview_default"
            };

            return locManager.GetLocalizedText(previewKey);
        }

        return locManager.GetLocalizedText("news_preview_default");
    }

    #endregion

    #region 스타일 관련 메서드

    /// <summary>
    /// 뉴스 타입에 따른 라벨 텍스트 (로컬라이징)
    /// </summary>
    string GetNewsLabel(NewsType type)
    {
        if (CSVLocalizationManager.Instance == null)
        {
            switch (type)
            {
                case NewsType.Breaking: return "📺 속보";
                case NewsType.Preview: return "📰 예고";
                case NewsType.Normal: return "📊 뉴스";
                default: return "📺 뉴스";
            }
        }

        switch (type)
        {
            case NewsType.Breaking:
                return CSVLocalizationManager.Instance.GetLocalizedText("news_label_breaking");
            case NewsType.Preview:
                return CSVLocalizationManager.Instance.GetLocalizedText("news_label_preview");
            case NewsType.Normal:
                return CSVLocalizationManager.Instance.GetLocalizedText("news_label_normal");
            default:
                return CSVLocalizationManager.Instance.GetLocalizedText("news_label_normal");
        }
    }

    /// <summary>
    /// 뉴스 타입에 따른 라벨 색상
    /// </summary>
    Color GetNewsLabelColor(NewsType type)
    {
        switch (type)
        {
            case NewsType.Breaking:
                return breakingNewsColor;
            case NewsType.Preview:
                return previewNewsColor;
            default:
                return normalNewsColor;
        }
    }

    /// <summary>
    /// 뉴스 타입에 따른 텍스트 색상
    /// </summary>
    Color GetNewsTextColor(NewsType type)
    {
        return Color.white;
    }

    /// <summary>
    /// 뉴스 타입에 따른 배경 색상
    /// </summary>
    Color GetBackgroundColor(NewsType type)
    {
        switch (type)
        {
            case NewsType.Breaking:
                return breakingBgColor;
            case NewsType.Preview:
                return previewBgColor;
            default:
                return normalBgColor;
        }
    }

    #endregion

    #region 외부 인터페이스

    /// <summary>
    /// 즉시 뉴스 표시 (외부 호출용)
    /// </summary>
    public void ShowCustomNews(string content, NewsType type = NewsType.Normal)
    {
        NewsData customNews = new NewsData
        {
            type = type,
            content = content
        };

        ShowNews(customNews);
    }

    /// <summary>
    /// 뉴스 티커 즉시 숨김
    /// </summary>
    public void HideNewsTicker()
    {
        isScrolling = false;

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        if (newsTickerPanel != null)
        {
            newsTickerPanel.SetActive(false);
        }

        newsQueue.Clear();
        currentNews = null;
    }

    /// <summary>
    /// 현재 뉴스 표시 여부
    /// </summary>
    public bool IsShowingNews => newsTickerPanel != null && newsTickerPanel.activeInHierarchy;

    #endregion

    #region 테스트 및 디버그

    /// <summary>
    /// 테스트 뉴스 표시
    /// </summary>
    void ShowTestNews()
    {
        StartCoroutine(TestNewsSequence());
    }

    IEnumerator TestNewsSequence()
    {
        yield return new WaitForSeconds(2f);

        ShowCustomNews("내일 주요 기술기업들 긴급 발표 예정... 업계 관계자들 주목 👀", NewsType.Preview);

        yield return new WaitForSeconds(8f);

        ShowCustomNews("🚀 AI 기술 혁신 발표! 메이저 기업들 혁신적 기술 공개", NewsType.Breaking);
    }

    [ContextMenu("테스트 로컬라이징 뉴스")]
    void TestLocalizedNews()
    {
        // 가짜 AI 이벤트로 테스트
        var testEvent = new TurnEvent
        {
            title = "AI 기술 혁신 발표!",
            description = "테스트 설명"
        };

        ShowEventNews(testEvent);
    }

    [ContextMenu("테스트 예고 뉴스")]
    void TestPreviewNewsContext()
    {
        var testEvent = new TurnEvent
        {
            title = "금리 정책 발표",
            description = "테스트 예고"
        };

        ShowPreviewNews(testEvent, 5);
    }

    #endregion
}

#region 데이터 구조

/// <summary>
/// ✅ 업데이트된 뉴스 데이터 구조 - LocalizedTurnEvent 지원 추가
/// </summary>
[System.Serializable]
public class NewsData
{
    public NewsType type;                       // 뉴스 타입
    public string content;                      // 뉴스 내용
    public TurnEvent relatedEvent;              // 관련 이벤트 (레거시)
    public LocalizedTurnEvent localizedEvent;   // ✅ 추가: 로컬라이징된 이벤트
}

/// <summary>
/// 뉴스 타입 열거형 (기존 유지)
/// </summary>
public enum NewsType
{
    Normal,     // 일반 뉴스
    Preview,    // 예고 뉴스
    Breaking    // 속보 뉴스
}


#endregion