using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GlowShaderController : MonoBehaviour
{
    [Header("글로우 설정")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(0.4f, 0.8f, 1f, 0.8f);

    [Range(0f, 3f)]
    public float glowPower = 0.8f;

    [Range(0f, 0.8f)]
    public float glowSpread = 0.1f;

    [Range(0.1f, 2f)]
    public float glowSoftness = 1.5f;

    [Header("애니메이션")]
    public bool animateGlow = true;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;

    private TextMeshProUGUI textComponent;
    private Material glowMaterial;
    private float basePower;

    void Start()
    {
        SetupGlowShader();
    }

    /// <summary>
    /// 글로우 셰이더 설정
    /// </summary>
    void SetupGlowShader()
    {
        textComponent = GetComponent<TextMeshProUGUI>();

        // 셰이더 찾기
        Shader glowShader = Shader.Find("TextMeshPro/SoftGlow");
        if (glowShader == null)
        {
            Debug.LogError("SoftGlow 셰이더를 찾을 수 없습니다! 셰이더 파일이 프로젝트에 있는지 확인하세요.");
            return;
        }

        // 새 머티리얼 생성
        glowMaterial = new Material(glowShader);
        textComponent.fontMaterial = glowMaterial;

        // 기본값 설정
        basePower = glowPower;
        UpdateShaderProperties();
    }

    /// <summary>
    /// 셰이더 프로퍼티 업데이트
    /// </summary>
    void UpdateShaderProperties()
    {
        if (glowMaterial == null) return;

        glowMaterial.SetColor("_GlowColor", glowColor);
        glowMaterial.SetFloat("_GlowPower", glowPower);
        glowMaterial.SetFloat("_GlowSpread", glowSpread);
        glowMaterial.SetFloat("_GlowSoftness", glowSoftness);
    }

    void Update()
    {
        if (animateGlow && glowMaterial != null)
        {
            // 펄스 애니메이션
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            float currentPower = basePower + pulse;
            glowMaterial.SetFloat("_GlowPower", currentPower);
        }
    }

    /// <summary>
    /// 인스펙터에서 값 변경 시 실시간 업데이트
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            basePower = glowPower;
            UpdateShaderProperties();
        }
    }

    /// <summary>
    /// 글로우 색상 변경
    /// </summary>
    public void SetGlowColor(Color newColor)
    {
        glowColor = newColor;
        if (glowMaterial != null)
            glowMaterial.SetColor("_GlowColor", glowColor);
    }

    /// <summary>
    /// 글로우 강도 변경
    /// </summary>
    public void SetGlowPower(float power)
    {
        glowPower = power;
        basePower = power;
        if (glowMaterial != null)
            glowMaterial.SetFloat("_GlowPower", glowPower);
    }
}