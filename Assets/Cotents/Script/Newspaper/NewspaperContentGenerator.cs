using UnityEngine;
using System;

/// <summary>
/// 간단한 매거진 콘텐츠 생성기 - HTML처럼 깔끔하게!
/// </summary>
public class NewspaperContentGenerator : MonoBehaviour
{
    [Header("생성 설정")]
    public bool useLocalization = true;

    [Header("디버그")]
    public bool enableDebugLog = true;


    /// <summary>
    /// 메인 매거진 콘텐츠 생성 (간단 버전)
    /// </summary>
    public MagazineContentData GenerateMagazineContent(GameResult gameResult)
    {
        var content = new MagazineContentData();

        // 기본 매거진 정보
        content.magazineTitle = GetLocalizedText("magazine_title", "INVESTOR");
        content.magazineSubtitle = GetLocalizedText("magazine_subtitle", "AI Investment Analysis");
        content.issueInfo = GenerateIssueInfo();

        // 투자 스타일 + 유명 투자자 분석
        var celebrityInfo = AnalyzeInvestmentStyleWithCelebrity(gameResult);

        // 1면: 투자 프로필
        content.coverHeadline = GetLocalizedText("magazine_cover_headline", "Investment Profile");
        content.investmentStyleLabel = GetLocalizedText("magazine_investment_style_label", "Investment Style");

        // 유명 투자자 스타일 표시
        string celebrityName = CSVLocalizationManager.Instance?.currentLanguage == Language.Korean ?
            celebrityInfo.celebrityNameKor : celebrityInfo.celebrityNameEng;
        string a = "";
        if (CSVLocalizationManager.Instance?.currentLanguage == Language.Korean)
        {
             a = "스타일";
        }
        else
        {
            a = "Style";
        }
        content.investmentStyle = GetLocalizedText(celebrityInfo.styleKey, celebrityInfo.styleKey) +
                                 $"\n({celebrityName} {a})";

        content.diversificationStars = GenerateDiversificationStars(gameResult.maxSectorsDiversified);
        content.profileDescription = GenerateProfileDescriptionWithCelebrity(gameResult, celebrityInfo);

        // 2면: 상세 분석
        content.analysisTitle = GetLocalizedText("magazine_analysis_title", "Investment Analysis");
        content.analysisSubtitle = GetLocalizedText("magazine_analysis_subtitle", "성공적인 투자 전략의 핵심");
        content.corePhilosophyTitle = GetLocalizedText("magazine_core_philosophy_title", "Core Philosophy");
        content.corePhilosophy = GenerateCorePhilosophyWithCelebrity(gameResult, celebrityInfo);
        content.marketStrategyTitle = GetLocalizedText("magazine_market_strategy_title", "Market Strategy");
        content.marketStrategy = GenerateMarketStrategyWithCelebrity(gameResult, celebrityInfo);
        content.expertQuote = GenerateExpertQuoteWithCelebrity(gameResult, celebrityInfo);
        content.expertSource = GetLocalizedText("magazine_expert_source", "AI Investment Research Institute");

        // AI 이미지 설명
        content.imageDescription = GenerateImageDescription(gameResult.lifestyleGrade);

        if (enableDebugLog)
        {
            Debug.Log($"📰 매거진 생성 완료: {celebrityInfo.celebrityNameKor} 스타일, 수익률: {gameResult.profitRate:F1}%");
        }

        return content;
    }

    /// <summary>
    /// 발행 정보 생성
    /// </summary>
    string GenerateIssueInfo()
    {
        string issueLabel = GetLocalizedText("magazine_issue_label", "ISSUE 01");
        string dateFormat = GetLocalizedText("magazine_date_format", "yyyy MM dd");
        string localizedDate = DateTime.Now.ToString(dateFormat);
        Debug.Log($"발행일: {localizedDate}");
        return $"{issueLabel}\n{localizedDate}";
    }

    /// <summary>
    /// 투자 스타일 분석 (5가지)
    /// </summary>

    /// <summary>
    /// 유명 투자자를 포함한 전문가 코멘트 생성
    /// </summary>
    string GenerateExpertQuoteWithCelebrity(GameResult gameResult, InvestmentStyleWithCelebrity celebrityInfo)
    {
        string celebrityName = CSVLocalizationManager.Instance?.currentLanguage == Language.Korean ?
            celebrityInfo.celebrityNameKor : celebrityInfo.celebrityNameEng;

        bool isProfit = gameResult.profitRate > 0;

        return celebrityInfo.styleKey switch
        {
            "style_balanced_growth_investor" => isProfit ?
                GetLocalizedText("advice_balanced_success") :
                GetLocalizedText("advice_balanced_improve"),

            "style_active_trader" => isProfit ?
                GetLocalizedText("advice_active_success") :
                GetLocalizedText("advice_active_improve"),

            "style_focused_investor" => isProfit ?
                GetLocalizedText("advice_focused_success") :
                GetLocalizedText("advice_focused_improve"),

            "style_cautious_investor" => isProfit ?
                GetLocalizedText("advice_cautious_success") :
                GetLocalizedText("advice_cautious_improve"),

            "style_growth_investor" => isProfit ?
                GetLocalizedText("advice_growth_success") :
                GetLocalizedText("advice_growth_improve"),

            "style_steady_investor" => isProfit ?
                GetLocalizedText("advice_steady_success") :
                GetLocalizedText("advice_steady_improve"),

            _ => isProfit ?
                GetLocalizedText("advice_default_success", "훌륭한 성과입니다!") :
                GetLocalizedText("advice_default_improve", "다음에는 더 좋은 결과가 있을 거예요!")
        };
    }
    /// <summary>
    /// 유명 투자자 정보를 포함한 시장 전략 생성
    /// </summary>
    string GenerateMarketStrategyWithCelebrity(GameResult gameResult, InvestmentStyleWithCelebrity celebrityInfo)
    {
        string celebrityName = CSVLocalizationManager.Instance?.currentLanguage == Language.Korean ?
            celebrityInfo.celebrityNameKor : celebrityInfo.celebrityNameEng;

        return celebrityInfo.styleKey switch
        {
            "style_balanced_growth_investor" => GetLocalizedText("strategy_balanced_complete",
                $"{celebrityName}의 올웨더 전략처럼 균형잡힌 분산투자를 통해 안정성과 성장성을 동시에 추구했습니다."),
            "style_active_trader" => GetLocalizedText("strategy_active_complete",
                $"{celebrityName}의 퀀텀 펀드 전략처럼 빠른 시장 변화에 민감하게 반응하며 적극적인 매매를 구사했습니다."),
            "style_focused_investor" => GetLocalizedText("strategy_focused_complete",
                $"{celebrityName}의 가치투자 전략처럼 선별된 우수 기업에 집중하여 장기적 관점에서 투자했습니다."),
            "style_cautious_investor" => GetLocalizedText("strategy_cautious_complete",
                $"{celebrityName}의 안전마진 전략처럼 신중한 분석을 통해 리스크를 최소화하며 투자했습니다."),
            "style_growth_investor" => GetLocalizedText("strategy_growth_complete",
                $"{celebrityName}의 성장주 발굴 전략처럼 숨겨진 성장 기업을 찾아 투자하는 전략을 보여줍니다."),
            "style_steady_investor" => GetLocalizedText("strategy_steady_complete",
    $"{celebrityName}의 인덱스 전략처럼 꾸준하고 안정적인 장기 투자 전략을 구사했습니다."),
            _ => GetLocalizedText("strategy_steady_complete",
                $"{celebrityName}의 인덱스 전략처럼 꾸준하고 안정적인 장기 투자 전략을 구사했습니다.")
        };
    }
        /// <summary>
        /// 정밀한 유명 투자자 매칭 시스템 - 각 투자자의 고유 특징을 세밀하게 분석
        /// </summary>
        InvestmentStyleWithCelebrity AnalyzeInvestmentStyleWithCelebrity(GameResult gameResult)
        {
            int totalTurns = gameResult.totalTurns;
            int totalTrades = gameResult.totalTrades;
            int maxSectors = gameResult.maxSectorsDiversified;
            float profitRate = gameResult.profitRate;
            float diversificationBonus = gameResult.diversificationBonus;

            // 핵심 지표 계산
            float tradeIntensity = (float)totalTrades / totalTurns;          // 턴당 거래 횟수
            float tradesPerSector = maxSectors > 0 ? (float)totalTrades / maxSectors : 0; // 섹터당 거래 횟수
            bool isHighDiversification = maxSectors >= 4;                    // 고분산 여부
            bool isLowTrades = totalTrades <= 10;                           // 저거래 여부
            bool isHighTrades = totalTrades >= 20;                          // 고거래 여부

            if (enableDebugLog)
            {
                Debug.Log($"🔍 투자 스타일 분석:");
                Debug.Log($"   거래강도: {tradeIntensity:F2} (거래횟수: {totalTrades}, 턴수: {totalTurns})");
                Debug.Log($"   분산도: {maxSectors}섹터, 보너스: {diversificationBonus:F1}%");
                Debug.Log($"   수익률: {profitRate:F1}%");
                Debug.Log($"   섹터당거래: {tradesPerSector:F1}");
            }

            // 1. 벤저민 그레이엄 - 신중한 가치투자자 (최고 우선순위)
            // 특징: 매우 신중함 + 완전분산 + 안전마진 추구 + 적당한 수익
            if (IsGrahamStyle(totalTrades, maxSectors, profitRate, diversificationBonus))
            {
                if (enableDebugLog) Debug.Log("✅ 벤저민 그레이엄 스타일 확정");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_cautious_investor",
                    celebrityNameKor = "벤저민 그레이엄",
                    celebrityNameEng = "Benjamin Graham",
                    description = "신중한 분석을 통한 안전 투자"
                };
            }

            // 2. 워런 버핏 - 집중 가치투자자
            // 특징: 적은 거래 + 집중투자(섹터 적음) + 높은 수익률
            if (IsBuffettStyle(totalTrades, maxSectors, profitRate, tradeIntensity))
            {
                if (enableDebugLog) Debug.Log("✅ 워런 버핏 스타일 확정");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_focused_investor",
                    celebrityNameKor = "워런 버핏",
                    celebrityNameEng = "Warren Buffett",
                    description = "선별된 우수 기업에 장기 집중 투자"
                };
            }

            // 3. 조지 소로스 - 적극적 트레이더
            // 특징: 매우 많은 거래 + 중간 분산 + 높은 수익률 + 리스크 감수
            if (IsSorosStyle(totalTrades, maxSectors, profitRate, tradeIntensity))
            {
                if (enableDebugLog) Debug.Log("✅ 조지 소로스 스타일 확정");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_active_trader",
                    celebrityNameKor = "조지 소로스",
                    celebrityNameEng = "George Soros",
                    description = "시장 기회를 포착하는 적극적 매매"
                };
            }

            // 4. 피터 린치 - 성장주 헌터
            // 특징: 중간 거래 + 성장 추구 + 높은 수익률 + 기술주 선호
            if (IsLynchStyle(totalTrades, maxSectors, profitRate, tradeIntensity))
            {
                if (enableDebugLog) Debug.Log("✅ 피터 린치 스타일 확정");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_growth_investor",
                    celebrityNameKor = "피터 린치",
                    celebrityNameEng = "Peter Lynch",
                    description = "성장 기업 발굴을 통한 수익 창출"
                };
            }

            // 5. 레이 달리오 - 균형 분산투자자
            // 특징: 높은 분산 + 중간 거래 + 안정적 수익
            if (IsDalioStyle(totalTrades, maxSectors, profitRate, diversificationBonus))
            {
                if (enableDebugLog) Debug.Log("✅ 레이 달리오 스타일 확정");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_balanced_growth_investor",
                    celebrityNameKor = "레이 달리오",
                    celebrityNameEng = "Ray Dalio",
                    description = "위험 분산을 통한 안정적 성장 추구"
                };
            }

            // ===============================
            // 🛡️ 안전망: 조건 완화 단계
            // ===============================

            // 안전망 1: 완화된 버핏 스타일 (집중투자면서 수익 괜찮음)
            if (maxSectors <= 2 && profitRate >= 20f)
            {
                if (enableDebugLog) Debug.Log("🛡️ 완화된 워런 버핏 스타일");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_focused_investor",
                    celebrityNameKor = "워런 버핏",
                    celebrityNameEng = "Warren Buffett",
                    description = "선별된 우수 기업에 장기 집중 투자"
                };
            }

            // 안전망 2: 완화된 소로스 스타일 (매우 액티브함)
            if (totalTrades >= 20 && profitRate >= 15f)
            {
                if (enableDebugLog) Debug.Log("🛡️ 완화된 조지 소로스 스타일");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_active_trader",
                    celebrityNameKor = "조지 소로스",
                    celebrityNameEng = "George Soros",
                    description = "시장 기회를 포착하는 적극적 매매"
                };
            }

            // 안전망 3: 완화된 그레이엄 스타일 (신중하고 분산 잘함)
            if (totalTrades <= 12 && maxSectors >= 4)
            {
                if (enableDebugLog) Debug.Log("🛡️ 완화된 벤저민 그레이엄 스타일");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_cautious_investor",
                    celebrityNameKor = "벤저민 그레이엄",
                    celebrityNameEng = "Benjamin Graham",
                    description = "신중한 분석을 통한 안전 투자"
                };
            }

            // 안전망 4: 완화된 달리오 스타일 (분산 잘함)
            if (maxSectors >= 3 && profitRate >= 0f)
            {
                if (enableDebugLog) Debug.Log("🛡️ 완화된 레이 달리오 스타일");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_balanced_growth_investor",
                    celebrityNameKor = "레이 달리오",
                    celebrityNameEng = "Ray Dalio",
                    description = "위험 분산을 통한 안정적 성장 추구"
                };
            }

            // 안전망 5: 수익률 기준 백업
            if (profitRate >= 40f)
            {
                if (enableDebugLog) Debug.Log("🛡️ 수익률 기준 피터 린치 스타일");
                return new InvestmentStyleWithCelebrity
                {
                    styleKey = "style_growth_investor",
                    celebrityNameKor = "피터 린치",
                    celebrityNameEng = "Peter Lynch",
                    description = "성장 기업 발굴을 통한 수익 창출"
                };
            }

            // ===============================
            // 🔄 최종 안전망 (절대 실패 없음)
            // ===============================

            // 6. 존 보글 - 안정적 인덱스 투자자 (무조건 적용)
            if (enableDebugLog) Debug.Log("✅ 존 보글 스타일 (최종 안전망)");
            return new InvestmentStyleWithCelebrity
            {
                styleKey = "style_steady_investor",
                celebrityNameKor = "존 보글",
                celebrityNameEng = "John Bogle",
                description = "꾸준하고 안정적인 장기 투자"
            };
        }
    

    /// <summary>
    /// 벤저민 그레이엄 스타일 판정 - 신중한 완전분산 가치투자
    /// </summary>
    bool IsGrahamStyle(int totalTrades, int maxSectors, float profitRate, float diversificationBonus)
    {
        // 핵심 조건: 매우 신중 + 완전분산 + 안정적 수익
        bool isVeryCautious = totalTrades <= 10;                     // 매우 적은 거래
        bool isFullyDiversified = maxSectors >= 5;                   // 완전 분산 (5섹터)
        bool hasHighDiversificationBonus = diversificationBonus >= 15.0f; // 높은 분산 보너스
        bool hasModerateProfit = profitRate >= 0f && profitRate <= 30f;   // 안정적이지만 보수적 수익

        return isVeryCautious && isFullyDiversified && hasHighDiversificationBonus && hasModerateProfit;
    }
    /// <summary>
    /// 워런 버핏 스타일 판정 - 집중투자 가치투자
    /// </summary>
    bool IsBuffettStyle(int totalTrades, int maxSectors, float profitRate, float tradeIntensity)
    {
        // 핵심 조건: 적은 거래 + 집중투자 + 높은 수익
        bool isLongTermHolder = totalTrades <= 8;                    // 매우 적은 거래 (장기보유)
        bool isFocusedInvestor = maxSectors <= 2;                    // 집중투자 (2섹터 이하)
        bool hasHighReturn = profitRate >= 40f;                      // 높은 수익률
        bool isPatient = tradeIntensity <= 0.8f;                     // 인내심 있는 거래

        return isLongTermHolder && isFocusedInvestor && hasHighReturn && isPatient;
    }

    /// <summary>
    /// 조지 소로스 스타일 판정 - 적극적 헤지펀드 스타일
    /// </summary>
    bool IsSorosStyle(int totalTrades, int maxSectors, float profitRate, float tradeIntensity)
    {
        // 핵심 조건: 매우 많은 거래 + 중간 분산 + 높은 수익 + 적극성
        bool isVeryActive = totalTrades >= 25;                       // 매우 많은 거래
        bool isModerateDiversification = maxSectors >= 2 && maxSectors <= 4; // 중간 분산
        bool hasExcellentReturn = profitRate >= 35f;                 // 뛰어난 수익률
        bool isAggressive = tradeIntensity >= 2.5f;                  // 적극적 거래

        return isVeryActive && isModerateDiversification && hasExcellentReturn && isAggressive;
    }

    /// <summary>
    /// 피터 린치 스타일 판정 - 성장주 발굴 전문가
    /// </summary>
    bool IsLynchStyle(int totalTrades, int maxSectors, float profitRate, float tradeIntensity)
    {
        // 핵심 조건: 중간 거래 + 성장 추구 + 매우 높은 수익
        bool isModeratlyActive = totalTrades >= 15 && totalTrades <= 25; // 중간 거래량
        bool hasGrowthFocus = maxSectors >= 2 && maxSectors <= 4;    // 성장 분야 집중
        bool hasOutstandingReturn = profitRate >= 50f;               // 매우 높은 수익률 (성장주 대박)
        bool isGrowthHunter = tradeIntensity >= 1.5f && tradeIntensity <= 2.5f; // 성장주 헌팅

        return isModeratlyActive && hasGrowthFocus && hasOutstandingReturn && isGrowthHunter;
    }

    /// <summary>
    /// 레이 달리오 스타일 판정 - 올웨더 포트폴리오
    /// </summary>
    bool IsDalioStyle(int totalTrades, int maxSectors, float profitRate, float diversificationBonus)
    {
        // 핵심 조건: 높은 분산 + 중간 거래 + 균형잡힌 수익
        bool isWellDiversified = maxSectors == 4;                    // 4섹터 분산 (완전 5섹터는 아님)
        bool hasGoodDiversificationBonus = diversificationBonus >= 10.0f && diversificationBonus < 20.0f; // 중간 분산 보너스
        bool hasBalancedReturn = profitRate >= 25f && profitRate <= 45f; // 균형잡힌 수익률
        bool isModerateTrades = totalTrades >= 10 && totalTrades <= 20; // 중간 거래량

        return isWellDiversified && hasGoodDiversificationBonus && hasBalancedReturn && isModerateTrades;
    }
    /// <summary>
    /// 유명 투자자 정보를 포함한 핵심 철학 생성
    /// </summary>
    string GenerateCorePhilosophyWithCelebrity(GameResult gameResult, InvestmentStyleWithCelebrity celebrityInfo)
    {
        string celebrityName = CSVLocalizationManager.Instance?.currentLanguage == Language.Korean ?
            celebrityInfo.celebrityNameKor : celebrityInfo.celebrityNameEng;

        return celebrityInfo.styleKey switch
        {
            "style_balanced_growth_investor" => GetLocalizedText("philosophy_balanced_complete",
                $"{celebrityName}처럼 리스크 분산과 성장 기회의 균형을 추구하는 체계적인 투자 철학을 보여줍니다."),
            "style_active_trader" => GetLocalizedText("philosophy_active_complete",
                $"{celebrityName}처럼 시장 흐름을 면밀히 분석하여 최적의 타이밍을 포착하는 능동적 투자 철학이 특징입니다."),
            "style_focused_investor" => GetLocalizedText("philosophy_focused_complete",
                $"{celebrityName}처럼 선별된 우수 기업에 집중하여 장기적 가치를 추구하는 투자 철학을 구사합니다."),
            "style_cautious_investor" => GetLocalizedText("philosophy_cautious_complete",
                $"{celebrityName}처럼 신중한 분석과 계산된 의사결정을 바탕으로 하는 사려깊은 투자 철학입니다."),
            "style_growth_investor" => GetLocalizedText("philosophy_growth_complete",
                $"{celebrityName}처럼 성장 기업을 발굴하여 장기적 성과를 추구하는 투자 철학을 보여줍니다."),
            "style_steady_investor" => GetLocalizedText("philosophy_steady_complete",
   $"{celebrityName}처럼 균형잡힌 투자 접근법을 통해 안정적인 성과를 추구하는 철학입니다."),
            _ => GetLocalizedText("philosophy_default",
                $"{celebrityName}처럼 균형잡힌 투자 접근법을 통해 안정적인 성과를 추구하는 철학입니다.")
        };
    }
    /// <summary>
    /// 유명 투자자 정보를 포함한 프로필 설명 생성
    /// </summary>
    string GenerateProfileDescriptionWithCelebrity(GameResult gameResult, InvestmentStyleWithCelebrity celebrityInfo)
    {
        string description = celebrityInfo.description;

        // 분산투자 보너스 추가
        if (gameResult.diversificationBonus > 0)
        {
            string bonusText = GetLocalizedText("profile_diversification_success");
            description += " " + string.Format(bonusText, gameResult.diversificationBonus.ToString("F1"));
        }

        return description;
    }





    /// <summary>
    /// 성과 등급 결정 (5단계)
    /// </summary>
    PerformanceGrade DeterminePerformanceGrade(GameResult gameResult)
    {
        if (gameResult.profitRate >= 50f)
            return PerformanceGrade.Excellent;
        else if (gameResult.profitRate >= 20f)
            return PerformanceGrade.Good;
        else if (gameResult.profitRate >= 0f)
            return PerformanceGrade.Average;
        else if (gameResult.profitRate >= -20f)
            return PerformanceGrade.Learning;
        else
            return PerformanceGrade.Challenge;
    }

    /// <summary>
    /// 분산투자 별점 생성
    /// </summary>
    string GenerateDiversificationStars(int maxSectors)
    {
        string stars = "";
        for (int i = 1; i <= 5; i++)
        {
            stars += (i <= maxSectors) ? "★" : "☆";
        }
        return stars;
    }

    /// <summary>
    /// AI 이미지 설명 생성
    /// </summary>
    string GenerateImageDescription(LifestyleGrade grade)
    {
        return grade switch
        {
            LifestyleGrade.Upper => "luxury penthouse interior, premium furniture, city view, elegant lifestyle",
            LifestyleGrade.MiddleUpper => "modern apartment interior, comfortable living room, middle-class lifestyle",
            LifestyleGrade.Middle => "cozy home interior, simple furniture, everyday comfortable living",
            LifestyleGrade.Lower => "modest room interior, basic furniture, simple living space",
            _ => "modern interior design, clean living space"
        };
    }

    /// <summary>
    /// 로컬라이징 텍스트 가져오기
    /// </summary>
    string GetLocalizedText(string key, string fallback = "")
    {
        if (useLocalization && CSVLocalizationManager.Instance != null)
        {
            return CSVLocalizationManager.Instance.GetLocalizedText(key);
        }
        return !string.IsNullOrEmpty(fallback) ? fallback : key;
    }
}

#region 간단한 데이터 구조

/// <summary>
/// 성과 등급 (교육용)
/// </summary>
public enum PerformanceGrade
{
    Excellent,    // 50%+
    Good,         // 20%+
    Average,      // 0%+
    Learning,     // -20%+
    Challenge     // -20% 미만
}

/// <summary>
/// 투자 스타일 정보
/// </summary>
[System.Serializable]
public class InvestmentStyleInfo
{
    public string localizationKey;
    public string englishName;
}

#endregion