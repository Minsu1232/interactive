using UnityEngine;

/// <summary>
/// �Ű��� ������ ������ ���� ��ũ��Ʈ - Inspector �Ҵ� ����
/// </summary>
[System.Serializable]
public class MagazineContentData
{
    [Header("�Ű��� ���")]
    public string magazineTitle;                       // ���ö���¡: "magazine_title"
    public string magazineSubtitle;                    // ���ö���¡: "magazine_subtitle" 
    public string issueInfo;                           // ���ö���¡�� ���� ����

    [Header("1�� - ���� ������")]
    public string coverHeadline;                       // ���ö���¡: "magazine_cover_headline"
    public string investmentStyleLabel;               // ���ö���¡: "magazine_investment_style_label"  
    public string investmentStyle;                     // ���ö���¡: "style_balanced_growth_investor" ��
    public string diversificationStars;               // "�ڡڡڡڡ�"  
    public string profileDescription;                 // ���� ��Ÿ�� ����

    [Header("2�� - �� �м�")]
    public string analysisTitle;                       // ���ö���¡: "magazine_analysis_title"
    public string analysisSubtitle;                   // ���ö���¡: "magazine_analysis_subtitle"
    public string corePhilosophyTitle;               // ���ö���¡: "magazine_core_philosophy_title"
    public string corePhilosophy;                     // Core Investment Philosophy ����
    public string marketStrategyTitle;               // ���ö���¡: "magazine_market_strategy_title"
    public string marketStrategy;                     // Market Response Strategy ���� 
    public string expertQuote;                        // ������ �ο뱸
    public string expertSource;                       // ���ö���¡: "magazine_expert_source"

    [Header("AI �̹��� ���� - �����븸")]
    public string imageDescription;                    // �̹��� ���� (ComfyUI ������Ʈ �����)

    /// <summary>
    /// ������ �ʱ�ȭ
    /// </summary>
    public void Initialize()
    {
        magazineTitle = "";
        magazineSubtitle = "";
        issueInfo = "";
        coverHeadline = "";
        investmentStyleLabel = "";
        investmentStyle = "";
        diversificationStars = "";
        profileDescription = "";
        analysisTitle = "";
        analysisSubtitle = "";
        corePhilosophyTitle = "";
        corePhilosophy = "";
        marketStrategyTitle = "";
        marketStrategy = "";
        expertQuote = "";
        expertSource = "";
        imageDescription = "";
    }

    /// <summary>
    /// ������ ��ȿ�� �˻�
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(magazineTitle) &&
               !string.IsNullOrEmpty(investmentStyle) &&
               !string.IsNullOrEmpty(profileDescription);
    }

    /// <summary>
    /// ����� ���� ���
    /// </summary>
    public void LogDebugInfo()
    {
        Debug.Log($"=== �Ű��� ������ ������ ===");
        Debug.Log($"����: {magazineTitle}");
        Debug.Log($"���ڽ�Ÿ��: {investmentStyle}");
        Debug.Log($"�л�����: {diversificationStars}");
      
    }
}

