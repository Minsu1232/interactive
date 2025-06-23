using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 게임 제어 버튼들을 관리하는 매니저
/// 턴 넘기기, 5초 스탑, 가이드 패널 온오프 기능
/// </summary>
public class GameButtonManager : MonoBehaviour
{
    [Header("게임 제어 버튼들")]
    [SerializeField] private Button nextTurnButton;        // 턴 넘기기 버튼
    [SerializeField] private Button pauseButton;           // 5초 스탑 버튼
    [SerializeField] private Button guideToggleButton;     // 가이드 패널 토글 버튼

    [Header("버튼 텍스트")]
    [SerializeField] private TextMeshProUGUI nextTurnButtonText;
    [SerializeField] private TextMeshProUGUI pauseButtonText;
    [SerializeField] private TextMeshProUGUI guideButtonText;

    [Header("가이드 패널")]
    [SerializeField] private GameRulesPanel gameRulesPanel; // 가이드 패널 참조

    [Header("스탑 기능 설정")]
    [SerializeField] private float stopDuration = 5f;       // 스탑 지속 시간 (5초)

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    // 스탑 기능 관련 변수들
    private bool hasUsedStopThisTurn = false;               // 현재 턴에서 스탑을 사용했는지
    private bool isGamePaused = false;                      // 현재 게임이 일시정지 상태인지
    private Coroutine pauseCoroutine;                       // 일시정지 코루틴

    // 싱글톤 패턴
    private static GameButtonManager instance;
    public static GameButtonManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GameButtonManager>();
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
        // 버튼 이벤트 설정
        SetupButtonEvents();

        // GameManager 이벤트 구독
        StartCoroutine(SubscribeToGameManagerEvents());

        // 초기 UI 상태 설정
        UpdateButtonStates();
    }

    void OnDestroy()
    {
        // GameManager 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= OnTurnChanged;
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    void SetupButtonEvents()
    {
        // 턴 넘기기 버튼
        if (nextTurnButton != null)
        {
            nextTurnButton.onClick.RemoveAllListeners();
            nextTurnButton.onClick.AddListener(OnNextTurnButtonClicked);
        }

        // 5초 스탑 버튼
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        // 가이드 토글 버튼
        if (guideToggleButton != null)
        {
            guideToggleButton.onClick.RemoveAllListeners();
            guideToggleButton.onClick.AddListener(OnGuideToggleButtonClicked);
        }

        if (enableDebugLog)
            Debug.Log("✅ GameButtonManager 버튼 이벤트 설정 완료");
    }

    /// <summary>
    /// GameManager 이벤트 구독
    /// </summary>
    System.Collections.IEnumerator SubscribeToGameManagerEvents()
    {
        // GameManager 초기화 대기
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        // 이벤트 구독
        GameManager.Instance.OnTurnChanged += OnTurnChanged;
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

        if (enableDebugLog)
            Debug.Log("🔗 GameButtonManager: GameManager 이벤트 구독 완료");
    }

    #region 버튼 클릭 이벤트들

    /// <summary>
    /// 턴 넘기기 버튼 클릭
    /// </summary>
    void OnNextTurnButtonClicked()
    {
        if (GameManager.Instance == null || !GameManager.Instance.CanSkipCurrentTurn)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ 현재 턴을 넘길 수 없습니다.");
            return;
        }

        // GameManager의 턴 스킵 메서드 호출
        GameManager.Instance.ForceEndCurrentTurn();

        if (enableDebugLog)
            Debug.Log("⏭️ 턴 넘기기 버튼 클릭 - GameManager를 통해 턴 강제 종료");
    }

    /// <summary>
    /// 5초 스탑 버튼 클릭
    /// </summary>
    void OnPauseButtonClicked()
    {
        // 이미 이번 턴에 사용했거나 게임이 진행 중이 아니면 무시
        if (hasUsedStopThisTurn || !GameManager.Instance.IsGameActive || isGamePaused)
        {
            if (enableDebugLog)
            {
                if (hasUsedStopThisTurn)
                    Debug.LogWarning("⚠️ 이번 턴에 이미 스탑을 사용했습니다.");
                else if (isGamePaused)
                    Debug.LogWarning("⚠️ 이미 일시정지 중입니다.");
                else
                    Debug.LogWarning("⚠️ 게임이 진행 중이 아닙니다.");
            }
            return;
        }

        // 5초 스탑 시작
        StartStopFunction();
    }

    /// <summary>
    /// 가이드 패널 토글 버튼 클릭
    /// </summary>
    void OnGuideToggleButtonClicked()
    {
        if (gameRulesPanel == null)
        {
            if (enableDebugLog)
                Debug.LogWarning("⚠️ GameRulesPanel이 연결되지 않았습니다.");
            return;
        }

        // 가이드 패널 토글
        gameRulesPanel.TogglePanel();

        if (enableDebugLog)
            Debug.Log($"📖 가이드 패널 토글: {(gameRulesPanel.IsVisible ? "열림" : "닫힘")}");
    }

    #endregion

    #region 5초 스탑 기능

    /// <summary>
    /// 5초 스탑 기능 시작
    /// </summary>
    void StartStopFunction()
    {
        if (pauseCoroutine != null)
            StopCoroutine(pauseCoroutine);

        pauseCoroutine = StartCoroutine(StopCoroutine());
        hasUsedStopThisTurn = true;

        UpdateButtonStates();

        if (enableDebugLog)
            Debug.Log($"⏸️ {stopDuration}초 스탑 시작!");
    }

    /// <summary>
    /// 스탑 코루틴 (GameManager 연동 버전)
    /// </summary>
    System.Collections.IEnumerator StopCoroutine()
    {
        isGamePaused = true;

        // GameManager를 통해 게임 일시정지
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }

        // UI에 스탑 상태 표시
        ShowStopUI(true);

        // 실제 시간으로 5초 대기
        float elapsedTime = 0f;
        while (elapsedTime < stopDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            // 스탑 UI 업데이트 (남은 시간 표시)
            UpdateStopUI(stopDuration - elapsedTime);

            yield return null;
        }

        // GameManager를 통해 게임 재개
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }

        isGamePaused = false;

        // UI 복구
        ShowStopUI(false);
        UpdateButtonStates();

        if (enableDebugLog)
            Debug.Log("▶️ 스탑 종료 - 게임 재개");

        pauseCoroutine = null;
    }

    /// <summary>
    /// 스탑 UI 표시/숨기기
    /// </summary>
    void ShowStopUI(bool show)
    {
        // 스탑 상태를 시각적으로 표시하는 로직
        // 예: 화면에 "PAUSED" 텍스트나 오버레이 표시
        // 필요에 따라 구현

        if (pauseButtonText != null)
        {
            if (show)
            {
                // 스탑 중일 때 버튼 텍스트 변경
                pauseButtonText.text = "일시정지 중...";
                pauseButtonText.color = Color.red;
            }
            else
            {
                // 로컬라이징된 텍스트로 복구
                UpdateButtonTexts();
            }
        }
    }

    /// <summary>
    /// 스탑 UI 업데이트 (남은 시간 표시)
    /// </summary>
    void UpdateStopUI(float remainingTime)
    {
        if (pauseButtonText != null)
        {
            int seconds = Mathf.CeilToInt(remainingTime);
            pauseButtonText.text = $"일시정지 중... {seconds}초";
        }
    }

    #endregion

    #region GameManager 이벤트 처리

    /// <summary>
    /// 턴 변경 이벤트 처리
    /// </summary>
    void OnTurnChanged(int newTurn)
    {
        // 새 턴이 시작되면 스탑 사용 가능 상태로 리셋
        hasUsedStopThisTurn = false;

        // 🔧 추가: 게임이 일시정지 상태라면 재개
        if (isGamePaused)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            isGamePaused = false;

            if (pauseCoroutine != null)
            {
                StopCoroutine(pauseCoroutine);
                pauseCoroutine = null;
            }

            ShowStopUI(false);
        }

        // 버튼 상태 업데이트
        UpdateButtonStates();

        if (enableDebugLog)
            Debug.Log($"🔄 턴 {newTurn} 시작 - 스탑 기능 리셋, 버튼 상태 업데이트");
    }

    /// <summary>
    /// 게임 상태 변경 이벤트 처리
    /// </summary>
    void OnGameStateChanged(GameState newState)
    {
        // 게임 상태에 따른 버튼 활성화/비활성화
        UpdateButtonStates();

        if (enableDebugLog)
            Debug.Log($"🎮 게임 상태 변경: {newState} - 버튼 상태 업데이트");
    }

    #endregion

    #region UI 업데이트

    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    void UpdateButtonStates()
    {
        bool isGameActive = GameManager.Instance != null && GameManager.Instance.IsGameActive;
        bool canSkipTurn = GameManager.Instance != null && GameManager.Instance.CanSkipCurrentTurn;

        // 🔧 수정: 게임이 일시정지되어도 턴 넘기기는 가능해야 함
        // 턴 넘기기 버튼 (일시정지 상태와 관계없이 스킵 가능하면 활성화)
        if (nextTurnButton != null)
        {
            nextTurnButton.interactable = canSkipTurn;
        }

        // 스탑 버튼 (이번 턴에 사용했거나 게임이 일시정지 중이면 비활성화)
        if (pauseButton != null)
        {
            pauseButton.interactable = isGameActive &&
                                       !hasUsedStopThisTurn &&
                                       !isGamePaused;
        }

        // 가이드 버튼 (항상 사용 가능)
        if (guideToggleButton != null)
        {
            guideToggleButton.interactable = true;
        }

        // 버튼 텍스트 업데이트
        UpdateButtonTexts();
    }

    /// <summary>
    /// 버튼 텍스트 업데이트 (로컬라이징 지원)
    /// </summary>
    void UpdateButtonTexts()
    {
        if (CSVLocalizationManager.Instance == null) return;

        var locManager = CSVLocalizationManager.Instance;

        // 턴 넘기기 버튼
        if (nextTurnButtonText != null)
        {
            nextTurnButtonText.text = locManager.GetLocalizedText("button_next_turn");
        }

        // 스탑 버튼
        if (pauseButtonText != null && !isGamePaused)
        {
            if (hasUsedStopThisTurn)
            {
                pauseButtonText.text = locManager.GetLocalizedText("button_stop_used");
                pauseButtonText.color = Color.gray;
            }
            else
            {
                pauseButtonText.text = locManager.GetLocalizedText("button_stop");
                pauseButtonText.color = Color.white;
            }
        }

        // 가이드 버튼
        if (guideButtonText != null)
        {
            guideButtonText.text = locManager.GetLocalizedText("button_guide");
        }
    }

    #endregion

    #region 공개 메서드들

    /// <summary>
    /// 외부에서 스탑 기능 사용 가능 여부 확인
    /// </summary>
    public bool CanUseStop => !hasUsedStopThisTurn && !isGamePaused &&
                               GameManager.Instance != null && GameManager.Instance.IsGameActive;

    /// <summary>
    /// 현재 게임이 일시정지 상태인지 확인
    /// </summary>
    public bool IsGamePaused => isGamePaused;

    /// <summary>
    /// 강제로 스탑 기능 리셋 (디버그용)
    /// </summary>
    [ContextMenu("스탑 기능 리셋")]
    public void ResetStopFunction()
    {
        hasUsedStopThisTurn = false;

        if (pauseCoroutine != null)
        {
            StopCoroutine(pauseCoroutine);
            pauseCoroutine = null;
        }

        Time.timeScale = 1f;
        isGamePaused = false;
        ShowStopUI(false);
        UpdateButtonStates();

        if (enableDebugLog)
            Debug.Log("🔧 스탑 기능 강제 리셋");
    }

    #endregion

    #region 디버그 메서드들

    [ContextMenu("턴 넘기기 테스트")]
    void DebugNextTurn()
    {
        OnNextTurnButtonClicked();
    }

    [ContextMenu("5초 스탑 테스트")]
    void DebugStopFunction()
    {
        OnPauseButtonClicked();
    }

    [ContextMenu("가이드 토글 테스트")]
    void DebugGuideToggle()
    {
        OnGuideToggleButtonClicked();
    }

    [ContextMenu("버튼 상태 출력")]
    void PrintButtonStates()
    {
        Debug.Log($"📊 GameButtonManager 상태:");
        Debug.Log($"  - 이번 턴 스탑 사용: {hasUsedStopThisTurn}");
        Debug.Log($"  - 게임 일시정지: {isGamePaused}");
        Debug.Log($"  - 게임 진행 중: {(GameManager.Instance?.IsGameActive ?? false)}");
        Debug.Log($"  - 게임 상태: {(GameManager.Instance?.CurrentState ?? GameState.WaitingToStart)}");
        Debug.Log($"  - 현재 턴: {(GameManager.Instance?.CurrentTurn ?? 0)}");
        Debug.Log($"  - 남은 시간: {(GameManager.Instance?.CurrentTurnRemainingTime ?? 0):F1}초");
        Debug.Log($"  - 스탑 사용 가능: {CanUseStop}");
        Debug.Log($"  - 턴 스킵 가능: {(GameManager.Instance?.CanSkipCurrentTurn ?? false)}");
        Debug.Log($"  - 턴 넘기기 버튼 활성화: {(nextTurnButton?.interactable ?? false)}");
        Debug.Log($"  - 스탑 버튼 활성화: {(pauseButton?.interactable ?? false)}");
    }

    #endregion
}