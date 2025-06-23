using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoftGlowEffect : MonoBehaviour
{
    [Header("메인 텍스트")]
    public TextMeshProUGUI mainText;

    [Header("글로우 설정")]
    public int glowCopyCount = 4;           // 글로우 복사본 개수 (더 많을수록 부드러움)
    public float maxGlowDistance = 20f;     // 최대 글로우 거리
    public Color glowColor = new Color(0.4f, 0.8f, 1f, 0.6f);
    public bool animateGlow = true;         // 글로우 애니메이션 여부
    public float pulseSpeed = 2f;           // 펄스 속도

    private TextMeshProUGUI[] glowCopies;
    private RectTransform mainRect;

    void Start()
    {
        SetupSoftGlow();
    }

    /// <summary>
    /// 부드러운 글로우 효과 설정
    /// </summary>
    void SetupSoftGlow()
    {
        if (mainText == null) return;

        mainRect = mainText.rectTransform;
        glowCopies = new TextMeshProUGUI[glowCopyCount];

        // 글로우 레이어들 생성
        for (int i = 0; i < glowCopyCount; i++)
        {
            CreateGlowLayer(i);
        }

        // 메인 텍스트를 최상위로
        mainText.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 개별 글로우 레이어 생성
    /// </summary>
    void CreateGlowLayer(int layerIndex)
    {
        // 메인 텍스트 복사
        GameObject glowObj = Instantiate(mainText.gameObject, mainText.transform.parent);
        glowObj.name = $"GlowLayer_{layerIndex}";

        TextMeshProUGUI glowText = glowObj.GetComponent<TextMeshProUGUI>();
        glowCopies[layerIndex] = glowText;

        // 글로우 설정
        float normalizedIndex = (float)(layerIndex + 1) / glowCopyCount;
        float distance = maxGlowDistance * normalizedIndex;
        float alpha = glowColor.a * (1f - normalizedIndex * 0.7f); // 멀수록 투명

        // 색상 설정
        Color layerColor = glowColor;
        layerColor.a = alpha;
        glowText.color = layerColor;

        // 블러 효과를 위한 스케일 증가
        float scale = 1f + (normalizedIndex * 0.1f);
        glowText.transform.localScale = Vector3.one * scale;

        // 메인 텍스트보다 뒤에 배치
        glowText.transform.SetAsFirstSibling();

        // 그림자 효과를 위한 약간의 오프셋
        RectTransform glowRect = glowText.rectTransform;
        glowRect.anchoredPosition = mainRect.anchoredPosition + Vector2.one * (distance * 0.1f);

        // 머티리얼 설정 (추가 블러)
        SetupGlowMaterial(glowText, normalizedIndex);
    }

    /// <summary>
    /// 글로우 머티리얼 설정
    /// </summary>
    void SetupGlowMaterial(TextMeshProUGUI glowText, float intensity)
    {
        // 새 머티리얼 생성
        Material glowMat = new Material(glowText.font.material);
        glowText.fontMaterial = glowMat;

        // Dilate로 글자 확장
        float dilate = intensity * 0.3f;
        glowMat.SetFloat("_FaceDilate", dilate);

        // 외곽선으로 추가 확산 효과
        glowMat.SetFloat("_OutlineWidth", intensity * 0.4f);
        glowMat.SetColor("_OutlineColor", glowColor * 0.5f);

        // 부드러운 가장자리
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
    /// 글로우 펄스 애니메이션
    /// </summary>
    void AnimateGlowPulse()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0~1 범위
        float intensityMultiplier = 0.7f + (pulse * 0.6f); // 0.7~1.3 범위

        for (int i = 0; i < glowCopies.Length; i++)
        {
            if (glowCopies[i] != null)
            {
                Color currentColor = glowCopies[i].color;
                float baseAlpha = glowColor.a * (1f - ((float)(i + 1) / glowCopyCount) * 0.7f);
                currentColor.a = baseAlpha * intensityMultiplier;
                glowCopies[i].color = currentColor;

                // 스케일도 약간 변화
                float baseScale = 1f + (((float)(i + 1) / glowCopyCount) * 0.1f);
                float scaleMultiplier = 1f + (pulse * 0.05f);
                glowCopies[i].transform.localScale = Vector3.one * baseScale * scaleMultiplier;
            }
        }
    }

    /// <summary>
    /// 텍스트 내용 변경 시 모든 레이어 업데이트
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
    /// 런타임에서 글로우 강도 조절
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