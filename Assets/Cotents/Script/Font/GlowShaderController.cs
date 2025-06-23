using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GlowShaderController : MonoBehaviour
{
    [Header("�۷ο� ����")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(0.4f, 0.8f, 1f, 0.8f);

    [Range(0f, 3f)]
    public float glowPower = 0.8f;

    [Range(0f, 0.8f)]
    public float glowSpread = 0.1f;

    [Range(0.1f, 2f)]
    public float glowSoftness = 1.5f;

    [Header("�ִϸ��̼�")]
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
    /// �۷ο� ���̴� ����
    /// </summary>
    void SetupGlowShader()
    {
        textComponent = GetComponent<TextMeshProUGUI>();

        // ���̴� ã��
        Shader glowShader = Shader.Find("TextMeshPro/SoftGlow");
        if (glowShader == null)
        {
            Debug.LogError("SoftGlow ���̴��� ã�� �� �����ϴ�! ���̴� ������ ������Ʈ�� �ִ��� Ȯ���ϼ���.");
            return;
        }

        // �� ��Ƽ���� ����
        glowMaterial = new Material(glowShader);
        textComponent.fontMaterial = glowMaterial;

        // �⺻�� ����
        basePower = glowPower;
        UpdateShaderProperties();
    }

    /// <summary>
    /// ���̴� ������Ƽ ������Ʈ
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
            // �޽� �ִϸ��̼�
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            float currentPower = basePower + pulse;
            glowMaterial.SetFloat("_GlowPower", currentPower);
        }
    }

    /// <summary>
    /// �ν����Ϳ��� �� ���� �� �ǽð� ������Ʈ
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
    /// �۷ο� ���� ����
    /// </summary>
    public void SetGlowColor(Color newColor)
    {
        glowColor = newColor;
        if (glowMaterial != null)
            glowMaterial.SetColor("_GlowColor", glowColor);
    }

    /// <summary>
    /// �۷ο� ���� ����
    /// </summary>
    public void SetGlowPower(float power)
    {
        glowPower = power;
        basePower = power;
        if (glowMaterial != null)
            glowMaterial.SetFloat("_GlowPower", glowPower);
    }
}