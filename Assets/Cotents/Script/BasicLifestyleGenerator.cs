using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BasicLifestylePrompt
{
    [Header("Lifestyle Info")]
    public string lifestyleName;
    public string fileName;
    public string incomeRange;

    [Header("AI Prompt")]
    [TextArea(3, 6)]
    public string aiPrompt;

    [Header("Status")]
    public bool generated = false;

    [Header("Backup Strategy")]
    public bool isBackupVersion = true; // 안전빵 버전
}

public class BasicLifestyleGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public bool generateOnStart = false;
    public bool testConnectionFirst = true;
    public float delayBetweenGeneration = 5f;

    [Header("4등급 라이프스타일 프롬프트 (Clean Realistic 스타일)")]
    public BasicLifestylePrompt[] lifestylePrompts = new BasicLifestylePrompt[]
    {
        new BasicLifestylePrompt
        {
            lifestyleName = "하류층 - 절약형 라이프",
            fileName = "lifestyle_poor",
            incomeRange = "70만원 미만",
            aiPrompt = "small studio apartment, compact living space, simple furniture, cozy atmosphere, efficient use of space, modern minimalist design, soft natural lighting, organized small home, comfortable modest lifestyle",
            generated = false,
            isBackupVersion = true
        },
        new BasicLifestylePrompt
        {
            lifestyleName = "평범층 - 안정적인 생활",
            fileName = "lifestyle_middle",
            incomeRange = "70-130만원",
            aiPrompt = "comfortable family apartment, well-organized living room, modern furniture, warm family atmosphere, standard middle-class interior, everyday comfort, balanced lifestyle, practical home design",
            generated = false,
            isBackupVersion = true
        },
        new BasicLifestylePrompt
        {
            lifestyleName = "중상류층 - 여유로운 삶",
            fileName = "lifestyle_upper_middle",
            incomeRange = "130-200만원",
            aiPrompt = "elegant modern apartment, quality furniture, sophisticated interior design, spacious living area, refined lifestyle, contemporary comfort, successful professional atmosphere, stylish home decor",
            generated = false,
            isBackupVersion = true
        },
        new BasicLifestylePrompt
        {
            lifestyleName = "상류층 - 럭셔리 라이프",
            fileName = "lifestyle_wealthy",
            incomeRange = "200만원 이상",
            aiPrompt = "luxury penthouse interior, premium furniture, panoramic city view, high-end modern design, executive lifestyle, architectural excellence, sophisticated luxury living, exclusive residential space",
            generated = false,
            isBackupVersion = true
        }
    };

    [Header("Common Style Settings (Clean Realistic)")]
    [TextArea(2, 4)]
    public string commonStyleSuffix = ", clean realistic interior photography, professional architectural visualization, soft natural lighting, contemporary design, uncluttered space, high-quality interior, modern lifestyle";

    [Header("Personalized Generation Settings")]
    public bool enablePersonalizedGeneration = true;
    public int maxPersonalizedAttempts = 3; // 실시간 생성 실패시 재시도 횟수

    private int currentGenerationIndex = 0;
    private bool isGenerating = false;

    void Start()
    {
        if (generateOnStart)
        {
            StartBasicLifestyleGeneration();
        }
    }

    /// <summary>
    /// 기본 라이프스타일 이미지 4종 생성 시작 (안전빵 버전)
    /// </summary>
    public void StartBasicLifestyleGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(GenerateAllBasicLifestyles());
        }
        else
        {
            Debug.LogWarning("⚠️ Generation already in progress!");
        }
    }

    /// <summary>
    /// 모든 기본 라이프스타일 이미지 생성 (안전빵용)
    /// </summary>
    private IEnumerator GenerateAllBasicLifestyles()
    {
        isGenerating = true;
        Debug.Log("🏠 Starting basic lifestyle image generation (Backup Version)...");

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

        // 2. 각 라이프스타일 이미지 생성
        for (int i = 0; i < lifestylePrompts.Length; i++)
        {
            currentGenerationIndex = i;
            var lifestylePrompt = lifestylePrompts[i];

            if (lifestylePrompt.generated)
            {
                Debug.Log($"⏭️ Skipping {lifestylePrompt.lifestyleName} (already generated)");
                continue;
            }

            Debug.Log($"🏠 Generating lifestyle {i + 1}/{lifestylePrompts.Length}: {lifestylePrompt.lifestyleName}");
            Debug.Log($"💰 Income Range: {lifestylePrompt.incomeRange}");

            // 완전한 프롬프트 조합
            string fullPrompt = lifestylePrompt.aiPrompt + commonStyleSuffix;
            Debug.Log($"📝 Full prompt: {fullPrompt}");

            // 이미지 생성 (안전빵용이므로 Base 모델만 사용)
            bool success = false;
            string error = "";

            yield return StartCoroutine(ComfyUIClient.Instance.GenerateLifestyleBackground(
                fullPrompt,
                lifestylePrompt.fileName,
                (isSuccess, errorMsg) => {
                    success = isSuccess;
                    error = errorMsg;
                }
            ));

            if (success)
            {
                lifestylePrompt.generated = true;
                Debug.Log($"✅ Successfully generated: {lifestylePrompt.lifestyleName}");
            }
            else
            {
                Debug.LogError($"❌ Failed to generate {lifestylePrompt.lifestyleName}: {error}");
            }

            // 다음 생성까지 대기 (ComfyUI 부하 방지)
            if (i < lifestylePrompts.Length - 1)
            {
                Debug.Log($"⏳ Waiting {delayBetweenGeneration} seconds before next generation...");
                yield return new WaitForSeconds(delayBetweenGeneration);
            }
        }

        Debug.Log("🎉 Basic lifestyle image generation completed!");
        LogGenerationSummary();
        isGenerating = false;
    }

    /// <summary>
    /// 개인화된 라이프스타일 이미지 생성 (실시간, 사용자 데이터 기반)
    /// </summary>
    public IEnumerator GeneratePersonalizedLifestyle(
        int lifestyleLevel,
        Dictionary<string, object> userGameData,
        string personalizedFileName,
        System.Action<bool, string, string> onComplete)
    {
        if (!enablePersonalizedGeneration)
        {
            Debug.LogWarning("⚠️ Personalized generation is disabled, using backup version");
            string backupPath = GetBackupLifestylePath(lifestyleLevel);
            onComplete?.Invoke(true, backupPath, "backup");
            yield break;
        }

        Debug.Log($"🎯 Generating personalized lifestyle for level {lifestyleLevel}");

        // 기본 라이프스타일 프롬프트 가져오기
        if (lifestyleLevel < 0 || lifestyleLevel >= lifestylePrompts.Length)
        {
            Debug.LogError($"❌ Invalid lifestyle level: {lifestyleLevel}");
            onComplete?.Invoke(false, "", "error");
            yield break;
        }

        var baseLifestyle = lifestylePrompts[lifestyleLevel];

        // 사용자 데이터 기반 개인화 (실시간 생성)
        int attempts = 0;
        bool success = false;
        string error = "";

        while (attempts < maxPersonalizedAttempts && !success)
        {
            attempts++;
            Debug.Log($"🔄 Personalized generation attempt {attempts}/{maxPersonalizedAttempts}");

            yield return StartCoroutine(ComfyUIClient.Instance.GeneratePersonalizedLifestyle(
                baseLifestyle.aiPrompt,
                userGameData,
                personalizedFileName,
                (isSuccess, errorMsg) => {
                    success = isSuccess;
                    error = errorMsg;
                }
            ));

            if (!success && attempts < maxPersonalizedAttempts)
            {
                Debug.LogWarning($"⚠️ Attempt {attempts} failed, retrying...");
                yield return new WaitForSeconds(2f); // 재시도 대기
            }
        }

        if (success)
        {
            Debug.Log($"✅ Personalized lifestyle generated successfully!");
            onComplete?.Invoke(true, $"StreamingAssets/Generated/Lifestyle/{personalizedFileName}.png", "personalized");
        }
        else
        {
            Debug.LogWarning($"⚠️ Personalized generation failed after {maxPersonalizedAttempts} attempts, using backup");
            string backupPath = GetBackupLifestylePath(lifestyleLevel);
            onComplete?.Invoke(true, backupPath, "backup");
        }
    }

    /// <summary>
    /// 안전빵 라이프스타일 이미지 경로 반환
    /// </summary>
    private string GetBackupLifestylePath(int lifestyleLevel)
    {
        if (lifestyleLevel >= 0 && lifestyleLevel < lifestylePrompts.Length)
        {
            var lifestyle = lifestylePrompts[lifestyleLevel];
            return $"StreamingAssets/Generated/Lifestyle/{lifestyle.fileName}.png";
        }

        Debug.LogError($"❌ Invalid lifestyle level for backup: {lifestyleLevel}");
        return "";
    }

    /// <summary>
    /// 특정 라이프스타일만 다시 생성
    /// </summary>
    public void RegenerateSpecificLifestyle(int index)
    {
        if (index >= 0 && index < lifestylePrompts.Length && !isGenerating)
        {
            StartCoroutine(GenerateSingleLifestyle(index));
        }
    }

    /// <summary>
    /// 단일 라이프스타일 이미지 생성
    /// </summary>
    private IEnumerator GenerateSingleLifestyle(int index)
    {
        isGenerating = true;
        var lifestylePrompt = lifestylePrompts[index];

        Debug.Log($"🔄 Regenerating lifestyle: {lifestylePrompt.lifestyleName}");

        string fullPrompt = lifestylePrompt.aiPrompt + commonStyleSuffix;

        bool success = false;
        yield return StartCoroutine(ComfyUIClient.Instance.GenerateLifestyleBackground(
            fullPrompt,
            lifestylePrompt.fileName,
            (isSuccess, error) => success = isSuccess
        ));

        if (success)
        {
            lifestylePrompt.generated = true;
            Debug.Log($"✅ Successfully regenerated: {lifestylePrompt.lifestyleName}");
        }

        isGenerating = false;
    }

    /// <summary>
    /// 게임 결과를 라이프스타일 레벨로 변환
    /// </summary>
    public int ConvertToLifestyleLevel(float finalMoney)
    {
        // 기획서 기준: 상류층(200만+), 중상류층(130-200), 평범층(70-130), 하류층(70미만)
        if (finalMoney >= 2000000f) return 3; // 상류층
        else if (finalMoney >= 1300000f) return 2; // 중상류층  
        else if (finalMoney >= 700000f) return 1; // 평범층
        else return 0; // 하류층
    }

    /// <summary>
    /// 라이프스타일 이름 반환
    /// </summary>
    public string GetLifestyleName(int level)
    {
        if (level >= 0 && level < lifestylePrompts.Length)
        {
            return lifestylePrompts[level].lifestyleName;
        }
        return "Unknown";
    }

    /// <summary>
    /// 라이프스타일 소득 범위 반환
    /// </summary>
    public string GetIncomeRange(int level)
    {
        if (level >= 0 && level < lifestylePrompts.Length)
        {
            return lifestylePrompts[level].incomeRange;
        }
        return "Unknown";
    }

    /// <summary>
    /// 생성 진행률 확인
    /// </summary>
    public float GetGenerationProgress()
    {
        if (!isGenerating) return 1f;
        return (float)currentGenerationIndex / lifestylePrompts.Length;
    }

    /// <summary>
    /// 생성된 이미지 수 확인
    /// </summary>
    public int GetGeneratedCount()
    {
        int count = 0;
        foreach (var prompt in lifestylePrompts)
        {
            if (prompt.generated) count++;
        }
        return count;
    }

    /// <summary>
    /// 생성 완료 여부 확인
    /// </summary>
    public bool IsAllGenerated()
    {
        return GetGeneratedCount() == lifestylePrompts.Length;
    }

    /// <summary>
    /// 생성 요약 로그
    /// </summary>
    private void LogGenerationSummary()
    {
        Debug.Log("📊 Lifestyle Generation Summary:");
        for (int i = 0; i < lifestylePrompts.Length; i++)
        {
            var prompt = lifestylePrompts[i];
            string status = prompt.generated ? "✅" : "❌";
            Debug.Log($"  {status} {prompt.lifestyleName} ({prompt.incomeRange})");
        }

        int completed = GetGeneratedCount();
        Debug.Log($"📈 Total: {completed}/{lifestylePrompts.Length} completed");

        if (completed == lifestylePrompts.Length)
        {
            Debug.Log("🎊 All backup lifestyle images ready!");
        }
    }

    /// <summary>
    /// 실시간 생성 테스트 (개발용)
    /// </summary>
    public void TestPersonalizedGeneration()
    {
        if (!isGenerating)
        {
            // 테스트용 사용자 데이터
            var testUserData = new Dictionary<string, object>
            {
                {"finalReturn", 1.8f}, // 180% 수익 (중상류층)
                {"preferredSector", "tech"},
                {"riskTaking", true},
                {"investmentStyle", "aggressive"}
            };

            Debug.Log("🧪 Testing personalized generation...");
            StartCoroutine(GeneratePersonalizedLifestyle(
                2, // 중상류층
                testUserData,
                "test_personalized",
                (success, path, type) => {
                    Debug.Log($"🧪 Test result: {(success ? "Success" : "Failed")} ({type})");
                    if (success) Debug.Log($"📁 Image path: {path}");
                }
            ));
        }
    }

    /// <summary>
    /// Inspector에서 모든 생성 상태 리셋
    /// </summary>
    [ContextMenu("Reset All Generation Status")]
    public void ResetAllGenerationStatus()
    {
        foreach (var prompt in lifestylePrompts)
        {
            prompt.generated = false;
        }
        Debug.Log("🔄 All generation status reset");
    }

    /// <summary>
    /// Inspector에서 개인화 기능 토글
    /// </summary>
    [ContextMenu("Toggle Personalized Generation")]
    public void TogglePersonalizedGeneration()
    {
        enablePersonalizedGeneration = !enablePersonalizedGeneration;
        Debug.Log($"🔄 Personalized generation: {(enablePersonalizedGeneration ? "Enabled" : "Disabled")}");
    }

    /// <summary>
    /// Inspector에서 실시간 생성 테스트
    /// </summary>
    [ContextMenu("Test Personalized Generation")]
    public void TestPersonalizedGenerationFromInspector()
    {
        TestPersonalizedGeneration();
    }
}