using UnityEngine;

/// <summary>
/// 매거진 콘텐츠 데이터 전용 스크립트 - Inspector 할당 가능
/// </summary>
[System.Serializable]
public class MagazineContentData
{
    [Header("매거진 헤더")]
    public string magazineTitle;                       // 로컬라이징: "magazine_title"
    public string magazineSubtitle;                    // 로컬라이징: "magazine_subtitle" 
    public string issueInfo;                           // 로컬라이징된 발행 정보

    [Header("1면 - 투자 프로필")]
    public string coverHeadline;                       // 로컬라이징: "magazine_cover_headline"
    public string investmentStyleLabel;               // 로컬라이징: "magazine_investment_style_label"  
    public string investmentStyle;                     // 로컬라이징: "style_balanced_growth_investor" 등
    public string diversificationStars;               // "★★★★☆"  
    public string profileDescription;                 // 투자 스타일 설명

    [Header("2면 - 상세 분석")]
    public string analysisTitle;                       // 로컬라이징: "magazine_analysis_title"
    public string analysisSubtitle;                   // 로컬라이징: "magazine_analysis_subtitle"
    public string corePhilosophyTitle;               // 로컬라이징: "magazine_core_philosophy_title"
    public string corePhilosophy;                     // Core Investment Philosophy 내용
    public string marketStrategyTitle;               // 로컬라이징: "magazine_market_strategy_title"
    public string marketStrategy;                     // Market Response Strategy 내용 
    public string expertQuote;                        // 전문가 인용구
    public string expertSource;                       // 로컬라이징: "magazine_expert_source"

    [Header("AI 이미지 정보 - 참조용만")]
    public string imageDescription;                    // 이미지 설명 (ComfyUI 프롬프트 참고용)

    /// <summary>
    /// 데이터 초기화
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
    /// 데이터 유효성 검사
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(magazineTitle) &&
               !string.IsNullOrEmpty(investmentStyle) &&
               !string.IsNullOrEmpty(profileDescription);
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void LogDebugInfo()
    {
        Debug.Log($"=== 매거진 콘텐츠 데이터 ===");
        Debug.Log($"제목: {magazineTitle}");
        Debug.Log($"투자스타일: {investmentStyle}");
        Debug.Log($"분산투자: {diversificationStars}");
      
    }
}

