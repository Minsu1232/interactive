using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 배경에 떠다니는 장식 요소들을 관리하는 스크립트
/// 이모지 텍스트 기능 추가
/// </summary>
public class FloatingElements : MonoBehaviour
{
    [Header("장식 요소 설정")]
    [SerializeField] private GameObject decorationPrefab;
    [SerializeField] private int elementCount = 5;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatRange = 50f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("이모지 설정")]
    [SerializeField] private bool useEmojiText = true;
    [SerializeField] private string[] availableEmojis = { "📊", "🏛", "💵", "📰" };
    [SerializeField] private float emojiSize = 24f;
    [SerializeField] private TMP_FontAsset emojiFont; // 이모지 폰트 (선택사항)

    [Header("스폰 영역")]
    [SerializeField] private RectTransform canvasRect;

    private void Start()
    {
        // 장식 요소들 생성
        CreateFloatingElements();
    }

    // 장식 요소들 생성 메서드
    private void CreateFloatingElements()
    {
        for (int i = 0; i < elementCount; i++)
        {
            // 장식 요소 생성
            GameObject element = CreateDecorationElement();

            // 랜덤 위치에 배치
            Vector2 randomPos = GetRandomPosition();
            element.GetComponent<RectTransform>().anchoredPosition = randomPos;

            // 떠다니는 애니메이션 시작
            StartCoroutine(FloatAnimation(element, i * 0.5f));
        }
    }

    // 장식 요소 생성
    private GameObject CreateDecorationElement()
    {
        GameObject element;

        if (decorationPrefab != null)
        {
            element = Instantiate(decorationPrefab, transform);

            // 프리팹에 텍스트가 있다면 이모지 할당
            if (useEmojiText)
            {
                AssignRandomEmoji(element);
            }
        }
        else
        {
            // 기본 장식 요소 생성
            element = new GameObject("DecorationElement");
            element.transform.SetParent(transform);

            if (useEmojiText)
            {
                // 텍스트 컴포넌트로 이모지 생성
                CreateEmojiText(element);
            }
            else
            {
                // 기존 Image 방식
                CreateImageDecoration(element);
            }

            // RectTransform 설정
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Random.Range(60, 120), Random.Range(60, 120));
        }

        return element;
    }

    // 프리팹의 텍스트에 랜덤 이모지 할당
    private void AssignRandomEmoji(GameObject element)
    {
        // TextMeshProUGUI 찾기
        TextMeshProUGUI textComponent = element.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            // 없다면 TextMeshPro도 확인
            TextMeshPro textComponent3D = element.GetComponentInChildren<TextMeshPro>();
            if (textComponent3D != null)
            {
                string randomEmoji = GetRandomEmoji();
                textComponent3D.text = randomEmoji;
                textComponent3D.fontSize = emojiSize;

                if (emojiFont != null)
                    textComponent3D.font = emojiFont;
            }
        }
        else
        {
            string randomEmoji = GetRandomEmoji();
            textComponent.text = randomEmoji;
            textComponent.fontSize = emojiSize;

            if (emojiFont != null)
                textComponent.font = emojiFont;
        }
    }

    // 이모지 텍스트 생성
    private void CreateEmojiText(GameObject element)
    {
        // TextMeshProUGUI 컴포넌트 추가
        TextMeshProUGUI textComponent = element.AddComponent<TextMeshProUGUI>();

        // 랜덤 이모지 할당
        string randomEmoji = GetRandomEmoji();
        textComponent.text = randomEmoji;
        textComponent.fontSize = emojiSize;
        textComponent.color = new Color(1f, 1f, 1f, 0.6f); // 60% 투명도
        textComponent.alignment = TextAlignmentOptions.Center;

        // 폰트 설정
        if (emojiFont != null)
            textComponent.font = emojiFont;
    }

    // 기존 Image 방식 장식 생성
    private void CreateImageDecoration(GameObject element)
    {
        // Image 컴포넌트 추가
        Image img = element.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.1f); // 반투명 흰색

        // UIGradient 추가 (위에서 만든 스크립트)
        UIGradient gradient = element.AddComponent<UIGradient>();
        if (gradient != null)
        {
            // UIGradient 설정 (스크립트에 따라 다를 수 있음)
            var gradientType = gradient.GetType().GetField("gradientType");
            if (gradientType != null)
                gradientType.SetValue(gradient, 3); // Radial = 3

            var topColor = gradient.GetType().GetField("topColor");
            if (topColor != null)
                topColor.SetValue(gradient, new Color(1, 1, 1, 0.2f));

            var bottomColor = gradient.GetType().GetField("bottomColor");
            if (bottomColor != null)
                bottomColor.SetValue(gradient, new Color(1, 1, 1, 0.05f));
        }
    }

    // 랜덤 이모지 선택
    private string GetRandomEmoji()
    {
        if (availableEmojis.Length == 0) return "📊"; // 기본값

        int randomIndex = Random.Range(0, availableEmojis.Length);
        return availableEmojis[randomIndex];
    }

    // 랜덤 위치 반환
    private Vector2 GetRandomPosition()
    {
        if (canvasRect == null) canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        float x = Random.Range(-canvasRect.rect.width / 2, canvasRect.rect.width / 2);
        float y = Random.Range(-canvasRect.rect.height / 2, canvasRect.rect.height / 2);

        return new Vector2(x, y);
    }

    // 떠다니는 애니메이션
    private IEnumerator FloatAnimation(GameObject element, float delay)
    {
        yield return new WaitForSeconds(delay);

        RectTransform rect = element.GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;

        // 텍스트와 이미지 컴포넌트 가져오기
        TextMeshProUGUI textComponent = element.GetComponent<TextMeshProUGUI>();
        Image img = element.GetComponent<Image>();

        float time = 0f;

        while (element != null)
        {
            time += Time.deltaTime * floatSpeed;

            // 위아래 떠다니는 움직임
            float yOffset = Mathf.Sin(time) * floatRange;
            rect.anchoredPosition = startPos + new Vector2(0, yOffset);

            // 회전 효과
            float rotation = Mathf.Sin(time * 0.5f) * 30f;
            rect.rotation = Quaternion.Euler(0, 0, rotation);

            // 페이드 인/아웃 효과
            float alpha = (Mathf.Sin(time * fadeSpeed) + 1f) * 0.5f * 0.3f; // 최대 30% 투명도

            // 텍스트 투명도 조절
            if (textComponent != null)
            {
                Color textColor = textComponent.color;
                textColor.a = alpha;
                textComponent.color = textColor;
            }

            // 이미지 투명도 조절 (기존 방식)
            if (img != null)
            {
                Color color = img.color;
                color.a = alpha * 0.3f; // 이미지는 더 연하게
                img.color = color;
            }

            yield return null;
        }
    }
}