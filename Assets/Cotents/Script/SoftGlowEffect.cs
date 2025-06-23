using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoftGlowEffect : MonoBehaviour
{
    [Header("���� �ؽ�Ʈ")]
    public TextMeshProUGUI mainText;

    [Header("�۷ο� ����")]
    public int glowCopyCount = 4;           // �۷ο� ���纻 ���� (�� �������� �ε巯��)
    public float maxGlowDistance = 20f;     // �ִ� �۷ο� �Ÿ�
    public Color glowColor = new Color(0.4f, 0.8f, 1f, 0.6f);
    public bool animateGlow = true;         // �۷ο� �ִϸ��̼� ����
    public float pulseSpeed = 2f;           // �޽� �ӵ�

    private TextMeshProUGUI[] glowCopies;
    private RectTransform mainRect;

    void Start()
    {
        SetupSoftGlow();
    }

    /// <summary>
    /// �ε巯�� �۷ο� ȿ�� ����
    /// </summary>
    void SetupSoftGlow()
    {
        if (mainText == null) return;

        mainRect = mainText.rectTransform;
        glowCopies = new TextMeshProUGUI[glowCopyCount];

        // �۷ο� ���̾�� ����
        for (int i = 0; i < glowCopyCount; i++)
        {
            CreateGlowLayer(i);
        }

        // ���� �ؽ�Ʈ�� �ֻ�����
        mainText.transform.SetAsLastSibling();
    }

    /// <summary>
    /// ���� �۷ο� ���̾� ����
    /// </summary>
    void CreateGlowLayer(int layerIndex)
    {
        // ���� �ؽ�Ʈ ����
        GameObject glowObj = Instantiate(mainText.gameObject, mainText.transform.parent);
        glowObj.name = $"GlowLayer_{layerIndex}";

        TextMeshProUGUI glowText = glowObj.GetComponent<TextMeshProUGUI>();
        glowCopies[layerIndex] = glowText;

        // �۷ο� ����
        float normalizedIndex = (float)(layerIndex + 1) / glowCopyCount;
        float distance = maxGlowDistance * normalizedIndex;
        float alpha = glowColor.a * (1f - normalizedIndex * 0.7f); // �ּ��� ����

        // ���� ����
        Color layerColor = glowColor;
        layerColor.a = alpha;
        glowText.color = layerColor;

        // �� ȿ���� ���� ������ ����
        float scale = 1f + (normalizedIndex * 0.1f);
        glowText.transform.localScale = Vector3.one * scale;

        // ���� �ؽ�Ʈ���� �ڿ� ��ġ
        glowText.transform.SetAsFirstSibling();

        // �׸��� ȿ���� ���� �ణ�� ������
        RectTransform glowRect = glowText.rectTransform;
        glowRect.anchoredPosition = mainRect.anchoredPosition + Vector2.one * (distance * 0.1f);

        // ��Ƽ���� ���� (�߰� ��)
        SetupGlowMaterial(glowText, normalizedIndex);
    }

    /// <summary>
    /// �۷ο� ��Ƽ���� ����
    /// </summary>
    void SetupGlowMaterial(TextMeshProUGUI glowText, float intensity)
    {
        // �� ��Ƽ���� ����
        Material glowMat = new Material(glowText.font.material);
        glowText.fontMaterial = glowMat;

        // Dilate�� ���� Ȯ��
        float dilate = intensity * 0.3f;
        glowMat.SetFloat("_FaceDilate", dilate);

        // �ܰ������� �߰� Ȯ�� ȿ��
        glowMat.SetFloat("_OutlineWidth", intensity * 0.4f);
        glowMat.SetColor("_OutlineColor", glowColor * 0.5f);

        // �ε巯�� �����ڸ�
        glowMat.SetFloat("_OutlineSoftness", 0.3f);
    }

    void Update()
    {
        if (animateGlow && glowCopies != null)
        {
            AnimateGlowPulse();
        }
    }

    /// <summary>
    /// �۷ο� �޽� �ִϸ��̼�
    /// </summary>
    void AnimateGlowPulse()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0~1 ����
        float intensityMultiplier = 0.7f + (pulse * 0.6f); // 0.7~1.3 ����

        for (int i = 0; i < glowCopies.Length; i++)
        {
            if (glowCopies[i] != null)
            {
                Color currentColor = glowCopies[i].color;
                float baseAlpha = glowColor.a * (1f - ((float)(i + 1) / glowCopyCount) * 0.7f);
                currentColor.a = baseAlpha * intensityMultiplier;
                glowCopies[i].color = currentColor;

                // �����ϵ� �ణ ��ȭ
                float baseScale = 1f + (((float)(i + 1) / glowCopyCount) * 0.1f);
                float scaleMultiplier = 1f + (pulse * 0.05f);
                glowCopies[i].transform.localScale = Vector3.one * baseScale * scaleMultiplier;
            }
        }
    }

    /// <summary>
    /// �ؽ�Ʈ ���� ���� �� ��� ���̾� ������Ʈ
    /// </summary>
    public void UpdateText(string newText)
    {
        if (mainText != null)
        {
            mainText.text = newText;
        }

        if (glowCopies != null)
        {
            for (int i = 0; i < glowCopies.Length; i++)
            {
                if (glowCopies[i] != null)
                {
                    glowCopies[i].text = newText;
                }
            }
        }
    }

    /// <summary>
    /// ��Ÿ�ӿ��� �۷ο� ���� ����
    /// </summary>
    public void SetGlowIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);

        if (glowCopies != null)
        {
            for (int i = 0; i < glowCopies.Length; i++)
            {
                if (glowCopies[i] != null)
                {
                    Color layerColor = glowColor;
                    float baseAlpha = glowColor.a * (1f - ((float)(i + 1) / glowCopyCount) * 0.7f);
                    layerColor.a = baseAlpha * intensity;
                    glowCopies[i].color = layerColor;
                }
            }
        }
    }
}