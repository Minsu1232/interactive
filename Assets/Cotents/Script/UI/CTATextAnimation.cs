using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// CTA �ؽ�Ʈ ���� �ִϸ��̼� (�հ��� �̸��� ����)
/// TextMeshPro�� �⺻ Text ��� ����
/// </summary>
public class CTATextAnimation : MonoBehaviour
{
    [Header("�ִϸ��̼� Ÿ��")]
    [SerializeField] private AnimationType animationType = AnimationType.PulseAndFloat;

    [Header("�ִϸ��̼� ����")]
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float floatHeight = 10f;
    [SerializeField] private float fadeMin = 0.6f;
    [SerializeField] private float shakeIntensity = 3f;

    [Header("�հ��� �̸��� ���� �ִϸ��̼�")]
    [SerializeField] private bool separateHandAnimation = true;
    [SerializeField] private Transform handEmojiTransform; // �հ��� �̸����� ���� �ִϸ��̼��Ϸ���

    public enum AnimationType
    {
        PulseOnly,           // ũ�⸸ ��ȭ (�ε巯��)
        FadeOnly,            // ���İ��� ��ȭ (������)
        PulseAndFade,        // ũ�� + ���İ� (�⺻��)
        PulseAndFloat,       // ũ�� + ���Ʒ� ������ (��õ!)
        FloatAndFade,        // ���Ʒ� + ���İ� (�ڿ�������)
        QuickPulse,          // ���� �޽� (�����)
        BreathingEffect,     // ������ ȿ�� (������)
        Shake,               // ���� ȿ�� (�ָ�!)
        TypewriterBlink,     // �����̴� Ŀ�� ȿ�� (��Ư��)
        WaveMotion          // �ĵ� ������ (�ε巯��)
    }

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private TextMeshProUGUI tmpText;
    private Text legacyText;
    private bool isTextMeshPro;

    void Start()
    {
        // ������Ʈ Ÿ�� Ȯ��
        tmpText = GetComponent<TextMeshProUGUI>();
        legacyText = GetComponent<Text>();
        isTextMeshPro = (tmpText != null);

        // �ʱⰪ ����
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        originalColor = isTextMeshPro ? tmpText.color : legacyText.color;

        // �ִϸ��̼� ����
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

    // 1. ũ�⸸ ��ȭ (�ε巯�� �޽�)
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

    // 2. ���İ��� ��ȭ
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

    // 3. ũ�� + ���İ�
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

    // 4. ũ�� + ���Ʒ� ������ (��õ!)
    IEnumerator PulseAndFloatAnimation()
    {
        while (true)
        {
            float time = Time.time * animationSpeed;

            // �ε巯�� �޽�
            float pulseTime = Mathf.PingPong(time, 1f);
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.SmoothStep(0f, 1f, pulseTime));

            // ���Ʒ� �÷�Ʈ
            float floatOffset = Mathf.Sin(time * 0.7f) * floatHeight;

            transform.localScale = originalScale * scale;
            transform.localPosition = originalPosition + Vector3.up * floatOffset;
            yield return null;
        }
    }

    // 5. ���Ʒ� + ���İ�
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

    // 6. ���� �޽� (����� ǥ��)
    IEnumerator QuickPulseAnimation()
    {
        while (true)
        {
            float time = Mathf.PingPong(Time.time * (animationSpeed * 2.5f), 1f);
            float scale = Mathf.Lerp(1f, pulseScale * 1.3f, time * time); // �� �ް��� ��ȭ
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }

    // 7. ������ ȿ�� (�ſ� �ε巯��)
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

    // 8. ���� ȿ�� (�ָ�!)
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

            // ����ġ�� ��� ���ư���
            transform.localPosition = originalPosition;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 9. �����̴� Ŀ�� ȿ��
    IEnumerator TypewriterBlinkAnimation()
    {
        string originalText = GetTextContent();

        while (true)
        {
            // Ŀ�� �߰�
            SetTextContent(originalText + "|");
            yield return new WaitForSeconds(0.5f);

            // Ŀ�� ����
            SetTextContent(originalText);
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 10. �ĵ� ������
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

    // ���� �޼����
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

    // ��Ÿ�ӿ��� �ִϸ��̼� ����
    public void ChangeAnimation(AnimationType newType)
    {
        StopAllCoroutines();
        animationType = newType;

        // ���� ���·� ����
        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
        SetTextAlpha(originalColor.a);

        StartCoroutine(PlayAnimation());
    }
}