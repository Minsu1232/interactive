using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// StartScene의 메인 화면 관리 - 머니 레인 효과 (최적화됨)
/// </summary>
public class StartSceneManager : MonoBehaviour
{
    [Header("메인 UI 컴포넌트")]
    [SerializeField] private Button experienceCardButton;    // 💰 3분 투자 체험 카드

    [Header("게임 룰 패널")]
    [SerializeField] private GameRulesPanel gameRulesPanel;  // 게임 룰 패널 스크립트

    [Header("씬 전환 설정")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("머니 레인 설정")]
    [SerializeField] private Canvas mainCanvas;              // 메인 캔버스
    [SerializeField] private int moneyTextCount = 3;         // 생성할 텍스트 개수 (3개면 충분)
    [SerializeField] private float fallDuration = 3f;       // 떨어지는 시간
    [SerializeField] private float delayBetweenTexts = 0.3f; // 텍스트간 딜레이
    [SerializeField] private GameObject panel;                // 패널 오브젝트 (씬 전환용)
    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    void Start()
    {
        SetupEvents();
    }

    /// <summary>
    /// 이벤트 설정
    /// </summary>
    void SetupEvents()
    {
        // 💰 투자 체험 카드 버튼
        if (experienceCardButton != null)
        {
            experienceCardButton.onClick.AddListener(OnExperienceCardClicked);
        }

        // 게임 룰 패널의 게임 시작 이벤트 구독
        if (gameRulesPanel != null)
        {
            gameRulesPanel.OnGameStart += OnGameStart;
        }

        if (enableDebugLog)
            Debug.Log("✅ StartSceneManager 이벤트 설정 완료");
    }

    /// <summary>
    /// 💰 투자 체험 카드 클릭 - 룰 패널 열기
    /// </summary>
    void OnExperienceCardClicked()
    {
        if (enableDebugLog)
            Debug.Log("💰 투자 체험 카드 클릭 → 룰 패널 열기");

        if (gameRulesPanel != null)
        {
            gameRulesPanel.ShowPanel();
        }
    }

    /// <summary>
    /// 룰 패널에서 게임 시작 버튼 클릭 - 머니 레인 효과
    /// </summary>
    void OnGameStart()
    {// 패널의 RectTransform을 움직임

        RectTransform panelRect = panel.GetComponent<RectTransform>();

        // 중심점 계산

        Vector3 centerPoint = Vector3.zero; // 또는 특정 위치

        Sequence suctionSequence = DOTween.Sequence();

        suctionSequence.Append(panelRect.DOAnchorPos(centerPoint, 1f).SetEase(Ease.InBack))

                       .Join(panelRect.DOScale(0.1f, 1f).SetEase(Ease.InCirc))                       

                       .OnComplete(() => {

                           panel.SetActive(false); // 패널 비활성화

                       });
        if (enableDebugLog)
            Debug.Log("💰 머니 레인 시작!");

        StartCoroutine(MoneyRainAndLoadScene());
    }

    /// <summary>
    /// 머니 레인 효과 + 씬 전환
    /// </summary>
    IEnumerator MoneyRainAndLoadScene()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }

        // 머니 텍스트들 순차적으로 떨어뜨리기
        for (int i = 0; i < moneyTextCount; i++)
        {
            CreateFallingMoneyText(i);
            yield return new WaitForSeconds(delayBetweenTexts);
        }

        // 떨어지는 걸 잠깐 구경한 후 씬 전환
        yield return new WaitForSeconds(fallDuration);
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 이모지 가득한 텍스트 하나 생성해서 떨어뜨리기
    /// </summary>
    void CreateFallingMoneyText(int index)
    {
        // 텍스트 오브젝트 생성
        GameObject moneyTextObj = new GameObject($"MoneyRain_{index}");
        moneyTextObj.transform.SetParent(mainCanvas.transform, false);

        // TextMeshPro 컴포넌트 추가
        TextMeshProUGUI moneyText = moneyTextObj.AddComponent<TextMeshProUGUI>();

        // 이모지 가득한 텍스트 생성
        moneyText.text = GenerateMoneyRainText();
        moneyText.fontSize = Random.Range(40f, 60f);
        moneyText.color = GetRandomMoneyColor();
        moneyText.alignment = TextAlignmentOptions.Center;
        moneyText.raycastTarget = false; // 클릭 방지

        // RectTransform 설정
        RectTransform rectTransform = moneyText.rectTransform;
        rectTransform.sizeDelta = new Vector2(Screen.width * 0.8f, Screen.height * 1.5f); // 넉넉한 크기
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 시작 위치 (화면 위쪽, 좌우는 랜덤)
        float startX = Random.Range(-Screen.width * 0.3f, Screen.width * 0.3f);
        rectTransform.anchoredPosition = new Vector2(startX, Screen.height + 200);

        // 떨어지는 애니메이션
        Vector2 endPosition = new Vector2(
            startX + Random.Range(-100f, 100f), // 살짝 좌우로 흔들리며
            -Screen.height - 200
        );

        rectTransform.DOAnchorPos(endPosition, fallDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => {
                if (moneyTextObj != null)
                    Destroy(moneyTextObj);
            });

        // 살짝 회전하며 떨어지기
        float randomRotation = Random.Range(-90f, 90f);
        rectTransform.DORotate(new Vector3(0, 0, randomRotation), fallDuration, RotateMode.FastBeyond360);

        if (enableDebugLog)
            Debug.Log($"💸 머니 텍스트 {index + 1} 생성 완료");
    }

    /// <summary>
    /// 이모지 가득한 멀티라인 텍스트 생성
    /// </summary>
    string GenerateMoneyRainText()
    {
        string[] moneyEmojis = { "💰", "💵", "💸", "🤑", "💴", "💶", "💷", "🪙", "💲", "🏦" };

        string result = "";
        int lines = Random.Range(8, 12); // 8-12줄

        for (int line = 0; line < lines; line++)
        {
            int emojisPerLine = Random.Range(5, 8); // 한 줄에 5-8개

            for (int i = 0; i < emojisPerLine; i++)
            {
                // 랜덤 이모지 선택
                string emoji = moneyEmojis[Random.Range(0, moneyEmojis.Length)];
                result += emoji;

                // 가끔 공백 추가 (자연스러운 배치)
                if (Random.Range(0f, 1f) < 0.3f)
                {
                    result += " ";
                }
            }

            // 줄바꿈 (마지막 줄이 아니면)
            if (line < lines - 1)
            {
                result += "\n";
            }
        }

        return result;
    }

    /// <summary>
    /// 랜덤 머니 컬러
    /// </summary>
    Color GetRandomMoneyColor()
    {
        Color[] colors = {
            new Color(1f, 0.84f, 0f),     // 금색
            new Color(0f, 1f, 0.5f),      // 연두색 (달러)
            new Color(1f, 1f, 0.3f),      // 밝은 노란색
            new Color(0.9f, 0.9f, 0.9f)   // 밝은 회색
        };
        return colors[Random.Range(0, colors.Length)];
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gameRulesPanel != null)
        {
            gameRulesPanel.OnGameStart -= OnGameStart;
        }

        // DOTween 정리
        DOTween.KillAll();
    }
}