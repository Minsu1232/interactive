using UnityEngine;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Collections;

public class CSVLocalizationManager : MonoBehaviour
{
    [Header("언어 설정")]
    public Language currentLanguage = Language.Korean;

    [Header("CSV 파일")]
    [Tooltip("StreamingAssets 폴더 내의 CSV 파일명")]
    public string csvFileName = "localization.csv";

    [Header("폰트 설정")]
    public TMP_FontAsset koreanFont;
    public TMP_FontAsset englishFont;

    [Header("디버그")]
    public bool enableDebugLog = true;

    private Dictionary<string, Dictionary<Language, string>> localizationData;
    private static CSVLocalizationManager instance;
    private bool isInitialized = false;

    // 싱글톤 패턴
    public static CSVLocalizationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CSVLocalizationManager>();
                if (instance == null)
                {
                    Debug.LogError("❌ CSVLocalizationManager가 씬에 없습니다!");
                }
            }
            return instance;
        }
    }

    // 초기화 완료 여부
    public bool IsInitialized => isInitialized;

    void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeAsync());
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 비동기 초기화
    IEnumerator InitializeAsync()
    {
        yield return StartCoroutine(LoadLocalizationDataAsync());
        isInitialized = true;

        if (enableDebugLog)
            Debug.Log("✅ CSVLocalizationManager 초기화 완료");
    }

    // CSV 데이터 비동기 로드
    IEnumerator LoadLocalizationDataAsync()
    {
        localizationData = new Dictionary<string, Dictionary<Language, string>>();

        string filePath = GetCSVFilePath();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"❌ CSV 파일을 찾을 수 없습니다: {filePath}");

            // 기본 데이터라도 생성
            CreateDefaultLocalizationData();
            yield break;
        }

        string csvContent = null;

        // try-catch 밖에서 파일 읽기
        try
        {
            csvContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ CSV 파일 로드 실패: {e.Message}");
            CreateDefaultLocalizationData();
            yield break;
        }

        // 한 프레임 대기 (파일 읽기 후)
        yield return null;

        // CSV 파싱
        try
        {
            ParseCSVContent(csvContent);

            if (enableDebugLog)
                Debug.Log($"✅ 로컬라이징 데이터 로드 완료: {localizationData.Count}개 항목");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ CSV 파싱 실패: {e.Message}");
            CreateDefaultLocalizationData();
        }
    }

    // CSV 파일 경로 가져오기
    string GetCSVFilePath()
    {
        string path = Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (enableDebugLog)
        {
            Debug.Log($"🔍 StreamingAssets 경로: {Application.streamingAssetsPath}");
            Debug.Log($"🔍 CSV 파일명: {csvFileName}");
            Debug.Log($"🔍 전체 경로: {path}");
            Debug.Log($"🔍 파일 존재 여부: {File.Exists(path)}");
        }

        return path;
    }

    // CSV 내용 파싱
    void ParseCSVContent(string csvContent)
    {
        string[] lines = csvContent.Split('\n');

        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV 파일이 비어있거나 형식이 잘못되었습니다.");
            return;
        }

        // 첫 번째 줄은 헤더 확인
        string header = lines[0].Trim();
        if (!header.ToLower().Contains("key") || !header.ToLower().Contains("korean") || !header.ToLower().Contains("english"))
        {
            Debug.LogWarning("⚠️ CSV 헤더가 예상과 다릅니다. Key,Korean,English 순서를 확인하세요.");
        }

        // 데이터 줄 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = ParseCSVLine(line);
            if (values.Length >= 3)
            {
                string key = values[0].Trim().Trim('"');
                string korean = values[1].Trim().Trim('"');
                string english = values[2].Trim().Trim('"');

                if (!string.IsNullOrEmpty(key))
                {
                    localizationData[key] = new Dictionary<Language, string>
                    {
                        { Language.Korean, korean },
                        { Language.English, english }
                    };
                }
            }
            else if (enableDebugLog)
            {
                Debug.LogWarning($"⚠️ CSV 라인 {i + 1} 형식 오류: {line}");
            }
        }
    }

    // CSV 한 줄 파싱 (쉼표와 따옴표 처리)
    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result.ToArray();
    }

    // 기본 로컬라이징 데이터 생성 (CSV 로드 실패시)
    void CreateDefaultLocalizationData()
    {
        localizationData = new Dictionary<string, Dictionary<Language, string>>
        {
            ["ui_rank"] = new Dictionary<Language, string>
            {
                { Language.Korean, "순위" },
                { Language.English, "Rank" }
            },
            ["ui_stock_name"] = new Dictionary<Language, string>
            {
                { Language.Korean, "종목명" },
                { Language.English, "Stock Name" }
            },
            ["ui_current_price"] = new Dictionary<Language, string>
            {
                { Language.Korean, "현재가" },
                { Language.English, "Current Price" }
            },
            ["ui_change_rate"] = new Dictionary<Language, string>
            {
                { Language.Korean, "등락률" },
                { Language.English, "Change Rate" }
            }
        };

        Debug.LogWarning("⚠️ 기본 로컬라이징 데이터를 사용합니다.");
    }

    // 언어 변경
    public void SetLanguage(Language newLanguage)
    {
        if (currentLanguage == newLanguage) return;

        currentLanguage = newLanguage;

        // 모든 시스템에 언어 변경 알림
        UpdateAllTexts();
        UpdateAllFonts();

        // 이벤트 발생 (다른 시스템에서 구독 가능)
        OnLanguageChanged?.Invoke(newLanguage);

        if (enableDebugLog)
            Debug.Log($"🌍 언어가 {newLanguage}로 변경되었습니다.");
    }

    // 언어 변경 이벤트
    public System.Action<Language> OnLanguageChanged;

    // 로컬라이징된 텍스트 가져오기
    public string GetLocalizedText(string key)
    {
        // 초기화가 완료되지 않았으면 기다리기
        if (!isInitialized)
        {
            if (enableDebugLog)
                Debug.LogWarning($"⏳ 로컬라이징 시스템이 아직 초기화 중입니다. 키: {key}");
            return key; // 초기화 전에는 키 자체를 반환
        }

        if (localizationData == null || !localizationData.ContainsKey(key))
        {
            if (enableDebugLog)
                Debug.LogWarning($"⚠️ 로컬라이징 키를 찾을 수 없습니다: {key} (총 {localizationData?.Count ?? 0}개 키 로드됨)");
            return key;
        }

        if (localizationData[key].ContainsKey(currentLanguage))
        {
            return localizationData[key][currentLanguage];
        }

        // 현재 언어가 없으면 영어로 fallback
        if (localizationData[key].ContainsKey(Language.English))
        {
            return localizationData[key][Language.English];
        }

        return key;
    }

    // 종목명 가져오기 (StockManager용)
    public string GetStockName(string stockKey)
    {
        return GetLocalizedText($"stock_{stockKey.ToLower()}");
    }

    // 섹터명 가져오기
    public string GetSectorName(StockSector sector)
    {
        string sectorKey = $"sector_{sector.ToString().ToLower()}";
        return GetLocalizedText(sectorKey);
    }

    // 현재 폰트 가져오기
    public TMP_FontAsset GetCurrentFont()
    {
        if (currentLanguage == Language.Korean && koreanFont != null)
            return koreanFont;
        else if (englishFont != null)
            return englishFont;

        // 기본 폰트가 없으면 Resources에서 기본 폰트 찾기
        return Resources.GetBuiltinResource<TMP_FontAsset>("LegacyRuntime.fontsettings");
    }

    // 모든 텍스트 업데이트
    void UpdateAllTexts()
    {
        // LocalizedText 컴포넌트들 업데이트
        var localizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (var localizedText in localizedTexts)
        {
            localizedText.UpdateText();
        }

        // StockManager에 알림
        var stockManager = FindFirstObjectByType<StockManager>();
        if (stockManager != null)
        {
            stockManager.OnLanguageChanged();
        }
    }

    // 모든 폰트 업데이트
    void UpdateAllFonts()
    {
        TMP_FontAsset currentFont = GetCurrentFont();
        if (currentFont == null) return;

        var allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var text in allTexts)
        {
            text.font = currentFont;
        }
    }

    // CSV 데이터 다시 로드 (런타임에 파일 변경시)
    [ContextMenu("CSV 다시 로드")]
    public void ReloadCSV()
    {
        StartCoroutine(ReloadCSVAsync());
    }

    IEnumerator ReloadCSVAsync()
    {
        yield return StartCoroutine(LoadLocalizationDataAsync());
        UpdateAllTexts();

        if (enableDebugLog)
            Debug.Log("🔄 CSV 파일을 다시 로드했습니다.");
    }

    // 현재 로드된 키 목록 가져오기 (디버그용)
    public List<string> GetAllKeys()
    {
        if (localizationData == null) return new List<string>();
        return new List<string>(localizationData.Keys);
    }

    // 특정 키 존재 여부 확인
    public bool HasKey(string key)
    {
        return localizationData != null && localizationData.ContainsKey(key);
    }

    // 언어별 텍스트 개수 확인 (디버그용)
    public int GetTextCount(Language language)
    {
        if (localizationData == null) return 0;

        int count = 0;
        foreach (var data in localizationData.Values)
        {
            if (data.ContainsKey(language) && !string.IsNullOrEmpty(data[language]))
                count++;
        }
        return count;
    }

    // 편의 메서드들
    public void SwitchToKorean() => SetLanguage(Language.Korean);
    public void SwitchToEnglish() => SetLanguage(Language.English);
    public void ToggleLanguage() => SetLanguage(currentLanguage == Language.Korean ? Language.English : Language.Korean);

    // 디버그 정보 출력
    [ContextMenu("디버그 정보 출력")]
    void PrintDebugInfo()
    {
        if (localizationData == null)
        {
            Debug.Log("❌ 로컬라이징 데이터가 없습니다.");
            return;
        }

        Debug.Log($"📊 로컬라이징 디버그 정보:");
        Debug.Log($"  - 총 키 개수: {localizationData.Count}");
        Debug.Log($"  - 한국어 텍스트 개수: {GetTextCount(Language.Korean)}");
        Debug.Log($"  - 영어 텍스트 개수: {GetTextCount(Language.English)}");
        Debug.Log($"  - 현재 언어: {currentLanguage}");
        Debug.Log($"  - CSV 파일 경로: {GetCSVFilePath()}");
        Debug.Log($"  - 파일 존재 여부: {File.Exists(GetCSVFilePath())}");
    }
}

// LocalizedText 컴포넌트 (UI 텍스트에 부착)
public class LocalizedText : MonoBehaviour
{
    [Header("로컬라이징 설정")]
    [Tooltip("CSV 파일의 Key 값")]
    public string localizationKey;

    [Header("옵션")]
    public bool updateOnStart = true;
    public bool updateOnLanguageChange = true;

    private TextMeshProUGUI textComponent;
    private string originalText; // 백업용

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            originalText = textComponent.text;
        }
    }

    void Start()
    {
        if (updateOnStart)
        {
            UpdateText();
        }

        // 언어 변경 이벤트 구독
        if (updateOnLanguageChange && CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    void OnLanguageChanged(Language newLanguage)
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (textComponent == null) return;
        if (CSVLocalizationManager.Instance == null || !CSVLocalizationManager.Instance.IsInitialized) return;
        if (string.IsNullOrEmpty(localizationKey)) return;

        string localizedText = CSVLocalizationManager.Instance.GetLocalizedText(localizationKey);
        textComponent.text = localizedText;
    }

    // 키 설정 (스크립트에서 동적으로 사용)
    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    // 원본 텍스트로 복원
    public void RestoreOriginalText()
    {
        if (textComponent != null && !string.IsNullOrEmpty(originalText))
        {
            textComponent.text = originalText;
        }
    }

    // Inspector에서 키를 설정했을 때 즉시 업데이트 (에디터용)
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (CSVLocalizationManager.Instance != null && CSVLocalizationManager.Instance.IsInitialized)
        {
            UpdateText();
        }
    }
}

// 언어 열거형
public enum Language
{
    Korean,
    English
}