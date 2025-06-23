using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StockIconData
{
    [Header("Stock Info")]
    public string stockName;
    public string displayName;
    public string sector;

    [Header("Icon Generation")]
    [TextArea(2, 4)]
    public string iconPrompt;
    public string colorTheme;
    public bool generated = false;

    [Header("Real Company Inspiration")]
    [TextArea(1, 2)]
    public string realCompanyRef; // 개발 참고용 (실제 생성에는 미사용)
}

public class IconGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public bool generateOnStart = false;
    public bool testConnectionFirst = true;
    public float delayBetweenGeneration = 3f;

    [Header("LoRA Settings")]
    public bool useLoRA = true;
    public string loraFileName = "iconsXL.safetensors";
    public float loraStrength = 0.8f; // 0.8로 낮춤 (안정성)

    [Header("15개 종목 아이콘 설정")]
    public StockIconData[] stockIcons = new StockIconData[]
    {
        // 🔵 기술주 섹터 (5개) - 현실 기업 오마주
        new StockIconData
        {
            stockName = "스마트테크",
            displayName = "SmartTech",
            sector = "tech",
            iconPrompt = "smartphone icon, mobile device symbol, sleek phone outline, technology logo",
            colorTheme = "blue corporate color",
            realCompanyRef = "Apple 스타일 - 심플한 제품 아이콘"
        },
        new StockIconData
        {
            stockName = "클라우드킹",
            displayName = "CloudKing",
            sector = "tech",
            iconPrompt = "cloud computing symbol, fluffy cloud icon, data storage cloud shape, server technology",
            colorTheme = "blue technology color",
            realCompanyRef = "Microsoft Azure 스타일 - 클라우드 심볼"
        },
        new StockIconData
        {
            stockName = "서치마스터",
            displayName = "SearchMaster",
            sector = "tech",
            iconPrompt = "magnifying glass icon, search lens symbol, discovery tool shape, information technology",
            colorTheme = "multicolor gradient",
            realCompanyRef = "Google 스타일 - 검색 도구"
        },
        new StockIconData
        {
            stockName = "소셜버스",
            displayName = "SocialVerse",
            sector = "tech",
            iconPrompt = "social network nodes, connection symbol, communication web icon, platform logo",
            colorTheme = "blue social color",
            realCompanyRef = "Meta/Facebook 스타일 - 연결성"
        },
        new StockIconData
        {
            stockName = "스트림플러스",
            displayName = "StreamPlus",
            sector = "tech",
            iconPrompt = "play button triangle, streaming media symbol, entertainment play icon, video platform",
            colorTheme = "red entertainment color",
            realCompanyRef = "Netflix 스타일 - 재생 버튼"
        },
        
        // 🟢 전기차/에너지 섹터 (3개)
        new StockIconData
        {
            stockName = "썬더모터스",
            displayName = "ThunderMotors",
            sector = "ev",
            iconPrompt = "electric car silhouette, modern vehicle outline, sleek automotive shape, EV logo",
            colorTheme = "electric blue energy color",
            realCompanyRef = "Tesla 스타일 - 미래적 자동차"
        },
        new StockIconData
        {
            stockName = "그린카",
            displayName = "GreenCar",
            sector = "ev",
            iconPrompt = "eco-friendly car icon, leaf-shaped vehicle, green transportation symbol, sustainable auto",
            colorTheme = "eco green color",
            realCompanyRef = "친환경 자동차 브랜드"
        },
        new StockIconData
        {
            stockName = "클린에너지",
            displayName = "CleanEnergy",
            sector = "energy",
            iconPrompt = "wind turbine blade icon, renewable energy symbol, clean power windmill, sustainable energy",
            colorTheme = "sustainable green color",
            realCompanyRef = "재생에너지 기업"
        },
        
        // 🟡 반도체/AI 섹터 (3개)
        new StockIconData
        {
            stockName = "네오칩스",
            displayName = "NeoChips",
            sector = "semiconductor",
            iconPrompt = "microprocessor chip icon, CPU square symbol, computer brain chip, AI processor",
            colorTheme = "tech yellow gold color",
            realCompanyRef = "NVIDIA 스타일 - AI 칩"
        },
        new StockIconData
        {
            stockName = "칩팩토리",
            displayName = "ChipFactory",
            sector = "semiconductor",
            iconPrompt = "circuit board pattern, electronic pathways icon, tech manufacturing symbol, chip production",
            colorTheme = "industrial orange color",
            realCompanyRef = "TSMC 스타일 - 제조업"
        },
        new StockIconData
        {
            stockName = "라이젠텍",
            displayName = "RyzenTech",
            sector = "semiconductor",
            iconPrompt = "processor core icon, multi-core CPU symbol, computing power emblem, performance chip",
            colorTheme = "performance red color",
            realCompanyRef = "AMD 스타일 - 프로세서"
        },
        
        // 🟠 가상자산 섹터 (2개)
        new StockIconData
        {
            stockName = "디지털골드",
            displayName = "DigitalGold",
            sector = "crypto",
            iconPrompt = "digital coin symbol, cryptocurrency circle icon, blockchain currency emblem, crypto gold",
            colorTheme = "golden crypto color",
            realCompanyRef = "Bitcoin 스타일 - 디지털 화폐"
        },
        new StockIconData
        {
            stockName = "스마트코인",
            displayName = "SmartCoin",
            sector = "crypto",
            iconPrompt = "smart contract icon, hexagonal blockchain symbol, decentralized network emblem, fintech platform",
            colorTheme = "tech purple color",
            realCompanyRef = "Ethereum 스타일 - 스마트 컨트랙트"
        },
        
        // 🔴 대기업 섹터 (2개)
        new StockIconData
        {
            stockName = "코리아일렉",
            displayName = "KoreaElec",
            sector = "corporate",
            iconPrompt = "electronics gear icon, consumer tech symbol, digital device emblem, electronic products",
            colorTheme = "corporate blue color",
            realCompanyRef = "Samsung 스타일 - 종합 전자"
        },
        new StockIconData
        {
            stockName = "메모리킹",
            displayName = "MemoryKing",
            sector = "corporate",
            iconPrompt = "memory storage icon, data chip symbol, information storage emblem, memory technology",
            colorTheme = "storage silver color",
            realCompanyRef = "SK Hynix 스타일 - 메모리"
        }
    };

    [Header("Base Style Settings")]
    [TextArea(2, 4)]
    public string baseStyle = "minimalist icon, simple 2D design, clean logo style, professional corporate identity, flat vector graphic, no complex details, no text, pure white background, isolated on white, white backdrop";

    [Header("강제 흰배경 설정")]
    public bool forceWhiteBackground = true;
    public string whiteBackgroundKeywords = "pure white background, isolated on white, white backdrop, clean white background";
    public string backgroundNegativeKeywords = "colored background, blue background, gray background, gradient background, textured background, patterned background, colored backdrop, non-white background";

    private int currentGenerationIndex = 0;
    private bool isGenerating = false;

    void Start()
    {
        if (generateOnStart)
        {
            StartIconGeneration();
        }
    }

    /// <summary>
    /// 기업 아이콘 생성 시작
    /// </summary>
    public void StartIconGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateAllIcons());
        }
        else
        {
            Debug.LogWarning("⚠️ Icon generation already in progress!");
        }
    }

    /// <summary>
    /// 모든 기업 아이콘 생성
    /// </summary>
    private IEnumerator GenerateAllIcons()
    {
        isGenerating = true;
        Debug.Log("🏢 Starting corporate icon generation with LoRA...");
        Debug.Log($"🎨 Using LoRA: {(useLoRA ? loraFileName : "Disabled")} (Strength: {loraStrength})");

        // 1. ComfyUI 연결 테스트
        if (testConnectionFirst)
        {
            bool connected = false;
            yield return StartCoroutine(ComfyUIClient.Instance.TestConnection((result) => connected = result));

            if (!connected)
            {
                Debug.LogError("❌ ComfyUI server not available! Please start ComfyUI first.");
                isGenerating = false;
                yield break;
            }

            Debug.Log("✅ ComfyUI server connected");
        }

        // 2. 각 아이콘 생성
        for (int i = 0; i < stockIcons.Length; i++)
        {
            currentGenerationIndex = i;
            var iconData = stockIcons[i];

            if (iconData.generated)
            {
                Debug.Log($"⏭️ Skipping {iconData.stockName} (already generated)");
                continue;
            }

            Debug.Log($"🎨 Generating icon {i + 1}/{stockIcons.Length}: {iconData.stockName}");
            Debug.Log($"🎯 Inspiration: {iconData.realCompanyRef}");

            // 완전한 프롬프트 조합 (흰배경 강제 적용)
            string fullPrompt = $"{iconData.iconPrompt}, {baseStyle}";

            // 강제 흰배경 옵션이 켜져있으면 추가
            if (forceWhiteBackground)
            {
                fullPrompt += $", {whiteBackgroundKeywords}";
            }

            // LoRA 사용시 추가 키워드
            if (useLoRA)
            {
                fullPrompt += ", minimalist, icon design, clean vector";
            }

            Debug.Log($"📝 Full prompt: {fullPrompt}");

            // 아이콘 생성 (LoRA 포함, 고정 시드)
            bool success = false;
            string error = "";

            yield return StartCoroutine(ComfyUIClient.Instance.GenerateStockIcon(
                fullPrompt,
                $"icon_{iconData.stockName}",
                (isSuccess, errorMsg) => {
                    success = isSuccess;
                    error = errorMsg;
                }
            ));

            if (success)
            {
                iconData.generated = true;
                Debug.Log($"✅ Successfully generated: {iconData.stockName}");
            }
            else
            {
                Debug.LogError($"❌ Failed to generate {iconData.stockName}: {error}");
            }

            // 다음 생성까지 대기 (ComfyUI 부하 방지)
            if (i < stockIcons.Length - 1)
            {
                Debug.Log($"⏳ Waiting {delayBetweenGeneration} seconds before next generation...");
                yield return new WaitForSeconds(delayBetweenGeneration);
            }
        }

        Debug.Log("🎉 Corporate icon generation completed!");
        LogGenerationSummary();
        isGenerating = false;
    }

    /// <summary>
    /// 특정 아이콘만 다시 생성
    /// </summary>
    public void RegenerateSpecificIcon(int index)
    {
        if (index >= 0 && index < stockIcons.Length && !isGenerating)
        {
            StartCoroutine(GenerateSingleIcon(index));
        }
    }

    /// <summary>
    /// 단일 아이콘 생성
    /// </summary>
    private IEnumerator GenerateSingleIcon(int index)
    {
        isGenerating = true;
        var iconData = stockIcons[index];

        Debug.Log($"🔄 Regenerating icon: {iconData.stockName}");

        string fullPrompt = $"{iconData.iconPrompt}, {baseStyle}, {iconData.colorTheme}";
        if (useLoRA)
        {
            fullPrompt += ", minimalist, icon design, clean vector";
        }

        bool success = false;
        yield return StartCoroutine(ComfyUIClient.Instance.GenerateStockIcon(
            fullPrompt,
            $"icon_{iconData.stockName}",
            (isSuccess, error) => success = isSuccess
        ));

        if (success)
        {
            iconData.generated = true;
            Debug.Log($"✅ Successfully regenerated: {iconData.stockName}");
        }

        isGenerating = false;
    }

    /// <summary>
    /// 섹터별 아이콘 상태 확인
    /// </summary>
    public Dictionary<string, int> GetSectorProgress()
    {
        var sectorCount = new Dictionary<string, int>();
        var sectorTotal = new Dictionary<string, int>();

        foreach (var icon in stockIcons)
        {
            if (!sectorTotal.ContainsKey(icon.sector))
            {
                sectorTotal[icon.sector] = 0;
                sectorCount[icon.sector] = 0;
            }

            sectorTotal[icon.sector]++;
            if (icon.generated) sectorCount[icon.sector]++;
        }

        return sectorCount;
    }

    /// <summary>
    /// 생성 진행률 확인
    /// </summary>
    public float GetGenerationProgress()
    {
        if (!isGenerating) return 1f;
        return (float)currentGenerationIndex / stockIcons.Length;
    }

    /// <summary>
    /// 생성된 아이콘 수 확인
    /// </summary>
    public int GetGeneratedCount()
    {
        int count = 0;
        foreach (var icon in stockIcons)
        {
            if (icon.generated) count++;
        }
        return count;
    }

    /// <summary>
    /// 생성 완료 여부 확인
    /// </summary>
    public bool IsAllGenerated()
    {
        return GetGeneratedCount() == stockIcons.Length;
    }

    /// <summary>
    /// 생성 요약 로그
    /// </summary>
    private void LogGenerationSummary()
    {
        Debug.Log("📊 Icon Generation Summary:");

        var sectorProgress = GetSectorProgress();
        foreach (var sector in sectorProgress)
        {
            Debug.Log($"  📁 {sector.Key}: {sector.Value} icons generated");
        }

        int completed = GetGeneratedCount();
        Debug.Log($"📈 Total: {completed}/{stockIcons.Length} completed");

        if (completed == stockIcons.Length)
        {
            Debug.Log("🎊 All corporate icons generated successfully!");
        }
    }

    /// <summary>
    /// LoRA 설정 변경
    /// </summary>
    public void SetLoRASettings(bool enabled, string fileName, float strength)
    {
        useLoRA = enabled;
        loraFileName = fileName;
        loraStrength = strength;

        Debug.Log($"🔄 LoRA settings updated: {(enabled ? fileName : "Disabled")} (Strength: {strength})");
    }

    /// <summary>
    /// Inspector에서 모든 생성 상태 리셋
    /// </summary>
    [ContextMenu("Reset All Generation Status")]
    public void ResetAllGenerationStatus()
    {
        foreach (var icon in stockIcons)
        {
            icon.generated = false;
        }
        Debug.Log("🔄 All icon generation status reset");
    }

    /// <summary>
    /// Inspector에서 섹터별 진행률 확인
    /// </summary>
    [ContextMenu("Show Sector Progress")]
    public void ShowSectorProgress()
    {
        LogGenerationSummary();
    }

    /// <summary>
    /// Inspector에서 LoRA 테스트
    /// </summary>
    [ContextMenu("Test LoRA Generation")]
    public void TestLoRAGeneration()
    {
        if (!isGenerating && stockIcons.Length > 0)
        {
            Debug.Log("🧪 Testing LoRA with first icon...");
            StartCoroutine(GenerateSingleIcon(0));
        }
    }
}