using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// CTA 텍스트 전용 애니메이션 (손가락 이모지 포함)
/// TextMeshPro와 기본 Text 모두 지원
/// </summary>
public class CTATextAnimation : MonoBehaviour
{
    [Header("애니메이션 타입")]
    [SerializeField] private AnimationType animationType = AnimationType.PulseAndFloat;

    [Header("애니메이션 설정")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float floatHeight = 10f;
    [SerializeField] private float fadeMin = 0.6f;
    [SerializeField] private float shakeIntensity = 3f;

    [Header("손가락 이모지 별도 애니메이션")]
    [SerializeField] private bool separateHandAnimation = true;
    [SerializeField] private Transform handEmojiTransform; // 손가락 이모지만 따로 애니메이션하려면

    public enum AnimationType
    {
        PulseOnly,           // 크기만 변화 (부드러움)
        FadeOnly,            // 알파값만 변화 (은은함)
        PulseAndFade,        // 크기 + 알파값 (기본적)
        PulseAndFloat,       // 크기 + 위아래 움직임 (추천!)
        FloatAndFade,        // 위아래 + 알파값 (자연스러움)
        QuickPulse,          // 빠른 펄스 (긴급함)
        BreathingEffect,     // 숨쉬는 효과 (차분함)
        Shake,               // 진동 효과 (주목!)
        TypewriterBlink,     // 깜빡이는 커서 효과 (독특함)
        WaveMotion          // 파도 움직임 (부드러움)
    }

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private TextMeshProUGUI tmpText;
    private Text legacyText;
    private bool isTextMeshPro;

    void Start()
    {
        // 컴포넌트 타입 확인
        tmpText = GetComponent<TextMeshProUGUI>();
        legacyText = GetComponent<Text>();
        isTextMeshPro = (tmpText != null);

        // 초기값 저장
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        originalColor = isTextMeshPro ? tmpText.color : legacyText.color;

        // 애니메이션 시작
        StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        switch (animationType)
        {
            case AnimationType.PulseOnly:
                yield return StartCoroutine(PulseOnlyAnimation());
                break;
            case AnimationType.FadeOnly:
                yield return StartCoroutine(FadeOnlyAnimation());
                break;
            case AnimationType.PulseAndFade:
                yield return StartCoroutine(PulseAndFadeAnimation());
                break;
            case AnimationType.PulseAndFloat:
                yield return StartCoroutine(PulseAndFloatAnimation());
                break;
            case AnimationType.FloatAndFade:
                yield return StartCoroutine(FloatAndFadeAnimation());
                break;
            case AnimationType.QuickPulse:
                yield return StartCoroutine(QuickPulseAnimation());
                break;
            case AnimationType.BreathingEffect:
                yield return StartCoroutine(BreathingAnimation());
                break;
            case AnimationType.Shake:
                yield return StartCoroutine(ShakeAnimation());
                break;
            case AnimationType.TypewriterBlink:
                yield return StartCoroutine(TypewriterBlinkAnimation());
                break;
            case AnimationType.WaveMotion:
                yield return StartCoroutine(WaveMotionAnimation());
                break;
        }
    }

    // 1. 크기만 변화 (부드러운 펄스)
    IEnumerator PulseOnlyAnimation()
    {
        while (true)
        {
            float time = Mathf.PingPong(Time.time * animationSpeed, 1f);
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.SmoothStep(0f, 1f, time));
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    // 2. 알파값만 변화
    IEnumerator FadeOnlyAnimation()
    {
        while (true)
        {
            float time = Mathf.PingPong(Time.time * animationSpeed, 1f);
            float alpha = Mathf.Lerp(fadeMin, 1f, time);
            SetTextAlpha(alpha);
            yield return null;
        }
    }

    // 3. 크기 + 알파값
    IEnumerator PulseAndFadeAnimation()
    {
        while (true)
        {
            float time = Mathf.PingPong(Time.time * animationSpeed, 1f);
            float smoothTime = Mathf.SmoothStep(0f, 1f, time);

            float scale = Mathf.Lerp(1f, pulseScale, smoothTime);
            float alpha = Mathf.Lerp(fadeMin, 1f, smoothTime);

            transform.localScale = originalScale * scale;
            SetTextAlpha(alpha);
            yield return null;
        }
    }

    // 4. 크기 + 위아래 움직임 (추천!)
    IEnumerator PulseAndFloatAnimation()
    {
        while (true)
        {
            float time = Time.time * animationSpeed;

            // 부드러운 펄스
            float pulseTime = Mathf.PingPong(time, 1f);
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.SmoothStep(0f, 1f, pulseTime));

            // 위아래 플로트
            float floatOffset = Mathf.Sin(time * 0.7f) * floatHeight;

            transform.localScale = originalScale * scale;
            transform.localPosition = originalPosition + Vector3.up * floatOffset;
            yield return null;
        }
    }

    // 5. 위아래 + 알파값
    IEnumerator FloatAndFadeAnimation()
    {
        while (true)
        {
            float time = Time.time * animationSpeed;

            float floatOffset = Mathf.Sin(time) * floatHeight;
            float alpha = Mathf.Lerp(fadeMin, 1f, (Mathf.Sin(time * 1.3f) + 1f) * 0.5f);

            transform.localPosition = originalPosition + Vector3.up * floatOffset;
            SetTextAlpha(alpha);
            yield return null;
        }
    }

    // 6. 빠른 펄스 (긴급함 표현)
    IEnumerator QuickPulseAnimation()
    {
        while (true)
        {
            float time = Mathf.PingPong(Time.time * (animationSpeed * 2.5f), 1f);
            float scale = Mathf.Lerp(1f, pulseScale * 1.3f, time * time); // 더 급격한 변화
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    // 7. 숨쉬는 효과 (매우 부드러움)
    IEnumerator BreathingAnimation()
    {
        while (true)
        {
            float time = Time.time * (animationSpeed * 0.5f);
            float breath = (Mathf.Sin(time) + 1f) * 0.5f;

            float scale = Mathf.Lerp(0.95f, 1.05f, breath);
            float alpha = Mathf.Lerp(0.8f, 1f, breath);

            transform.localScale = originalScale * scale;
            SetTextAlpha(alpha);
            yield return null;
        }
    }

    // 8. 진동 효과 (주목!)
    IEnumerator ShakeAnimation()
    {
        while (true)
        {
            Vector3 shake = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity, shakeIntensity),
                0
            );

            transform.localPosition = originalPosition + shake;

            yield return new WaitForSeconds(0.05f);

            // 원위치로 잠깐 돌아가기
            transform.localPosition = originalPosition;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 9. 깜빡이는 커서 효과
    IEnumerator TypewriterBlinkAnimation()
    {
        string originalText = GetTextContent();

        while (true)
        {
            // 커서 추가
            SetTextContent(originalText + "|");
            yield return new WaitForSeconds(0.5f);

            // 커서 제거
            SetTextContent(originalText);
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 10. 파도 움직임
    IEnumerator WaveMotionAnimation()
    {
        while (true)
        {
            float time = Time.time * animationSpeed;

            float waveX = Mathf.Sin(time) * floatHeight * 0.5f;
            float waveY = Mathf.Sin(time * 1.3f) * floatHeight;
            float scale = 1f + Mathf.Sin(time * 0.7f) * 0.1f;

            transform.localPosition = originalPosition + new Vector3(waveX, waveY, 0);
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    // 헬퍼 메서드들
    void SetTextAlpha(float alpha)
    {
        Color newColor = originalColor;
        newColor.a = alpha;

        if (isTextMeshPro)
            tmpText.color = newColor;
        else
            legacyText.color = newColor;
    }

    string GetTextContent()
    {
        return isTextMeshPro ? tmpText.text : legacyText.text;
    }

    void SetTextContent(string text)
    {
        if (isTextMeshPro)
            tmpText.text = text;
        else
            legacyText.text = text;
    }

    // 런타임에서 애니메이션 변경
    public void ChangeAnimation(AnimationType newType)
    {
        StopAllCoroutines();
        animationType = newType;

        // 원래 상태로 복원
        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
        SetTextAlpha(originalColor.a);

        StartCoroutine(PlayAnimation());
    }
}